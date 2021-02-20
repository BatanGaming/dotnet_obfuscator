using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using CodeGen;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TestProject
{
    public static class Program
    {
        private static readonly Dictionary<string, object> _methods = new Dictionary<string, object>();
        private static readonly Dictionary<string, Type> _cachedTypes = new Dictionary<string, Type>();
        private static readonly Dictionary<string, MethodInfo> _cachedMethods = new Dictionary<string, MethodInfo>();

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

        private static MethodInfo GetMethodByName(string name, IReadOnlyCollection<string> parametersNames) {
            var index = name.LastIndexOf('.');
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
            method = type.GetMethod(methodName, parametersTypes);
            _cachedMethods[cachedName] = method;
            return method;
        }

        private static FieldInfo GetFieldByName(string name) {
            var index = name.LastIndexOf('.');
            var fieldName = name.Substring(index + 1);
            var typeName = name.Remove(index);
            var type = GetTypeByName(typeName);
            return type.GetField(fieldName);
        }

        private static void OverwriteTokens(SerializableMethodBody body, DynamicILInfo ilInfo) {
            foreach (var instruction in body.Instructions) {
                if (instruction.OperandInfo.OperandType == null) {
                    continue;
                }

                int token;
                switch (instruction.OperandInfo.OperandType) {
                    case OperandTypeInfo.Method:
                    {
                        var method = GetMethodByName(instruction.OperandInfo.OperandName,
                            instruction.OperandInfo.ParametersTypesNames);
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
                OverwriteInt32(token, (int)instruction.Offset + 1, body.IlCode);
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
            return delegateType.MakeGenericType(genericTypes);
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
                methodInfo.DeclaringType, 
                false
                );
            var methodBody = _methods[name] as SerializableMethodBody;
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
        
        public static Delegate GetMethod(object target) {
            var stackTrace = new StackTrace();
            var caller = stackTrace.GetFrame(1).GetMethod();
            var name = $"{caller.DeclaringType.FullName}.{caller.Name}";
            var callerMethodInfo = GetMethodByName(
                name, 
                caller
                    .GetParameters()
                    .Select(p => p.ParameterType.FullName)
                    .ToList()
                );
            return GetMethod(name, callerMethodInfo, target);
        }

        public static void TestGenerate() {
            var testClass = typeof(TestClass);
            foreach (var srcMethod in testClass.GetMethods().Where(m => m.Name.StartsWith("Old"))) {
                var generator = new SerializableMethodBodyGenerator(srcMethod);
                var generatedMethodBody = generator.Generate();
                _methods[$"{testClass.FullName}.{srcMethod.Name.Substring(3)}"] = generatedMethodBody;
            }
        }

        public static void Main(string[] args) {
            var n = int.Parse(args[0]);
            TestGenerate();
            var watch = Stopwatch.StartNew();
            TestClass.TestFactorialMany(n);
            watch.Stop();
            Console.WriteLine($"{watch.ElapsedMilliseconds} ms");
            watch.Reset();
            watch.Start();
            TestClass.FactorialMany(n);
            watch.Stop();
            Console.WriteLine($"{watch.ElapsedMilliseconds} ms");
            /*var testObject = new TestClass();
            
            testObject.FactorialMany(10);*/
            //var m = GetMethod(null);
            /*var testClass = typeof(TestClass);
            var srcMethod = testClass.GetMethod("Sum");
            var a = srcMethod.GetParameters();
            var dynamicMethod = new DynamicMethod("Sum", typeof(int), new[] {typeof(TestClass)}, testClass, false);
            var generator = new SerializableMethodBodyGenerator(srcMethod);
            var result = generator.Generate();
            var body = srcMethod.GetMethodBody();
            var bytes = body.GetILAsByteArray();
            var ilInfo = dynamicMethod.GetDynamicILInfo();
            var localVarSigHelper = SignatureHelper.GetLocalVarSigHelper();
            foreach (var local in body.LocalVariables) {
                localVarSigHelper.AddArgument(local.LocalType, local.IsPinned);
            }
            ilInfo.SetLocalSignature(localVarSigHelper.GetSignature());
            OverwriteTokens(result, ilInfo);
            ilInfo.SetCode(result.IlCode, result.MaxStackSize);
            var testObject = new TestClass {a = 3, b = 4};
            var m = (Func<int>)dynamicMethod.CreateDelegate(typeof(Func<int>), testObject);
            Console.WriteLine(m());*/
        }
    }
}
/*var d = (Func<int>)dynamicMethod.CreateDelegate(typeof(Func<int>), testObject);
Console.WriteLine(d());
Console.WriteLine(testObject.a);/*
var assembly = Assembly.LoadFile(Path.GetFullPath("TestAssembly.dll"));
var generator = new AssemblyGenerator(assembly);
generator.GenerateAssembly();
}
}
}
/*
obj.M(arg1,arg2)
M(obj, arg1, arg2)
top [ arg2, arg1, obj ]
*/

/*
    obj.Method(arg1, ..., argN)
    [argN, ..., arg1, obj, ...] - non-static
    [argN, ..., arg1, ...] - static

    [obj, ...]
    get_field
    [field, ...]
    ld... x(number_of_parameters)
    [argN, ..., arg1, field, ...]
    callvirt field.invoke()
    [...]
    [ arg2, arg1, (field / get_field), obj]
*/