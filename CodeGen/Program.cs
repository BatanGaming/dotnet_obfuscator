using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json;

namespace ResultProject
{
    public static class Program
    {
        [Serializable]
        public class InstructionInfo
        {
            public long Offset { get; set; }
            public int Size { get; set; }
            public OperandInfo OperandInfo { get; set; }
        }
        
        [Serializable]
        public class OperandInfo
        {
            public OperandTypeInfo? OperandType { get; set; }
            public string OperandName { get; set; }
            public string[] ParametersTypesNames { get; set; }
            public string[] GenericTypesNames { get; set; }
        }
        
        [Serializable]
        public enum OperandTypeInfo
        {
            Type,
            Method,
            Field,
            String,
            Signature
        }
        
        [Serializable]
        public class SerializableLocalVariableInfo
        {
            public string TypeName { get; set; }
            public bool IsPinned { get; set; }
        }
        
        [Serializable]
        public class SerializableMethodBody
        {
            public byte[] IlCode { get; set; }
            public List<InstructionInfo> Instructions { get; set; }
            public int MaxStackSize { get; set; }
            public List<SerializableLocalVariableInfo> LocalVariables { get; set; }
        }
        
        private static readonly Dictionary<string, object> _methods = new Dictionary<string, object> {
            $SERIALIZED_METHODS
        };

        private static readonly List<string> _referencedAssemblies = new List<string> {
            $REFERENCED_ASSEMBLIES
        };
        private static readonly Dictionary<string, Type> _cachedTypes = new Dictionary<string, Type>();
        private static readonly Dictionary<string, MethodBase> _cachedMethods = new Dictionary<string, MethodBase>();

        private static Type GetTypeByName(string name) {
            if (_cachedTypes.TryGetValue(name, out var type)) {
                return type;
            }
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies) {
                var foundedType = assembly.GetType(name);
                if (foundedType != null) {
                    _cachedTypes[name] = foundedType;
                    return foundedType;
                }
            }

            return null;
        }

        private static MethodBase GetMethodByName(string name, IReadOnlyCollection<string> parametersNames) {
            var index = name.LastIndexOf('#');
            var methodName = name.Substring(index + 1);
            var cachedName = $"{methodName}({string.Join(',', parametersNames)})";
            if (_cachedMethods.TryGetValue(cachedName, out var method)) {
                return method;
            }
            var typeName = name.Remove(index);
            var type = GetTypeByName(typeName);
            var parametersTypes = parametersNames.Count != 0
                ? (from parameter in parametersNames select GetTypeByName(parameter)).ToArray()
                : Type.EmptyTypes;
            method = methodName == ".ctor" || methodName == ".cctor"
                ? (MethodBase)type.GetConstructor(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic, null, parametersTypes, null)
                : type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic, null, parametersTypes, null);
            _cachedMethods[cachedName] = method;
            return method;
        }
        
        private static MethodInfo GetGenericMethod(OperandInfo info) {
            var index = info.OperandName.LastIndexOf('#');
            var methodName = info.OperandName.Substring(index + 1);
            var typeName = info.OperandName.Remove(index);
            var type = GetTypeByName(typeName);
            var genericParametersTypes = info.GenericTypesNames.Select(GetTypeByName).ToArray();
            var parametersTypes = info.ParametersTypesNames.Length != 0
                ? (from parameter in info.ParametersTypesNames select GetTypeByName(parameter)).ToArray()
                : Type.EmptyTypes;
            var possibleMethods = type
                .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.Instance)
                .Where(m => 
                    m.Name == methodName && 
                    m.IsGenericMethod && 
                    m.GetGenericArguments().Length == genericParametersTypes.Length &&
                    m.GetParameters().Length == parametersTypes.Length
                )
                .ToList();

            return (from method in possibleMethods
                select method.MakeGenericMethod(genericParametersTypes)
                into exactMethod
                let parameters = exactMethod.GetParameters()
                let isGood = !parameters.Where((t, i) => t.ParameterType != parametersTypes[i]).Any()
                where isGood
                select exactMethod).FirstOrDefault();
        }

        private static FieldInfo GetFieldByName(string name) {
            var index = name.LastIndexOf('#');
            var fieldName = name.Substring(index + 1);
            var typeName = name.Remove(index);
            var type = GetTypeByName(typeName);
            return type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private static void OverwriteTokens(SerializableMethodBody body, DynamicILInfo ilInfo) {
            foreach (var instruction in body.Instructions.Where(instruction => instruction.OperandInfo.OperandType != null)) {
                int token;
                switch (instruction.OperandInfo.OperandType) {
                    case OperandTypeInfo.Method:
                    {
                        MethodBase method;
                        if (instruction.OperandInfo.GenericTypesNames != null && instruction.OperandInfo.GenericTypesNames.Length != 0) {
                            method = GetGenericMethod(instruction.OperandInfo);
                        }
                        else {
                            method = GetMethodByName(instruction.OperandInfo.OperandName,
                            instruction.OperandInfo.ParametersTypesNames);
                        }
                        token = method.DeclaringType.IsGenericType
                            ? ilInfo.GetTokenFor(method.MethodHandle, method.DeclaringType.TypeHandle)
                            : ilInfo.GetTokenFor(method.MethodHandle);
                        break;
                    }
                    case OperandTypeInfo.Field:
                    {
                        var field = GetFieldByName(instruction.OperandInfo.OperandName);
                        token = field.DeclaringType.IsGenericType
                            ? ilInfo.GetTokenFor(field.FieldHandle, field.DeclaringType.TypeHandle)
                            : ilInfo.GetTokenFor(field.FieldHandle);
                        break;
                    }
                    default:
                        token = instruction.OperandInfo.OperandType switch {
                            OperandTypeInfo.String => ilInfo.GetTokenFor(instruction.OperandInfo.OperandName),
                            OperandTypeInfo.Type => ilInfo.GetTokenFor(GetTypeByName(instruction.OperandInfo.OperandName).TypeHandle)
                        };
                        break;
                }
                OverwriteInt32(token, (int)instruction.Offset + instruction.Size, body.IlCode);
            }
        }
        
        private static void OverwriteInt32(int value, int pos, IList<byte> array) {
            array[pos++] = (byte) value;
            array[pos++] = (byte) (value >> 8);
            array[pos++] = (byte) (value >> 16);
            array[pos++] = (byte) (value >> 24);
        }

        private static Type GetDelegateType(int parametersCount, bool returnValue) {
            return parametersCount switch {
                0 when returnValue => typeof(Func<>),
                0 when !returnValue => typeof(Action),
                1 when returnValue => typeof(Func<,>),
                1 when !returnValue => typeof(Action<>),
                2 when returnValue => typeof(Func<,,>),
                2 when !returnValue => typeof(Action<,>),
                3 when returnValue => typeof(Func<,,,>),
                3 when !returnValue => typeof(Action<,,>),
                4 when returnValue => typeof(Func<,,,,>),
                4 when !returnValue => typeof(Action<,,,>),
                5 when returnValue => typeof(Func<,,,,,>),
                5 when !returnValue => typeof(Action<,,,,>),
                6 when returnValue => typeof(Func<,,,,,,>),
                6 when !returnValue => typeof(Action<,,,,,>),
                7 when returnValue => typeof(Func<,,,,,,,>),
                7 when !returnValue => typeof(Action<,,,,,,>),
                8 when returnValue => typeof(Func<,,,,,,,,>),
                8 when !returnValue => typeof(Action<,,,,,,,>),
                9 when returnValue => typeof(Func<,,,,,,,,,>),
                9 when !returnValue => typeof(Action<,,,,,,,,>),
                10 when returnValue => typeof(Func<,,,,,,,,,,>),
                10 when !returnValue => typeof(Action<,,,,,,,,,>),
                11 when returnValue => typeof(Func<,,,,,,,,,,,>),
                11 when !returnValue => typeof(Action<,,,,,,,,,,>),
                12 when returnValue => typeof(Func<,,,,,,,,,,,,>),
                12 when !returnValue => typeof(Action<,,,,,,,,,,,>),
                13 when returnValue => typeof(Func<,,,,,,,,,,,,,>),
                13 when !returnValue => typeof(Action<,,,,,,,,,,,,>),
                14 when returnValue => typeof(Func<,,,,,,,,,,,,,,>),
                14 when !returnValue => typeof(Action<,,,,,,,,,,,,,>),
                15 when returnValue => typeof(Func<,,,,,,,,,,,,,,,>),
                15 when !returnValue => typeof(Action<,,,,,,,,,,,,,,>),
                16 when returnValue => typeof(Func<,,,,,,,,,,,,,,,,>),
                16 when !returnValue => typeof(Action<,,,,,,,,,,,,,,,>),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static Type CloseDelegateType(Type delegateType, Type[] genericTypes) {
            return delegateType.IsGenericTypeDefinition
                ? delegateType.MakeGenericType(genericTypes)
                : delegateType;
        }

        private static Delegate GetMethod(string name, MethodInfo methodInfo, object target) {
            if (_methods[name] is (DynamicMethod method, Type delegateType)) {
                return method.CreateDelegate(delegateType, target);
            }
            var parameters = methodInfo.GetParameters().Select(p => p.ParameterType).ToList();
            var hasReturnType = methodInfo.ReturnType != typeof(void);
            delegateType = CloseDelegateType(
                GetDelegateType(parameters.Count, hasReturnType),
                (hasReturnType
                    ? parameters.Concat(new[] {methodInfo.ReturnType})
                    : parameters).ToArray()
            );
            
            if (!methodInfo.IsStatic) {
                parameters.Insert(0, methodInfo.DeclaringType);
            }
            method = new DynamicMethod(
                methodInfo.Name, 
                methodInfo.ReturnType, 
                parameters.ToArray(),
                target?.GetType() ?? methodInfo.DeclaringType,
                false
            );
            var encodedMethodBody = _methods[name] as string;
            var serializedMethodBody = Encoding.ASCII.GetString(Convert.FromBase64String(encodedMethodBody));
            var methodBody = JsonSerializer.Deserialize<SerializableMethodBody>(serializedMethodBody);
            var ilInfo = method.GetDynamicILInfo();
            
            var localVarSigHelper = SignatureHelper.GetLocalVarSigHelper();
            foreach (var local in methodBody.LocalVariables) {
                localVarSigHelper.AddArgument(GetTypeByName(local.TypeName), local.IsPinned);
            }
            ilInfo.SetLocalSignature(localVarSigHelper.GetSignature());
            
            OverwriteTokens(methodBody, ilInfo);
            ilInfo.SetCode(methodBody.IlCode, methodBody.MaxStackSize);
            _methods[name] = (method, delegateType);
            return method.CreateDelegate(delegateType, target);
        }

        private static void LoadAssemblies() {
            foreach (var assemblyName in _referencedAssemblies) {
                Assembly.Load(assemblyName);
            }
        }
        
        public static Delegate GetMethod(object target) {
            var stackTrace = new StackTrace();
            var caller = stackTrace.GetFrame(1).GetMethod();
            var name = $"{caller.DeclaringType.FullName}#{caller.Name}";
            var callerMethodInfo = GetMethodByName(
                name, 
                caller
                    .GetParameters()
                    .Select(p => p.ParameterType.FullName)
                    .ToList()
                );
            return GetMethod(name, callerMethodInfo as MethodInfo, target);
        }
        
        public static void Main(string[] args) {
            LoadAssemblies();

            var assembly_name = new AssemblyName("$ASSEMBLY_NAME");
            var assembly_builder = AssemblyBuilder.DefineDynamicAssembly(assembly_name, AssemblyBuilderAccess.Run);
            var module_builder = assembly_builder.DefineDynamicModule(assembly_name.Name + ".dll");
                
            $TYPES
                
            $NESTED_TYPES
                
            $PARENTS    
                
            $INTERFACES_IMPLEMENTATIONS
                
            $FIELDS
                
                
            $CONSTRUCTORS_DEFINITIONS
                
            $CONSTRUCTORS_BODIES
                
            
            $METHODS_DEFINITIONS
                
            
            $METHODS_BODIES
                

            $CREATED_TYPES
        }
    }
}