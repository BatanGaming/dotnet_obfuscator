using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using CodeGen;

namespace TestProject
{
    public static class Program
    {
        public static void OverwriteInt32(int value, int pos, byte[] array) {
            array[pos++] = (byte) value;
            array[pos++] = (byte) (value >> 8);
            array[pos++] = (byte) (value >> 16);
            array[pos++] = (byte) (value >> 24);
        }

        public static void Main(string[] args) {
            var testClass = typeof(TestClass);
            var srcMethod = testClass.GetMethod("Sum");

            var dynamicMethod = new DynamicMethod("Sum", typeof(int), new[] {typeof(TestClass)}, testClass, false);
            /*using (var ilGenerator = new GroboIL(dynamicMethod)) {
                var local = ilGenerator.DeclareLocal(typeof(int));
                ilGenerator.Ldarg(0);
                ilGenerator.Ldfld(testClass.GetField("a"));
                ilGenerator.Ldarg(0);
                ilGenerator.Ldfld(testClass.GetField("b"));
                ilGenerator.Add();
                ilGenerator.Stloc(local);
                ilGenerator.Ldarg(0);
                ilGenerator.Ldloc(local);
                ilGenerator.Stfld(testClass.GetField("a"));
                ilGenerator.Ldloc(local);
                ilGenerator.Ret();
            }*/
            var body = srcMethod.GetMethodBody();
            var bytes = body.GetILAsByteArray();
            var localVarSigHelper = SignatureHelper.GetLocalVarSigHelper();
            foreach (var local in body.LocalVariables) {
                localVarSigHelper.AddArgument(local.LocalType, local.IsPinned);
            }
            var ilInfo = dynamicMethod.GetDynamicILInfo();
            var token1 = ilInfo.GetTokenFor(testClass.GetField("a").FieldHandle);
            var token2 = ilInfo.GetTokenFor(testClass.GetField("b").FieldHandle);
            var token3 = ilInfo.GetTokenFor(testClass.GetMethod("Method").MethodHandle);
            //OverwriteInt32(token3, 3, bytes);
            OverwriteInt32(token1, 10, bytes);
            OverwriteInt32(token2, 16, bytes);
            ilInfo.SetLocalSignature(localVarSigHelper.GetSignature());
            ilInfo.SetCode(bytes, body.MaxStackSize);
            var testObject = new TestClass {a = 3, b = 4};
            var m = (Func<int>)dynamicMethod.CreateDelegate(typeof(Func<int>), testObject);
            Console.WriteLine(m());
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