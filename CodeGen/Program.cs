using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Newtonsoft.Json;

namespace ResultProject
{
    [Serializable]
    public class InstructionInfo
    {
        public long Offset;
        public int Size;
        public OperandInfo OperandInfo;
    }

    [Serializable]
    public class OperandInfo
    {
        public OperandTypeInfo? OperandType;
        public string OperandName;
        public TypeInfo[] Parameters;
        public TypeInfo[] GenericTypes;
        public TypeInfo[] DeclaringTypeGenericTypes;
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
        public TypeInfo Info;
        public bool IsPinned;
    }

    [Serializable]
    public class SerializableMethodBody
    {
        public byte[] IlCode;
        public InstructionInfo[] Instructions;
        public int MaxStackSize;
        public SerializableLocalVariableInfo[] LocalVariables;
    }

    [Serializable]
    public class TypeInfo
    {
        public string Name;
        public TypeInfo[] GenericArguments;
        public bool IsByRef;
        public bool IsPointer;
        public bool IsArray;
    }
    public static class Program
    {
        private static Assembly _currentAssembly;

        private static Dictionary<string, object> _methods = new Dictionary<string, object>();

        private static readonly List<string> _referencedAssemblies = new List<string> {
            $REFERENCED_ASSEMBLIES
        };

        private static Stopwatch _generationWatch = new();
        private static int _generationCount = 0;

        private static Type GetTypeByName(string name) {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies.Except(new[] { _currentAssembly })) {
                var foundedType = assembly.GetType(name);
                if (foundedType != null) {
                    return foundedType;
                }
            }
            return null;
        }

        private static MethodBase GetMethodByName(OperandInfo info, IReadOnlyCollection<TypeInfo> parameters, IReadOnlyDictionary<string, Type> genericTypes, IEnumerable<TypeInfo> genericArguments) {
            var index = info.OperandName.LastIndexOf('#');
            var methodName = info.OperandName.Substring(index + 1);
            var cachedName = $"{info.OperandName}({string.Join(',', parameters)})";
            var typeName = info.OperandName.Remove(index);
            var type = GetTypeByName(typeName);
            if (type.IsGenericTypeDefinition) {
                type = type.MakeGenericType(genericArguments.Select(a => ConstructGenericType(a, genericTypes)).ToArray());
            }
            var parametersTypes = parameters.Count != 0
                ? (from parameter in parameters select ConstructGenericType(parameter, genericTypes)).ToArray()
                : Type.EmptyTypes;
            MethodBase method;
            if (methodName.Contains(".ctor") || methodName.Contains(".cctor")) {
                var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | (methodName == ".ctor" ? BindingFlags.Instance : BindingFlags.Static);
                method = type.GetConstructor(bindingFlags, null, parametersTypes, null);
            }
            else {
                method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic, null, parametersTypes, null);
            }
            return method;
        }

        private static MethodInfo GetGenericMethod(OperandInfo info, IReadOnlyDictionary<string, Type> genericTypes) {
            var index = info.OperandName.LastIndexOf('#');
            var methodName = info.OperandName.Substring(index + 1);
            var typeName = info.OperandName.Remove(index);
            var type = GetTypeByName(typeName);
            var genericParametersTypes = info.GenericTypes.Select(t => ConstructGenericType(t, genericTypes)).ToArray();
            var parametersTypes = info.Parameters.Length != 0
                ? (from parameter in info.Parameters select ConstructGenericType(parameter, genericTypes)).ToArray()
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

        private static FieldInfo GetFieldByName(string name, IReadOnlyDictionary<string, Type> genericTypes, IEnumerable<TypeInfo> genericArguments) {
            var index = name.LastIndexOf('#');
            var fieldName = name.Substring(index + 1);
            var typeName = name.Remove(index);
            var type = GetTypeByName(typeName);
            if (type.IsGenericTypeDefinition) {
                type = type.MakeGenericType(genericArguments.Select(t => ConstructGenericType(t, genericTypes)).ToArray());
            }
            return type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private static void OverwriteTokens(SerializableMethodBody body, DynamicILInfo ilInfo, IReadOnlyDictionary<string, Type> genericTypes = null) {
            foreach (var instruction in body.Instructions.Where(instruction => instruction.OperandInfo.OperandType != null)) {
                int token = 0;
                switch (instruction.OperandInfo.OperandType) {
                    case OperandTypeInfo.Method: {
                        MethodBase method;
                        if (instruction.OperandInfo.GenericTypes != null && instruction.OperandInfo.GenericTypes.Length != 0) {
                            method = GetGenericMethod(instruction.OperandInfo, genericTypes);
                        }
                        else {
                            method = GetMethodByName(instruction.OperandInfo,
                            instruction.OperandInfo.Parameters, genericTypes, instruction.OperandInfo.DeclaringTypeGenericTypes);
                        }
                        
                        token = method.DeclaringType.IsGenericType
                            ? ilInfo.GetTokenFor(method.MethodHandle, method.DeclaringType.TypeHandle)
                            : ilInfo.GetTokenFor(method.MethodHandle);
                        break;
                    }
                    case OperandTypeInfo.Field: {
                        var field = GetFieldByName(instruction.OperandInfo.OperandName, genericTypes, instruction.OperandInfo.DeclaringTypeGenericTypes);
                        
                        token = field.DeclaringType.IsGenericType
                            ? ilInfo.GetTokenFor(field.FieldHandle, field.DeclaringType.TypeHandle)
                            : ilInfo.GetTokenFor(field.FieldHandle);
                        break;
                    }
                    case OperandTypeInfo.String: {
                        token = ilInfo.GetTokenFor(instruction.OperandInfo.OperandName);
                        break;
                    }
                    case OperandTypeInfo.Type: {
                        var type = ConstructGenericType(new TypeInfo {
                            Name = instruction.OperandInfo.OperandName,
                            GenericArguments = Enumerable.Empty<TypeInfo>()
                                .Concat(instruction.OperandInfo.GenericTypes ?? Enumerable.Empty<TypeInfo>())
                                .Concat(instruction.OperandInfo.DeclaringTypeGenericTypes ?? Enumerable.Empty<TypeInfo>()).ToArray()
                        }, genericTypes);
                        
                        token = ilInfo.GetTokenFor(type.TypeHandle);
                        break;
                    }
                }
                
                OverwriteInt32(token, (int)instruction.Offset + instruction.Size, body.IlCode);
                
            }
        }

        private static void OverwriteInt32(int value, int pos, IList<byte> array) {
            array[pos++] = (byte)value;
            array[pos++] = (byte)(value >> 8);
            array[pos++] = (byte)(value >> 16);
            array[pos++] = (byte)(value >> 24);
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

        private static bool CheckIfNeedConstruct(Type type) {
            return type.GetGenericArguments().Any(t => t.IsGenericParameter || (t.IsGenericType && CheckIfNeedConstruct(t)));
        }

        private static Type ConstructGenericType(Type type, IReadOnlyDictionary<string, Type> genericTypes) {
            if (!CheckIfNeedConstruct(type) && !type.IsGenericParameter) {
                return type;
            }

            return type.IsGenericParameter
                ? genericTypes[$"{type.Name}"]
                : GetTypeByName(type.Name).MakeGenericType(type.GetGenericArguments().Select(t => ConstructGenericType(t, genericTypes)).ToArray());
        }

        private static Type ConstructGenericType(TypeInfo type, IReadOnlyDictionary<string, Type> genericTypes) {
            var resultType = GetTypeByName(type.Name);
            if (resultType is { IsGenericTypeDefinition: true }) {
                var genericArguments = resultType.GetGenericArguments();
                var isTotalGenericDefinition = !genericArguments.Where((t, i) => type.GenericArguments[i].Name != $"{t.Namespace}.{t.Name}").Any();
                if (!isTotalGenericDefinition) {
                    resultType = resultType.MakeGenericType(type.GenericArguments
                        .Select(t => ConstructGenericType(t, genericTypes))
                        .ToArray());
                }
            }
            else if (resultType == null) {
                resultType = genericTypes[type.Name[(type.Name.LastIndexOf('.') + 1)..]];
            }

            if (type.IsArray) {
                return resultType.MakeArrayType();
            }
            if (type.IsByRef) {
                return resultType.MakeByRefType();
            }
            if (type.IsPointer) {
                return resultType.MakePointerType();
            }
            return resultType;
        }

        private static Delegate GetGenericMethod(MethodBase methodBase, string name, IReadOnlyDictionary<string, Type> genericTypes, object target) {
            _generationCount++;
            _generationWatch.Start();
            var parameters = methodBase.GetParameters().Select(p => ConstructGenericType(p.ParameterType, genericTypes)).ToList();
            var returnType = methodBase is ConstructorInfo || ((MethodInfo)methodBase).ReturnType == typeof(void)
                ? null
                : ConstructGenericType(((MethodInfo)methodBase).ReturnType, genericTypes);
            var delegateType = CloseDelegateType(
                GetDelegateType(parameters.Count, returnType != null),
                (returnType == null ? parameters : parameters.Concat(new[] { returnType })).ToArray()
                );
            var declaringType = ConstructGenericType(methodBase.DeclaringType, genericTypes);
            if (!methodBase.IsStatic) {
                parameters.Insert(0, declaringType);
            }
            var method = new DynamicMethod(
                methodBase.Name,
                returnType,
                parameters.ToArray(),
                target?.GetType() ?? declaringType,
                false
            );
            var encodedMethodBody = _methods[name] as string;
            var serializedMethodBody = Encoding.ASCII.GetString(Convert.FromBase64String(encodedMethodBody));
            var methodBody = JsonConvert.DeserializeObject<SerializableMethodBody>(serializedMethodBody);
            var ilInfo = method.GetDynamicILInfo();

            var localVarSigHelper = SignatureHelper.GetLocalVarSigHelper();
            foreach (var local in methodBody.LocalVariables) {
                var type = ConstructGenericType(local.Info, genericTypes);
                localVarSigHelper.AddArgument(type, local.IsPinned);
            }

            ilInfo.SetLocalSignature(localVarSigHelper.GetSignature());
            OverwriteTokens(methodBody, ilInfo, genericTypes);
            ilInfo.SetCode(methodBody.IlCode, methodBody.MaxStackSize);
            _generationWatch.Stop();
            return method.CreateDelegate(delegateType, target);
        }

        public static Delegate GetMethod(MethodBase methodBase, string name, Dictionary<string, Type> genericTypes, object target) {

            if (genericTypes != null) {
                return GetGenericMethod(methodBase, name, genericTypes, target);
            }
            if (_methods[name] is (DynamicMethod method, Type delegateType)) {
                return method.CreateDelegate(delegateType, target);
            }

            _generationCount++;
            _generationWatch.Start();
            var parameters = methodBase.GetParameters().Select(p => p.ParameterType).ToList();
            var hasReturnType = methodBase is MethodInfo info && info.ReturnType != typeof(void);
            delegateType = CloseDelegateType(
                GetDelegateType(parameters.Count, hasReturnType),
                (hasReturnType
                    ? parameters.Concat(new[] { ((MethodInfo)methodBase).ReturnType })
                    : parameters).ToArray()
            );

            if (!methodBase.IsStatic) {
                parameters.Insert(0, methodBase.DeclaringType);
            }
            method = new DynamicMethod(
                methodBase.Name,
                hasReturnType ? ((MethodInfo)methodBase).ReturnType : null,
                parameters.ToArray(),
                target?.GetType() ?? methodBase.DeclaringType,
                false
            );
            var encodedMethodBody = _methods[name] as string;
            var serializedMethodBody = Encoding.ASCII.GetString(Convert.FromBase64String(encodedMethodBody));
            var methodBody = JsonConvert.DeserializeObject<SerializableMethodBody>(serializedMethodBody);
            var ilInfo = method.GetDynamicILInfo();

            var localVarSigHelper = SignatureHelper.GetLocalVarSigHelper();
            foreach (var local in methodBody.LocalVariables) {
                var type = ConstructGenericType(local.Info, genericTypes);

                localVarSigHelper.AddArgument(type, local.IsPinned);
            }
            ilInfo.SetLocalSignature(localVarSigHelper.GetSignature());

            OverwriteTokens(methodBody, ilInfo);
            ilInfo.SetCode(methodBody.IlCode, methodBody.MaxStackSize);
            _methods[name] = (method, delegateType);
            _generationWatch.Stop();
            return method.CreateDelegate(delegateType, target);
        }

        private static void LoadAssemblies() {
            foreach (var assemblyName in _referencedAssemblies) {
                Assembly.Load(assemblyName);
            }
        }

        public static void Main(string[] args) {
            _methods = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(@"$PATH_TO_METHODS"));
            _currentAssembly = Assembly.GetExecutingAssembly();
            LoadAssemblies();

            var assembly_name = new AssemblyName("$ASSEMBLY_NAME");
            var assembly_builder = AssemblyBuilder.DefineDynamicAssembly(assembly_name, AssemblyBuilderAccess.Run);
            var module_builder = assembly_builder.DefineDynamicModule(assembly_name.Name + ".dll");

            #region Types
            $TYPES
            #endregion
            #region NestedTypes
            $NESTED_TYPES
            #endregion
            #region Parents
            $PARENTS
            #endregion
            #region GenericConstraintsTypes
            $GENERIC_CONSTRAINTS_TYPES
            #endregion
            #region InterfacesImplementations
            $INTERFACES_IMPLEMENTATIONS
            #endregion
            #region Fields
            $FIELDS
            #endregion
            #region EnumConstants
            $ENUM_CONSTANTS
            #endregion
            #region ConstructorsDefinitions    
            $CONSTRUCTORS_DEFINITIONS
            #endregion
            #region MethodsDefinitions
            $METHODS_DEFINITIONS
            #endregion
            #region GenericConstraintsMethods
            $GENERIC_CONSTRAINTS_METHODS
            #endregion
            #region MethodsParameters
            $METHODS_PARAMETERS
            #endregion
            #region MethodsReturnTypes
            $METHODS_RETURN_TYPES
            #endregion
            #region MethodsOverriding
            $METHODS_OVERRIDING
            #endregion
            #region MethodsBodies    
            $METHODS_BODIES
            #endregion
            #region Properties
            $PROPERTIES
            #endregion
            
            #region CreatedTypes    
            $CREATED_TYPES
            #endregion

            Console.WriteLine($"Generations count: {_generationCount}");
            Console.WriteLine($"Total generations time {_generationWatch.Elapsed.TotalMilliseconds * 1000:F3} us");
        }
    }
}