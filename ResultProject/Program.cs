using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;

namespace ResultProject
{
    public static class Program
{
    public static void Main(string[] args) {
        // MAIN PART START

        var assembly_name = new AssemblyName("$ASSEMBLY_NAME");
        var assembly_builder = AssemblyBuilder.DefineDynamicAssembly(assembly_name, AssemblyBuilderAccess.Run);
        var module_builder = assembly_builder.DefineDynamicModule(assembly_name.Name + ".dll");

        // MAIN PART END

        // TYPES START

        var type_Class1_builder = module_builder.DefineType(
            "Class1",
            System.Reflection.TypeAttributes.AutoLayout | System.Reflection.TypeAttributes.AnsiClass | System.Reflection.TypeAttributes.Class | System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.BeforeFieldInit,
            typeof(System.Object));
        var type_Class2_builder = module_builder.DefineType(
                        "Class2",
                        System.Reflection.TypeAttributes.AutoLayout | System.Reflection.TypeAttributes.AnsiClass | System.Reflection.TypeAttributes.Class | System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.BeforeFieldInit,
                        typeof(System.Object));


        // TYPES END

        // FIELDS START



        // FIELDS END

        // METHODS DEFINITIONS START

        var type_Class1_method_Factorial_builder = type_Class1_builder.DefineMethod(
                    "Factorial",
                    System.Reflection.MethodAttributes.PrivateScope | System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Static | System.Reflection.MethodAttributes.HideBySig,
                    typeof(int),
                    new[] { typeof(int) }
                    );
        var type_Class1_method_FactorialMany_builder = type_Class1_builder.DefineMethod(
                                "FactorialMany",
                                System.Reflection.MethodAttributes.PrivateScope | System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.HideBySig,
                                typeof(void),
                                Type.EmptyTypes
                                );


        // METHODS DEFINITIONS END

        // METHODS BODIES START

        var type_Class1_il_method_Factorial_builder = type_Class1_method_Factorial_builder.GetILGenerator();
        var label_type_Class1_method_Factorial_builder_15 = type_Class1_il_method_Factorial_builder.DefineLabel();
        type_Class1_il_method_Factorial_builder.Emit(OpCodes.Ldarg_0);
        type_Class1_il_method_Factorial_builder.Emit(OpCodes.Ldc_I4_1);
        type_Class1_il_method_Factorial_builder.Emit(OpCodes.Beq_S, label_type_Class1_method_Factorial_builder_15);
        type_Class1_il_method_Factorial_builder.Emit(OpCodes.Ldarg_0);
        type_Class1_il_method_Factorial_builder.Emit(OpCodes.Ldarg_0);
        type_Class1_il_method_Factorial_builder.Emit(OpCodes.Ldc_I4_1);
        type_Class1_il_method_Factorial_builder.Emit(OpCodes.Sub);
        type_Class1_il_method_Factorial_builder.Emit(OpCodes.Call, type_Class1_method_Factorial_builder);
        type_Class1_il_method_Factorial_builder.Emit(OpCodes.Mul);
        type_Class1_il_method_Factorial_builder.Emit(OpCodes.Ret);
        type_Class1_il_method_Factorial_builder.MarkLabel(label_type_Class1_method_Factorial_builder_15);
        type_Class1_il_method_Factorial_builder.Emit(OpCodes.Ldc_I4_1);
        type_Class1_il_method_Factorial_builder.Emit(OpCodes.Ret);

        var type_Class1_il_method_FactorialMany_builder = type_Class1_method_FactorialMany_builder.GetILGenerator();
        type_Class1_il_method_FactorialMany_builder.DeclareLocal(typeof(System.Collections.Generic.List<int>.Enumerator), false);
        var label_type_Class1_method_FactorialMany_builder_102 = type_Class1_il_method_FactorialMany_builder.DefineLabel();
        var label_type_Class1_method_FactorialMany_builder_85 = type_Class1_il_method_FactorialMany_builder.DefineLabel();
        var label_type_Class1_method_FactorialMany_builder_127 = type_Class1_il_method_FactorialMany_builder.DefineLabel();
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Newobj, typeof(System.Collections.Generic.List<int>).GetConstructor(
                                   Type.EmptyTypes));
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Dup);
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Ldc_I4_1);
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Callvirt, typeof(System.Collections.Generic.List<int>).GetMethod("Add",
                                   new[] { typeof(int) }));
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Dup);
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Ldc_I4_2);
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Callvirt, typeof(System.Collections.Generic.List<int>).GetMethod("Add",
                                   new[] { typeof(int) }));
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Dup);
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Ldc_I4_3);
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Callvirt, typeof(System.Collections.Generic.List<int>).GetMethod("Add",
                                   new[] { typeof(int) }));
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Dup);
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Ldc_I4_4);
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Callvirt, typeof(System.Collections.Generic.List<int>).GetMethod("Add",
                                   new[] { typeof(int) }));
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Dup);
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Ldc_I4_5);
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Callvirt, typeof(System.Collections.Generic.List<int>).GetMethod("Add",
                                   new[] { typeof(int) }));
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Dup);
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Ldc_I4_6);
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Callvirt, typeof(System.Collections.Generic.List<int>).GetMethod("Add",
                                   new[] { typeof(int) }));
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Dup);
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Ldc_I4_7);
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Callvirt, typeof(System.Collections.Generic.List<int>).GetMethod("Add",
                                   new[] { typeof(int) }));
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Dup);
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Ldc_I4_8);
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Callvirt, typeof(System.Collections.Generic.List<int>).GetMethod("Add",
                                   new[] { typeof(int) }));
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Dup);
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Ldc_I4_S, 9);
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Callvirt, typeof(System.Collections.Generic.List<int>).GetMethod("Add",
                                   new[] { typeof(int) }));
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Dup);
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Ldc_I4_S, 10);
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Callvirt, typeof(System.Collections.Generic.List<int>).GetMethod("Add",
                                   new[] { typeof(int) }));
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Callvirt, typeof(System.Collections.Generic.List<int>).GetMethod("GetEnumerator",
                                   Type.EmptyTypes));
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Stloc_0);
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Br_S, label_type_Class1_method_FactorialMany_builder_102);
        type_Class1_il_method_FactorialMany_builder.MarkLabel(label_type_Class1_method_FactorialMany_builder_85);
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Ldloca_S, 0);
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Call, typeof(System.Collections.Generic.List<int>.Enumerator).GetProperty("Current").GetMethod);
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Call, type_Class1_method_Factorial_builder);
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Call, typeof(System.Console).GetMethod("WriteLine",
                                   new[] { typeof(int) }));
        type_Class1_il_method_FactorialMany_builder.MarkLabel(label_type_Class1_method_FactorialMany_builder_102);
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Ldloca_S, 0);
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Call, typeof(System.Collections.Generic.List<int>.Enumerator).GetMethod("MoveNext",
                                   Type.EmptyTypes));
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Brtrue_S, label_type_Class1_method_FactorialMany_builder_85);
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Leave_S, label_type_Class1_method_FactorialMany_builder_127);
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Ldloca_S, 0);
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Constrained, typeof(System.Collections.Generic.List<int>.Enumerator));
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Callvirt, typeof(System.IDisposable).GetMethod("Dispose",
                                   Type.EmptyTypes));
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Endfinally);
        type_Class1_il_method_FactorialMany_builder.MarkLabel(label_type_Class1_method_FactorialMany_builder_127);
        type_Class1_il_method_FactorialMany_builder.Emit(OpCodes.Ret);



        // METHODS BODIES END

        // CREATING TYPES

        var type_Class1 = type_Class1_builder.CreateType();
        var type_Class2 = type_Class2_builder.CreateType();
        var obj = Activator.CreateInstance(type_Class1);
        type_Class1.GetMethod("FactorialMany").Invoke(obj, null);
    }
}
}