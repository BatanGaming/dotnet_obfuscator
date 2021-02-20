using System;

namespace CodeGen
{
    [Serializable]
    public class InstructionInfo
    {
        public long Offset { get; set; }
        public OperandInfo OperandInfo { get; set; }
    }
}
