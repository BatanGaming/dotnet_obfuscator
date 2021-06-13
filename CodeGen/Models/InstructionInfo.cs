using System;

namespace CodeGen.Models
{
    [Serializable]
    public class InstructionInfo
    {
        public long Offset;
        public int Size;
        public OperandInfo OperandInfo;
    }
}
