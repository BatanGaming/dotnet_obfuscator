using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;
using CodeGen;

namespace TestProject
{
    public static class Program
    {
        private static readonly Dictionary<MainMethodInfo, object> _methods = new Dictionary<MainMethodInfo, object>();

        private static readonly List<Module> _modules = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.Modules).ToList();
        private static readonly List<OpCode> _opCodes =
            typeof(OpCodes)
                .GetFields()
                .Select(fi => (OpCode)fi.GetValue(null))
                .ToList();

        private static Type ResolveType(int token) {
            Type type = null;
            foreach (var module in _modules) {
                try {
                    var found = module.ResolveType(token);
                    if (type != null) {
                        Console.WriteLine($"Found another type for {type.FullName} is {found.FullName}");
                    }

                    type = found;
                }
                catch {

                }
            }

            return type;
        }

        private static MethodBase ResolveMethod(int token) {
            MethodBase method = null;
            foreach (var module in _modules) {
                try {
                    var found = module.ResolveMethod(token);
                    if (method != null) {
                        Console.WriteLine($"Found another method for {method.Name} is {found.Name}");
                    }

                    method = found;
                }
                catch {

                }
            }

            return method;
        }

        private static FieldInfo ResolveField(int token) {
            FieldInfo field = null;
            foreach (var module in _modules) {
                try {
                    var found = module.ResolveField(token);
                    if (field != null) {
                        Console.WriteLine($"Found another field for {field.Name} is {found.Name}");
                    }

                    field = found;
                }
                catch {

                }
            }

            return field;
        }

        private static string ResolveString(int token) {
            string str = null;
            foreach (var module in _modules) {
                try {
                    var found = module.ResolveString(token);
                    if (str != null) {
                        Console.WriteLine($"Found another string for {str} is {found}");
                    }

                    str = found;
                }
                catch {

                }
            }

            return str;
        }

        private static object ResolveToken(Instruction serializableInstruction) {
            var token = (int)serializableInstruction.OperandToken!.Value;
            return serializableInstruction.OpCode.OperandType switch {
                OperandType.InlineMethod => ResolveMethod(token),
                OperandType.InlineField => ResolveField(token),
                OperandType.InlineSig => throw new NotImplementedException(),
                OperandType.InlineString => ResolveString(token),
                OperandType.InlineType => ResolveType(token),
                OperandType.InlineTok => ResolveType(token) ?? ResolveMethod(token) ?? (object)ResolveField(token),
                var x when
                    x == OperandType.ShortInlineI ||
                    x == OperandType.ShortInlineBrTarget ||
                    x == OperandType.ShortInlineVar
                    => token,
                _ => null
            };
        }

        private static void InjectGettingMethod(SerializableMethodInfo methodInfo, ILGenerator ilGenerator) {
            var dynamicMethodInfoLocal = ilGenerator.DeclareLocal(typeof(MainMethodInfo));
            var dynamicMethodLocal = ilGenerator.DeclareLocal(typeof(DynamicMethod));
            ilGenerator.Emit(OpCodes.Newobj, typeof(MainMethodInfo));
            ilGenerator.Emit(OpCodes.Stloc, dynamicMethodInfoLocal);

            ilGenerator.Emit(OpCodes.Ldloc, dynamicMethodInfoLocal);
            ilGenerator.Emit(OpCodes.Ldstr, methodInfo.Name);
            ilGenerator.Emit(OpCodes.Callvirt, dynamicMethodInfoLocal.LocalType.GetProperty(nameof(MainMethodInfo.Name)).SetMethod);

            ilGenerator.Emit(OpCodes.Ldloc, dynamicMethodInfoLocal);
            ilGenerator.Emit(OpCodes.Ldc_I4, methodInfo.OwnerTypeToken);
            ilGenerator.Emit(OpCodes.Callvirt, dynamicMethodLocal.LocalType.GetProperty(nameof(MainMethodInfo.OwnerTypeToken)).SetMethod);

            ilGenerator.Emit(OpCodes.Ldloc, dynamicMethodInfoLocal);
            ilGenerator.Emit(OpCodes.Ldc_I4, methodInfo.ReturnTypeToken);
            ilGenerator.Emit(OpCodes.Callvirt, dynamicMethodLocal.LocalType.GetProperty(nameof(MainMethodInfo.ReturnTypeToken)).SetMethod);

            ilGenerator.Emit(OpCodes.Ldloc, dynamicMethodInfoLocal);
            ilGenerator.Emit(OpCodes.Ldc_I4, methodInfo.ParameterTypesTokens.Length);
            ilGenerator.Emit(OpCodes.Newarr, typeof(int));
            // can we just push array and call setter?
            for (var i = 0; i < methodInfo.ParameterTypesTokens.Length; ++i) {
                ilGenerator.Emit(OpCodes.Dup);
                ilGenerator.Emit(OpCodes.Ldc_I4, i);
                ilGenerator.Emit(OpCodes.Ldc_I4, methodInfo.ParameterTypesTokens[i]);
                ilGenerator.Emit(OpCodes.Stelem_I4);
            }
            ilGenerator.Emit(OpCodes.Callvirt, dynamicMethodInfoLocal.LocalType.GetProperty(nameof(methodInfo.ParameterTypesTokens)).SetMethod);

            ilGenerator.Emit(OpCodes.Ldloc, dynamicMethodInfoLocal);
            ilGenerator.Emit(OpCodes.Callvirt, typeof(Program).GetMethod("GetMethod", new[] { typeof(MainMethodInfo) }));
            ilGenerator.Emit(OpCodes.Stloc, dynamicMethodLocal);
            ilGenerator.Emit(OpCodes.Ldloc, dynamicMethodLocal);

        }

        public static DynamicMethod GenerateMethod(SerializableMethodInfo methodInfo) {
            var dynamicMethod = new DynamicMethod(
                methodInfo.Name,
                methodInfo.Attributes,
                methodInfo.CallingConventions,
                ResolveType(methodInfo.ReturnTypeToken),
                methodInfo.ParameterTypesTokens == null
                    ? Type.EmptyTypes 
                    : methodInfo.ParameterTypesTokens.Select(ResolveType).ToArray(),
                typeof(Program),
                false
                );

            var ilGenerator = dynamicMethod.GetILGenerator();
            foreach (var localToken in methodInfo.LocalVariablesTypesTokens) {
                ilGenerator.DeclareLocal(ResolveType(localToken));
            }
            var labels = new Dictionary<long, Label>();
            var instructions = methodInfo.Instructions
                .Select(instruction =>
                    new Instruction(_opCodes.First(opCode => opCode.Value == instruction.OpCodeValue),
                        instruction.OperandToken, instruction.Offset)).ToList();
            foreach (var instruction in instructions.Where(instruction =>
                instruction.OpCode.FlowControl == FlowControl.Cond_Branch ||
                instruction.OpCode.FlowControl == FlowControl.Branch)) {
                if (labels.ContainsKey(instruction.Offset)) {
                    continue;
                }

                labels[instruction.Offset] = ilGenerator.DefineLabel();
            }

            foreach (var instruction in instructions) {
                if (labels.ContainsKey(instruction.Offset)) {
                    ilGenerator.MarkLabel(labels[instruction.Offset]);
                }

                if (instruction.OperandToken == null) {
                    ilGenerator.Emit(instruction.OpCode);
                    continue;
                }
                var operand = ResolveToken(instruction);
                if (methodInfo.CustomMethodsOffsets.Contains(instruction.Offset) && instruction.OpCode == OpCodes.Call && operand is MethodBase m) {
                    var declaringType = m.DeclaringType;
                    var method = declaringType.GetMethod(m.Name, m.GetParameters().Select(m => m.ParameterType).ToArray());
                    var subMethodInfo = new MainMethodInfo {
                        OwnerTypeToken = declaringType.MetadataToken,
                        ReturnTypeToken = method.ReturnType.MetadataToken,
                        Name = method.Name,
                        ParameterTypesTokens = method.GetParameters().Select(m => m.ParameterType.MetadataToken).ToArray()
                    };
                }
                var exactMethod = typeof(ILGenerator).GetMethod("Emit", new[] {typeof(OpCode), operand.GetType()});
                exactMethod.Invoke(ilGenerator, new[] {instruction.OpCode, operand});
            }
            return dynamicMethod;
        }

        public static DynamicMethod GetMethod(MainMethodInfo methodInfo) {
            if (_methods[methodInfo] is DynamicMethod method) {
                return method;
            }

            var json = (string) _methods[methodInfo];
            var deserializedMethodInfo = JsonSerializer.Deserialize<SerializableMethodInfo>(json);
            method = GenerateMethod(deserializedMethodInfo);
            _methods[methodInfo] = method;

            return method;
        }

        public static void Main(string[] args) {
            var mainInfo = new MainMethodInfo {
                OwnerTypeToken = typeof(Program).MetadataToken,
                ReturnTypeToken = typeof(int).MetadataToken,
                ParameterTypesTokens = new[] { typeof(int).MetadataToken, typeof(int).MetadataToken },
                Name = "method1"
            };
            var instructions = new List<SerializableInstruction> {
                new SerializableInstruction{OpCodeValue=0x02, OperandToken = null, Offset = 0},
                new SerializableInstruction{OpCodeValue=0x03, OperandToken = null, Offset = 1},
                new SerializableInstruction{OpCodeValue = 0x58, OperandToken = null, Offset = 2},
                new SerializableInstruction{OpCodeValue = 0x2A, OperandToken = null, Offset = 3},
            };
            var serializableMethodInfo = new SerializableMethodInfo {
                Attributes = MethodAttributes.Static | MethodAttributes.Public,
                CallingConventions = CallingConventions.Standard,
                Instructions = instructions,
                Name = "method1",
                OwnerTypeToken = typeof(Program).MetadataToken,
                ReturnTypeToken = typeof(int).MetadataToken,
                ParameterTypesTokens = new[] {typeof(int).MetadataToken, typeof(int).MetadataToken}
            };
            _methods[mainInfo] = JsonSerializer.Serialize(serializableMethodInfo);
            //var m = GetMethod<A>(mainInfo, null);
            /*var assembly = Assembly.LoadFile(@"D:\Projects\Packer\TestProject\bin\Debug\netcoreapp3.1\TestAssembly.dll");
            var generator = new AssemblyGenerator(assembly);
            generator.GenerateAssembly();*/
        }
    }
}