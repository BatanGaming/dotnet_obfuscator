using System.Reflection.Emit;

namespace Parser
{
    public class Instruction
    {
        public OpCode OpCode { get; set; }
        public int? OperandToken { get; set; }
        public long Offset { get; set; }
    }
}