using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;

namespace $NAMESPACE
{
    public static class Program
    {
        private static readonly Dictionary<MainMethodInfo, object> _methods = new Dictionary<MainMethodInfo, object> {
            $METHODS_BODIES_JIT
        };

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

        private static DynamicMethod GenerateMethod(SerializableMethodInfo methodInfo) {
            var method = new DynamicMethod(
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
            var ilGenerator = method.GetILGenerator();
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
                var exactMethod = typeof(ILGenerator).GetMethod("Emit", new[] { typeof(OpCode), operand.GetType() });
                exactMethod.Invoke(ilGenerator, new[] { instruction.OpCode, operand });
            }
            return method;
        }

        public static DynamicMethod GetMethod(MainMethodInfo methodInfo) {
            if (_methods[methodInfo] is DynamicMethod method) {
                return method;
            }

            var json = (string)_methods[methodInfo];
            var deserializedMethodInfo = JsonSerializer.Deserialize<SerializableMethodInfo>(json);
            method = GenerateMethod(deserializedMethodInfo);
            _methods[methodInfo] = method;

            return method;
        }

        public static void Main(string[] args) {
            // MAIN PART START

            var assembly_name = new AssemblyName("$ASSEMBLY_NAME");
            var assembly_builder = AssemblyBuilder.DefineDynamicAssembly(assembly_name, AssemblyBuilderAccess.Run);
            var module_builder = assembly_builder.DefineDynamicModule(assembly_name.Name + ".dll");

            // MAIN PART END
            
            // TYPES START

            $TYPES

            // TYPES END
            
            // FIELDS START
            
            $FIELDS
            
            // FIELDS END

            // METHODS DEFINITIONS START
            
            $METHODS_DEFINITIONS
            
            // METHODS DEFINITIONS END
            
            // METHODS BODIES START
            
            $METHODS_BODIES
            
            // METHODS BODIES END
        }
    }
}