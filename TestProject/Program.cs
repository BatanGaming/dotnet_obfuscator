using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using CodeGen;
using TestAssembly;

namespace TestProject
{
    public static class Program
    {
        public static void GenerateMethod(Type owner, string name, Type returnType, Type[] parameterTypes) {
            var method1 = new DynamicMethod(name, MethodAttributes.Public,
                CallingConventions.Standard, returnType, parameterTypes, owner, false);
            var generator = method1.GetILGenerator();
            generator.Emit(OpCodes.Ldfld, typeof(Class1).GetField("a"));
            generator.Emit(OpCodes.Ret);
            
        }
        public static void Main(string[] args) {
            /*var assembly = Assembly.LoadFile(@"/Users/batangaming/Desktop/Projects/dotnet_dynamic_assembly_converter/TestProject/bin/Debug/netcoreapp3.1/TestAssembly.dll");
            var generator = new AssemblyGenerator(assembly);
            generator.GenerateAssembly();*/
            var method1 = new DynamicMethod("Method1", MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard, typeof(int), new []{typeof(int)}, typeof(Program), false);
            var generator = method1.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ret);
            //Console.WriteLine(m == null);
        }
    }
}