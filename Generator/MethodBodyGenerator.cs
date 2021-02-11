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

        private static string FixOpCodeName(string name) {
            if (name == "constrained.") {
                return "Constrained";
            }
            return string.Join('_',
                from substr in name.Split('.') 
                select char.ToUpper(substr[0]) + substr.Substring(1)
                );
        }

        private static IEnumerable<string> DeclareLocals(string ilGeneratorName, IEnumerable<LocalVariableInfo> vars, Func<object,string> objectResolver) {
            return from variable in vars
                let type = $"{objectResolver(variable.LocalType) ?? $"typeof({GenericConverter.ConvertGenericName(variable.LocalType, objectResolver)})"}"
                let isPinned = variable.IsPinned.ToString().ToLower()
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
            }

            return builder.ToString();
        }
    }
}
