using System.Reflection.Emit;

namespace Generator
{
    internal class Instruction
    {
        public OpCode OpCode { get; }
        public object Operand { get; }
        public long Offset { get; }

        public Instruction(OpCode opCode, object operand, long offset) {
            OpCode = opCode;
            Operand = operand;
            Offset = offset;
        }
    }
}
