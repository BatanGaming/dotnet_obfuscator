using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Generator
{
    internal class IlParser
    {
        private readonly MethodBase _method;

        private static readonly List<OpCode> _opCodes;

        private static int GetByteCount(OperandType type) {
            return type switch
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

        private object ResolveToken(int token, OperandType type) {
            var module = _method.Module;
            return type switch
            {
                OperandType.InlineMethod => module.ResolveMethod(token),
                OperandType.InlineField => module.ResolveField(token),
                OperandType.InlineSig => module.ResolveSignature(token),
                OperandType.InlineString => module.ResolveString(token),
                OperandType.InlineType => module.ResolveType(token),
                var x when
                    x == OperandType.ShortInlineI ||
                    x == OperandType.ShortInlineBrTarget
                    => token,
                _ => null
            };
        }

        static IlParser() {
            _opCodes = typeof(OpCodes)
                .GetFields()
                .Select(fi => (OpCode)fi.GetValue(null))
                .ToList();
        }

        public IlParser(MethodBase method) {
            _method = method;
        }

        public List<Instruction> Parse() {
            var body = _method.GetMethodBody();
            var ilBytes = body.GetILAsByteArray().ToList();
            var instructions = new List<Instruction>();
            for (var i = 0; i < ilBytes.Count; ++i) {
                OpCode currentOpCode;
                if (ilBytes[i] == 254) {
                    var currentByte = ilBytes[i];
                    ++i;
                    var value = BitConverter.ToInt16(new[] { ilBytes[i], (byte)currentByte }, 0);
                    currentOpCode = _opCodes.Find(op => op.Value == value);
                }
                else {
                    currentOpCode = _opCodes.Find(op => op.Value == ilBytes[i]);
                }

                object resolvedObject = null;
                long offset = 0;

                if (currentOpCode.OperandType != OperandType.InlineNone) {
                    var byteCount = GetByteCount(currentOpCode.OperandType);
                    long operandToken = 0;
                    for (var j = 0; j < byteCount; j++) {
                        ++i;
                        operandToken |= ((long)ilBytes[i]) << (8 * j);
                    }

                    if (currentOpCode.FlowControl == FlowControl.Branch || currentOpCode.FlowControl == FlowControl.Cond_Branch) {
                        var labelOffset = operandToken;
                        var indexFromStart = (i + 1) + (byteCount == 1 ? (sbyte) labelOffset : labelOffset);
                        resolvedObject = indexFromStart;
                    }
                    else {
                        resolvedObject = ResolveToken((int)operandToken, currentOpCode.OperandType);
                    }
                }

                if (instructions.Count != 0) {
                    var prevInstructions = instructions[^1];
                    offset = prevInstructions.Offset + prevInstructions.OpCode.Size +
                             GetByteCount(prevInstructions.OpCode.OperandType);
                }
                instructions.Add(new Instruction(currentOpCode, resolvedObject, offset));
            }

            return instructions;
        }
    }
}
