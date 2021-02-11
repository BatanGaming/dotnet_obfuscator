using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Parser;

namespace CodeGen
{
    public class MethodBodyGenerator
    {
        private readonly MethodInfo _method;

        private StringBuilder GenerateLocals() {
            var builder = new StringBuilder();
            var ilGeneratorName = CommonGenerator.ResolveMethodBodyBuilderName(_method);
            foreach (var localVariable in _method.GetMethodBody().LocalVariables) {
                var code = new LocalVariableGenerator(localVariable).Generate();
                builder.AppendLine($"{ilGeneratorName}.{code};");
            }

            return builder;
        }
        

        public MethodBodyGenerator(MethodInfo method) {
            _method = method;
        }

        public string Generate() {
            var ilGeneratorName = CommonGenerator.GenerateMethodBodyGeneratorName(_method);
            var builder = GenerateLocals();
            var labels = new Dictionary<long, string>();
            var instructions = new IlParser(_method).Parse();
            foreach (var branchInstruction in instructions.Where(instruction =>
                instruction.OpCode.FlowControl == FlowControl.Cond_Branch ||
                instruction.OpCode.FlowControl == FlowControl.Branch)) {
                var labelOffset = branchInstruction.OperandToken!.Value;
                if (labels.ContainsKey(labelOffset)) {
                    continue;
                }
                labels[labelOffset] = $"label_{CommonGenerator.ResolveCustomName(_method)}_{labelOffset}";
                builder.AppendLine($@"var {labels[labelOffset]} = {ilGeneratorName}.DefineLabel();");
            }

            foreach (var instruction in instructions) {
                if (labels.ContainsKey(instruction.Offset)) {
                    builder.AppendLine($@"{ilGeneratorName}.MarkLabel({labels[instruction.Offset]});");
                }

                var code = new EmitInstructionGenerator(instruction, _method.Module).Generate(labels);
                builder.AppendLine($@"{ilGeneratorName}.{code};");
            }

            return builder.ToString();
        }
    }
}