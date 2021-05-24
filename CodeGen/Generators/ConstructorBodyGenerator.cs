using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Parser;

namespace CodeGen.Generators
{
    public class ConstructorBodyGenerator
    {
        private readonly ConstructorInfo _constructor;
        
        private StringBuilder GenerateLocals() {
            var builder = new StringBuilder();
            var ilGeneratorName = CommonGenerator.ResolveMethodBodyBuilderName(_constructor);
            foreach (var localVariable in _constructor.GetMethodBody().LocalVariables) {
                var code = new LocalVariableGenerator(localVariable).Generate();
                builder.AppendLine($"{ilGeneratorName}.{code};");
            }

            return builder;
        }

        public ConstructorBodyGenerator(ConstructorInfo constructor) {
            _constructor = constructor;
        }

        public string Generate() {
            var ilGeneratorName = CommonGenerator.GenerateMethodBodyGeneratorName(_constructor);
            var builder = GenerateLocals();
            var labels = new Dictionary<long, string>();
            var instructions = new IlParser(_constructor).Parse();
            foreach (var branchInstruction in instructions.Where(instruction =>
                instruction.OpCode.FlowControl == FlowControl.Cond_Branch ||
                instruction.OpCode.FlowControl == FlowControl.Branch)) {
                var labelOffset = branchInstruction.OperandToken!.Value;
                if (labels.ContainsKey(labelOffset)) {
                    continue;
                }
                labels[labelOffset] = $"label_{CommonGenerator.ResolveCustomName(_constructor)}_{labelOffset}";
                builder.AppendLine($@"var {labels[labelOffset]} = {ilGeneratorName}.DefineLabel();");
            }

            foreach (var instruction in instructions) {
                if (labels.ContainsKey(instruction.Offset)) {
                    builder.AppendLine($@"{ilGeneratorName}.MarkLabel({labels[instruction.Offset]});");
                }

                var genericArguments = _constructor.DeclaringType.GetGenericArguments();
                var code = new EmitInstructionGenerator(instruction, _constructor.Module, genericArguments).Generate(labels);
                builder.AppendLine($@"{ilGeneratorName}.{code};");
            }

            return builder.ToString();
        }
    }
}