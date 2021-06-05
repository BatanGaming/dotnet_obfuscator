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
            public string[] DeclaringTypeGenericTypesNames { get; set; }
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
            public string[] GenericTypesNames { get; set; }
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
        
        private static Type GetTypeByName(string name) {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies) {
                var foundedType = assembly.GetType(name);
                if (foundedType != null) {
                    return foundedType;
                }
            }
            return null;
        }

        private static MethodBase GetMethodByName(string name, IReadOnlyCollection<string> parametersNames, IReadOnlyDictionary<string, Type> genericTypes, IEnumerable<string> genericArguments) {
            var index = name.LastIndexOf('#');
            var methodName = name.Substring(index + 1);
            var cachedName = $"{name}({string.Join(',', parametersNames)})";
            var typeName = name.Remove(index);
            var type = GetTypeByName(typeName);
            if (type.IsGenericTypeDefinition) {
                type = type.MakeGenericType(genericArguments.Select(a => genericTypes[a]).ToArray());
            }
            var parametersTypes = parametersNames.Count != 0
                ? (from parameter in parametersNames select GetTypeByName(parameter)).ToArray()
                : Type.EmptyTypes;
            MethodBase method;
            if (methodName.Contains("ctor")) {
                var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | (methodName == ".ctor" ? BindingFlags.Instance : BindingFlags.Static);
                method = type.GetConstructor(bindingFlags, null, parametersTypes, null);
            }
            else {
                method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic, null, parametersTypes, null);
            }
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

        private static FieldInfo GetFieldByName(string name, IReadOnlyDictionary<string, Type> genericTypes, IEnumerable<string> genericArguments) {
            var index = name.LastIndexOf('#');
            var fieldName = name.Substring(index + 1);
            var typeName = name.Remove(index);
            var type = GetTypeByName(typeName);
            if (type.IsGenericTypeDefinition) {
                type = type.MakeGenericType(genericArguments.Select(t => genericTypes[t]).ToArray());
            }
            return type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private static void OverwriteTokens(SerializableMethodBody body, DynamicILInfo ilInfo, IReadOnlyDictionary<string, Type> genericTypes = null) {
            foreach (var instruction in body.Instructions.Where(instruction => instruction.OperandInfo.OperandType != null)) {
                int token = 0;
                switch (instruction.OperandInfo.OperandType) {
                    case OperandTypeInfo.Method:
                    {
                        MethodBase method;
                        if (instruction.OperandInfo.GenericTypesNames != null && instruction.OperandInfo.GenericTypesNames.Length != 0) {
                            if (genericTypes != null) {
                                for (var i = 0; i < instruction.OperandInfo.GenericTypesNames.Length; ++i) {
                                    if (genericTypes.ContainsKey(instruction.OperandInfo.GenericTypesNames[i])) {
                                        instruction.OperandInfo.GenericTypesNames[i] = genericTypes[instruction.OperandInfo.GenericTypesNames[i]].FullName;
                                    }
                                }
                            }
                            method = GetGenericMethod(instruction.OperandInfo);
                        }
                        else {
                            method = GetMethodByName(instruction.OperandInfo.OperandName,
                            instruction.OperandInfo.ParametersTypesNames, genericTypes, instruction.OperandInfo.DeclaringTypeGenericTypesNames);
                        }
                        token = method.DeclaringType.IsGenericType
                            ? ilInfo.GetTokenFor(method.MethodHandle, method.DeclaringType.TypeHandle)
                            : ilInfo.GetTokenFor(method.MethodHandle);
                        break;
                    }
                    case OperandTypeInfo.Field:
                    {
                        var field = GetFieldByName(instruction.OperandInfo.OperandName, genericTypes, instruction.OperandInfo.DeclaringTypeGenericTypesNames);
                        token = field.DeclaringType.IsGenericType
                            ? ilInfo.GetTokenFor(field.FieldHandle, field.DeclaringType.TypeHandle)
                            : ilInfo.GetTokenFor(field.FieldHandle);
                        break;
                    }
                    case OperandTypeInfo.String:
                    {
                        token = ilInfo.GetTokenFor(instruction.OperandInfo.OperandName);
                        break;
                    }
                    case OperandTypeInfo.Type:
                    {
                        if (genericTypes != null) {
                            if (genericTypes.ContainsKey(instruction.OperandInfo.OperandName)) {
                                token = ilInfo.GetTokenFor(genericTypes[instruction.OperandInfo.OperandName].TypeHandle);
                                break;
                            }
                        }
                        token = ilInfo.GetTokenFor(GetTypeByName(instruction.OperandInfo.OperandName).TypeHandle);
                        break;
                    }
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

        private static Delegate GetGenericMethod(MethodInfo methodInfo, IReadOnlyDictionary<string, Type> genericTypes, object target) {
            var methodName = $"{methodInfo.DeclaringType.FullName}#{methodInfo.Name}";
            var parameters = methodInfo.GetParameters().Select(p =>
                p.ParameterType.IsGenericParameter ? genericTypes[p.ParameterType.Name] : p.ParameterType).ToList();
            var returnType = methodInfo.ReturnType == typeof(void)
                ? null
                : methodInfo.ReturnType.IsGenericParameter
                    ? genericTypes[methodInfo.ReturnType.Name]
                    : methodInfo.ReturnType;
            var delegateType = CloseDelegateType(
                GetDelegateType(parameters.Count, returnType != null),
                (returnType == null ? parameters : parameters.Concat(new[] {returnType})).ToArray()
                );
            var declaringType = methodInfo.DeclaringType.IsGenericTypeDefinition
                ? methodInfo.DeclaringType.MakeGenericType(methodInfo.DeclaringType.GetGenericArguments().Select(t => genericTypes[t.Name]).ToArray())
                : methodInfo.DeclaringType;
            if (!methodInfo.IsStatic) {
                parameters.Insert(0, declaringType);
            }
            var method = new DynamicMethod(
                methodInfo.Name, 
                returnType, 
                parameters.ToArray(),
                target?.GetType() ?? declaringType,
                false
            );
            var encodedMethodBody = _methods[methodName] as string;
            var serializedMethodBody = Encoding.ASCII.GetString(Convert.FromBase64String(encodedMethodBody));
            var methodBody = JsonSerializer.Deserialize<SerializableMethodBody>(serializedMethodBody);
            var ilInfo = method.GetDynamicILInfo();
            
            var localVarSigHelper = SignatureHelper.GetLocalVarSigHelper();
            foreach (var local in methodBody.LocalVariables) {
                var type = GetTypeByName(local.TypeName) ?? genericTypes[local.TypeName];
                if (type.IsGenericTypeDefinition) {
                    type = type.MakeGenericType(local.GenericTypesNames.Select(a => genericTypes[a]).ToArray());
                }
                localVarSigHelper.AddArgument(type, local.IsPinned);
            }
            ilInfo.SetLocalSignature(localVarSigHelper.GetSignature());
            
            OverwriteTokens(methodBody, ilInfo, genericTypes);
            ilInfo.SetCode(methodBody.IlCode, methodBody.MaxStackSize);
            return method.CreateDelegate(delegateType, target);
        }

        public static Delegate GetMethod(MethodInfo methodInfo, Dictionary<string, Type> genericTypes, object target) {
            if (genericTypes != null) {
                return GetGenericMethod(methodInfo, genericTypes, target);
            }
            var methodName = $"{methodInfo.DeclaringType.FullName}#{methodInfo.Name}";
            if (_methods[methodName] is (DynamicMethod method, Type delegateType)) {
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
            var encodedMethodBody = _methods[methodName] as string;
            var serializedMethodBody = Encoding.ASCII.GetString(Convert.FromBase64String(encodedMethodBody));
            var methodBody = JsonSerializer.Deserialize<SerializableMethodBody>(serializedMethodBody);
            var ilInfo = method.GetDynamicILInfo();
            
            var localVarSigHelper = SignatureHelper.GetLocalVarSigHelper();
            foreach (var local in methodBody.LocalVariables) {
                var type = GetTypeByName(local.TypeName);
                localVarSigHelper.AddArgument(type, local.IsPinned);
            }
            ilInfo.SetLocalSignature(localVarSigHelper.GetSignature());
            
            OverwriteTokens(methodBody, ilInfo);
            ilInfo.SetCode(methodBody.IlCode, methodBody.MaxStackSize);
            _methods[methodName] = (method, delegateType);
            return method.CreateDelegate(delegateType, target);
        }
        
        private static void LoadAssemblies() {
            foreach (var assemblyName in _referencedAssemblies) {
                Assembly.Load(assemblyName);
            }
        }

        public static void Main(string[] args) {
            LoadAssemblies();

            var assembly_name = new AssemblyName("$ASSEMBLY_NAME");
            var assembly_builder = AssemblyBuilder.DefineDynamicAssembly(assembly_name, AssemblyBuilderAccess.Run);
            var module_builder = assembly_builder.DefineDynamicModule(assembly_name.Name + ".dll");
                
            $TYPES
                
            $NESTED_TYPES
                
            $PARENTS

            $GENERIC_CONSTRAINTS_TYPES

            $INTERFACES_IMPLEMENTATIONS
                
            $FIELDS
                
            $ENUM_CONSTANTS
                
            $CONSTRUCTORS_DEFINITIONS
                
            $CONSTRUCTORS_BODIES
                
            
            $METHODS_DEFINITIONS
                
            $GENERIC_CONSTRAINTS_METHODS
                
            $METHODS_PARAMETERS
                
            $METHODS_RETURN_TYPES
                
            $METHODS_OVERRIDING
                
            
            $METHODS_BODIES
                

            $CREATED_TYPES
        }
    }
}