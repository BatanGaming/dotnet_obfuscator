using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Parser
{
    public class IlParser
    {
        private readonly MethodInfo _method;

        private static readonly List<OpCode>  _opCodes = 
            typeof(OpCodes)
            .GetFields()
            .Select(fi => (OpCode)fi.GetValue(null))
            .ToList();
        
        private static int GetByteCount(OperandType typeInfo) {
            return typeInfo switch
            {
                var x when
                    x == OperandType.InlineBrTarget ||
                    x == OperandType.InlineField ||
                    x == OperandType.InlineI ||
                    x == OperandType.InlineMethod ||
                    x == OperandType.InlineSig ||
                    x == OperandType.InlineString ||
                    x == OperandType.InlineSwitch ||
                    x == OperandType.InlineType ||
                    x == OperandType.InlineTok ||
                    x == OperandType.ShortInlineR => 4,
                var x when
                    x == OperandType.InlineI8 ||
                    x == OperandType.InlineR => 8,
                var x when
                    x == OperandType.ShortInlineBrTarget ||
                    x == OperandType.ShortInlineI ||
                    x == OperandType.ShortInlineVar => 1,
                _ => 0
            };
        }

        private static int? GetToken(OpCode opCode, IReadOnlyList<byte> ilBytes, ref int i) {
            
            if (opCode.OperandType == OperandType.InlineNone) {
                return null;
            }
            var operandToken = 0;
            var byteCount = GetByteCount(opCode.OperandType);
            for (var j = 0; j < byteCount; j++) {
                ++i;
                operandToken |= ilBytes[i] << (8 * j);
            }

            if (opCode.FlowControl == FlowControl.Branch || opCode.FlowControl == FlowControl.Cond_Branch) {
                operandToken = i + 1 + (byteCount == 1 ? (sbyte) operandToken : operandToken);
            }

            return operandToken;
        }

        public IlParser(MethodInfo method) {
            _method = method;
        }

        public List<Instruction> Parse() {
            var ilBytes = _method.GetMethodBody().GetILAsByteArray();
            var instructions = new List<Instruction>();
            for (var i = 0; i < ilBytes.Length; ++i) {
                OpCode opCode;
                if (ilBytes[i] == 0xFE) {
                    var currentByte = ilBytes[i++];
                    var value = BitConverter.ToInt16(new[] { ilBytes[i], currentByte }, 0);
                    opCode = _opCodes.Find(op => op.Value == value);
                }
                else {
                    opCode = _opCodes.Find(op => op.Value == ilBytes[i]);
                }
                var operandToken = GetToken(opCode, ilBytes, ref i);
                long offset = 0;
                if (instructions.Count != 0) {
                    var prevInstructions = instructions[^1];
                    offset = prevInstructions.Offset + prevInstructions.OpCode.Size +
                             GetByteCount(prevInstructions.OpCode.OperandType);
                }
                instructions.Add(
                    new Instruction {
                        OpCode = opCode,
                        OperandToken = operandToken,
                        Offset = offset
                        }
                    );
            }

            return instructions;
        }
    }
}