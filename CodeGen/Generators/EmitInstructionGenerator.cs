using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CodeGen.Extensions;
using Parser;

namespace CodeGen.Generators
{
    public class EmitInstructionGenerator
    {
        private readonly Instruction _instruction;
        private readonly Module _module;
        
        private static string FixOpCodeName(string name) {
            if (name == "constrained.") {
                return "Constrained";
            }
            return string.Join('_',
                from substr in name.Split('.') 
                select char.ToUpper(substr[0]) + substr.Substring(1)
            );
        }

        private object SafeResolveToken(int token) {
            object result = null;
            try {
                result = _module.ResolveType(token);
            }
            catch (ArgumentException) { }
            if (result != null) {
                return result;
            }
            
            try {
                result = _module.ResolveMethod(token);
            }
            catch (ArgumentException) { }
            if (result != null) {
                return result;
            }
            
            try {
                _module.ResolveField(token);
            }
            catch (ArgumentException) { }

            return result;
        }

        private object ResolveToken() {
            var token = (int) _instruction.OperandToken!.Value;
            return _instruction.OpCode.OperandType switch
            {
                OperandType.InlineMethod => _module.ResolveMethod(token),
                OperandType.InlineField => _module.ResolveField(token),
                OperandType.InlineSig => _module.ResolveSignature(token),
                OperandType.InlineString => _module.ResolveString(token),
                OperandType.InlineType => _module.ResolveType(token),
                OperandType.InlineTok => SafeResolveToken(token),
                var x when
                    x == OperandType.ShortInlineI ||
                    x == OperandType.ShortInlineBrTarget ||
                    x == OperandType.ShortInlineVar
                    => token,
                _ => null
            };
        }

        private static IEnumerable<string> StringifyMethodParameters(IEnumerable<ParameterInfo> parameters) {
            return from parameter in parameters 
                select CommonGenerator.ResolveTypeName(parameter.ParameterType);
        }

        private static string StringifyGetMethod(MethodBase method) {
            return method.IsConstructor 
                ? "GetConstructor("
                : $@"GetMethod(""{method.Name}"",";
        }

        private string GetParameter(IReadOnlyDictionary<long, string> labels) {
            if (_instruction.OpCode.FlowControl == FlowControl.Cond_Branch ||
                _instruction.OpCode.FlowControl == FlowControl.Branch) {
                return labels[_instruction.OperandToken!.Value];
            }

            var operand = ResolveToken();
            if (operand == null) {
                Console.WriteLine($"Instruction offset {_instruction.Offset}, opcode {_instruction.OpCode.Name}, token = {_instruction.OperandToken.Value}");
            }
            switch (operand) {
                case string str:
                    return $@"""{str}""";
                case Type type:
                    return CommonGenerator.ResolveTypeName(type);
                case FieldInfo field:
                    return CommonGenerator.ResolveCustomName(field);
                case MethodBase method:
                {
                    var customName = CommonGenerator.ResolveCustomName(method);
                    if (customName != null) {
                        return customName;
                    }
                    if (method.IsSpecialName && !method.IsConstructor) {

                        var specialName = method.Name.Split('_');
                        return $@"{CommonGenerator.ResolveTypeName(method.DeclaringType)}.GetProperty(""{specialName[1]}"").{specialName[0].Capitalize()}Method";
                    }
                    var parameters = method.GetParameters();
                    return $@"{CommonGenerator.ResolveTypeName(method.DeclaringType)}.{StringifyGetMethod(method)}
                           {(parameters.Length == 0 ? "Type.EmptyTypes" : $@"new [] {{ {string.Join(',', StringifyMethodParameters(parameters))} }}")})";
                }
                default:
                    return operand?.ToString();
            }
        }

        public EmitInstructionGenerator(Instruction instruction, Module module) {
            _instruction = instruction;
            _module = module;
        }

        public string Generate(Dictionary<long, string> labels) {
            var opCodeName = FixOpCodeName(_instruction.OpCode.Name);
            return _instruction.OperandToken == null ? $@"Emit(OpCodes.{opCodeName})" : $@"Emit(OpCodes.{opCodeName}, {GetParameter(labels)})";
        }
    }
}