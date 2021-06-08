using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Newtonsoft.Json;

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
            public TypeInfo[] Parameters { get; set; }
            public TypeInfo[] GenericTypes { get; set; }
            public TypeInfo[] DeclaringTypeGenericTypes { get; set; }
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
            public TypeInfo Info { get; set; }
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
        
        [Serializable]
        public class TypeInfo
        {
            public string Name { get; set; }
            public TypeInfo[] GenericArguments { get; set; }
            public bool IsByRef { get; set; }
            public bool IsPointer { get; set; }
        }
        
        private static Assembly _currentAssembly;
        
        private static readonly Dictionary<string, object> _methods = new Dictionary<string, object> {
            $SERIALIZED_METHODS
        };

        private static readonly List<string> _referencedAssemblies = new List<string> {
            $REFERENCED_ASSEMBLIES
        };

        private static Type GetTypeByName(string name) {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies.Except(new[] { _currentAssembly })) {
                var foundedType = assembly.GetType(name);
                if (foundedType != null) {
                    return foundedType;
                }
            }

            if (name.Contains("+")) {
                var types = name.Split("+");
                var root = GetTypeByName(types[0]);
                var currentType = root;
                var currentTypeName = types[0];
                for (var i = 1; i < types.Length; ++i) {
                    currentTypeName = $"{currentTypeName}+{types[i]}";
                    currentType = currentType.GetNestedType(currentTypeName, BindingFlags.Public | BindingFlags.NonPublic);
                }

                return currentType;
            }
            return null;
        }

        private static MethodBase GetMethodByName(OperandInfo info, IReadOnlyCollection<TypeInfo> parameters, IReadOnlyDictionary<string, Type> genericTypes, IEnumerable<TypeInfo> genericArguments) {
            var index = info.OperandName.LastIndexOf('#');
            var methodName = info.OperandName.Substring(index + 1);
            var cachedName = $"{info.OperandName}({string.Join(',', parameters)})";
            var typeName = info.OperandName.Remove(index);
            var type = GetTypeByName(typeName);
            if (type == null) {
                type = ConstructGenericType(new TypeInfo {
                    Name = info.OperandName[..(info.OperandName.IndexOf('['))],
                    GenericArguments = Enumerable.Empty<TypeInfo>()
                        .Concat(info.GenericTypes ?? Enumerable.Empty<TypeInfo>())
                        .Concat(info.DeclaringTypeGenericTypes ?? Enumerable.Empty<TypeInfo>()).ToArray()
                }, genericTypes);
            }
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
#if DEBUG
                            Console.WriteLine($"Resolved method {instruction.OperandInfo.OperandName} into {method.DeclaringType.FullName}.{method.Name}");
#endif
                            token = method.DeclaringType.IsGenericType
                                ? ilInfo.GetTokenFor(method.MethodHandle, method.DeclaringType.TypeHandle)
                                : ilInfo.GetTokenFor(method.MethodHandle);
                            break;
                        }
                    case OperandTypeInfo.Field: {
                            var field = GetFieldByName(instruction.OperandInfo.OperandName, genericTypes, instruction.OperandInfo.DeclaringTypeGenericTypes);
#if DEBUG
                            Console.WriteLine($"Resolved field {instruction.OperandInfo.OperandName} into {field.DeclaringType.FullName}.{field.Name}");
#endif
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
                            Type type = null;
                            if (genericTypes != null && genericTypes.ContainsKey(instruction.OperandInfo.OperandName)) {
                                type = genericTypes[instruction.OperandInfo.OperandName];
                            }
                            else {
                                type = ConstructGenericType(new TypeInfo {
                                    Name = instruction.OperandInfo.OperandName,
                                    GenericArguments = Enumerable.Empty<TypeInfo>()
                                        .Concat(instruction.OperandInfo.GenericTypes ?? Enumerable.Empty<TypeInfo>())
                                        .Concat(instruction.OperandInfo.DeclaringTypeGenericTypes ?? Enumerable.Empty<TypeInfo>()).ToArray()
                                }, genericTypes);
                            }
#if DEBUG
                            Console.WriteLine($"Resolved type {instruction.OperandInfo.OperandName} into {type.FullName ?? type.Name}");
#endif
                            token = ilInfo.GetTokenFor(type.TypeHandle);
                            break;
                        }
                }
#if DEBUG
                Console.WriteLine($"Overwriting at 0x{((int)instruction.Offset + instruction.Size):X2}");
#endif
                OverwriteInt32(token, (int)instruction.Offset + instruction.Size, body.IlCode);
#if DEBUG
                Console.WriteLine();
#endif
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
                ? genericTypes[$"{type.Namespace}.{type.Name}"]
                : GetTypeByName($"{type.Namespace}.{type.Name}").MakeGenericType(type.GetGenericArguments().Select(t => ConstructGenericType(t, genericTypes)).ToArray());
        }

        private static Type ConstructGenericType(TypeInfo type, IReadOnlyDictionary<string, Type> genericTypes) {
            var resultType = GetTypeByName(type.Name);
            if (resultType is { IsGenericTypeDefinition: false }) {
                goto end;
            }
            if (type.GenericArguments == null || type.GenericArguments.Length == 0) {
                resultType = genericTypes[type.Name];
                goto end;
            }

            if (resultType == null) {
                type.Name = type.Name[..type.Name.IndexOf('[')];
                resultType = ConstructGenericType(type, genericTypes);
            }
            else {
                resultType = resultType.MakeGenericType(type.GenericArguments.Select(t => ConstructGenericType(t, genericTypes)).ToArray());
            }
        end:
            if (type.IsByRef) {
                return resultType.MakeByRefType();
            }
            if (type.IsPointer) {
                return resultType.MakePointerType();
            }
            return resultType;
        }

        private static string GetFullName(Type type) {
            if (type.FullName != null) {
                return type.FullName;
            }

            if (type.IsGenericParameter) {
                return type.Name;
            }

            return $"{type.Namespace}.{type.Name} {string.Join(',', type.GetGenericArguments().Select(GetFullName))}";
        }

        private static Delegate GetGenericMethod(MethodBase methodBase, IReadOnlyDictionary<string, Type> genericTypes, object target) {
            var methodName = $"{methodBase.DeclaringType.FullName}#{methodBase.Name} {string.Join(',', methodBase.GetParameters().Select(p => GetFullName(p.ParameterType)))}";
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
            var encodedMethodBody = _methods[methodName] as string;
            var serializedMethodBody = Encoding.ASCII.GetString(Convert.FromBase64String(encodedMethodBody));
            var methodBody = JsonConvert.DeserializeObject<SerializableMethodBody>(serializedMethodBody);
            var ilInfo = method.GetDynamicILInfo();

            var localVarSigHelper = SignatureHelper.GetLocalVarSigHelper();
            foreach (var local in methodBody.LocalVariables) {
                var type = ConstructGenericType(local.Info, genericTypes);
                localVarSigHelper.AddArgument(type, local.IsPinned);
            }

            ilInfo.SetLocalSignature(localVarSigHelper.GetSignature());
#if DEBUG
            Console.WriteLine($"[{string.Join(',', methodBody.IlCode.Select(b => $"0x{b:X2}"))}]");
#endif
            OverwriteTokens(methodBody, ilInfo, genericTypes);
            ilInfo.SetCode(methodBody.IlCode, methodBody.MaxStackSize);
            return method.CreateDelegate(delegateType, target);
        }

        public static Delegate GetMethod(MethodBase methodBase, Dictionary<string, Type> genericTypes, object target) {
#if DEBUG
            Console.WriteLine($"Generating method {target?.GetType().FullName ?? methodBase.DeclaringType.FullName}.{methodBase.Name}");
#endif
            if (genericTypes != null) {
                return GetGenericMethod(methodBase, genericTypes, target);
            }
            var methodName = $"{methodBase.DeclaringType.FullName}#{methodBase.Name} {string.Join(',', methodBase.GetParameters().Select(p => GetFullName(p.ParameterType)))}";
            Console.WriteLine(methodName);
            if (!_methods.ContainsKey(methodName)) {
                methodName = $"{methodBase.DeclaringType.Namespace}.{methodBase.DeclaringType.Name.Replace(@"\", "")}#{methodBase.Name} {string.Join(',', methodBase.GetParameters().Select(p => GetFullName(p.ParameterType)))}";
            }
            if (_methods[methodName] is (DynamicMethod method, Type delegateType)) {
                return method.CreateDelegate(delegateType, target);
            }

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
            var encodedMethodBody = _methods[methodName] as string;
            var serializedMethodBody = Encoding.ASCII.GetString(Convert.FromBase64String(encodedMethodBody));
            var methodBody = JsonConvert.DeserializeObject<SerializableMethodBody>(serializedMethodBody);
            var ilInfo = method.GetDynamicILInfo();

            var localVarSigHelper = SignatureHelper.GetLocalVarSigHelper();
            foreach (var local in methodBody.LocalVariables) {
                var type = ConstructGenericType(local.Info, genericTypes);
#if DEBUG
                Console.WriteLine($"Resolved {local.Info.Name} into {type.FullName}");
#endif
                localVarSigHelper.AddArgument(type, local.IsPinned);
            }
            ilInfo.SetLocalSignature(localVarSigHelper.GetSignature());

#if DEBUG
            for (var i = 0; i < methodBody.IlCode.Length; ++i) {
                Console.WriteLine($"0x{methodBody.IlCode[i]:X2} - {i}");
            }
#endif

            OverwriteTokens(methodBody, ilInfo);
            ilInfo.SetCode(methodBody.IlCode, methodBody.MaxStackSize);
            _methods[methodName] = (method, delegateType);
#if DEBUG
            Console.WriteLine();
#endif
            return method.CreateDelegate(delegateType, target);
        }

        private static void LoadAssemblies() {
            foreach (var assemblyName in _referencedAssemblies) {
                Assembly.Load(assemblyName);
            }
        }

        public static void Main(string[] args) {
            _currentAssembly = Assembly.GetExecutingAssembly();
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
                
            $METHODS_DEFINITIONS

            $GENERIC_CONSTRAINTS_METHODS
                
            $METHODS_PARAMETERS
                
            $METHODS_RETURN_TYPES
                
            $METHODS_OVERRIDING
                
            $METHODS_BODIES
                
            $PROPERTIES
                
            $CREATED_TYPES
        }
    }
}