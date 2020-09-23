using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Generator
{
    internal class MethodBodyGenerator
    {
        private readonly MethodBase _method;

        private static string StringifyInstruction(object operandObject, string operandName) {
            switch (operandObject) {
                case string str:
                    return $@"""{operandName ?? str}""";
                case Type type:
                    return operandName ?? $"typeof({type.FullName})";
                case FieldInfo field:
                    return operandName ?? $"typeof({field.FieldType})";
                case MethodBase method:
                {
                    var parameters = method.GetParameters();

                    return operandName ?? $@"typeof({method.DeclaringType?.FullName})." +
                        $@"{(method.IsConstructor ? "GetConstructor(" : @$"GetMethod(""{method.Name}"",")}" +
                        $@"{(parameters.Length == 0 ? "Type.EmptyTypes" : $@" new [] {{{string.Join(',', from parameter in parameters select $"typeof({parameter.ParameterType.FullName})")}}}")})";

                }
                default:
                    return operandObject.ToString();
            }
        }

        private string FixOpCodeName(string name) {
            return string.Join('_',
                from substr in name.Split('.') 
                select char.ToUpper(substr[0]) + substr.Substring(1)
                );
        }

        private static IEnumerable<string> DeclareLocals(string ilGeneratorName, IEnumerable<LocalVariableInfo> vars, Func<object,string> objectResolver) {
            return from variable in vars
                let type = $"{objectResolver(variable.LocalType) ?? $"typeof({variable.LocalType.FullName})"}"
                let isPinnedStr = variable.IsPinned.ToString()
                let isPinned = char.ToLower(isPinnedStr[0]) + isPinnedStr.Substring(1)
                select $"{ilGeneratorName}.DeclareLocal({type}, {isPinned});";
        }

        public MethodBodyGenerator(MethodBase method) {
            _method = method;
        }

        public string Generate(string methodVarName, Func<object, string> objectResolver) {
            var ilBuilderVarName = $"il_{methodVarName}_builder";
            var builder = new StringBuilder();
            builder.AppendLine($@"var {ilBuilderVarName} = {methodVarName}.GetILGenerator();");
            var body = _method.GetMethodBody();
            foreach (var localDefinition in DeclareLocals(ilBuilderVarName, body.LocalVariables, objectResolver)) {
                builder.AppendLine(localDefinition);
            }
            var labels = new Dictionary<long, string>();
            var instructions = new IlParser(_method).Parse();
            foreach (var branchInstruction in instructions.Where(instruction =>
                instruction.OpCode.FlowControl == FlowControl.Cond_Branch ||
                instruction.OpCode.FlowControl == FlowControl.Branch)) {
                var labelOffset = (long)branchInstruction.Operand;
                if (labels.ContainsKey(labelOffset)) {
                    continue;
                }
                labels[labelOffset] = $"label_{methodVarName}_{labelOffset}";
                builder.AppendLine(
                    $@"var {labels[labelOffset]} = {ilBuilderVarName}.DefineLabel();");
            }

            foreach (var instruction in instructions) {
                if (labels.ContainsKey(instruction.Offset)) {
                    builder.AppendLine($@"{ilBuilderVarName}.MarkLabel({labels[instruction.Offset]});");
                }

                builder.Append($@"{ilBuilderVarName}.Emit(OpCodes.{FixOpCodeName(instruction.OpCode.Name)}");
                if (instruction.Operand != null) {
                    builder.Append(',');
                    if (instruction.OpCode.FlowControl == FlowControl.Cond_Branch ||
                        instruction.OpCode.FlowControl == FlowControl.Branch) {
                        builder.Append(labels[(long) instruction.Operand]);
                    }
                    else {
                        builder.Append(StringifyInstruction(instruction.Operand, objectResolver(instruction.Operand)));
                    }
                }
                builder.AppendLine(");");
            }

            return builder.ToString();
        }
    }
}
