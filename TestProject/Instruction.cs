using System.Reflection.Emit;

namespace TestProject
{
    public class Instruction
    {
        public OpCode OpCode { get; }
        public long? OperandToken { get; }
        public long Offset { get; }

        public Instruction(OpCode opCode, long? operand, long offset) {
            OpCode = opCode;
            OperandToken = operand;
            Offset = offset;
        }
    }
}
