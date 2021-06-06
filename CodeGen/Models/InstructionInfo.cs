using System;
using System.Text.Json.Serialization;

namespace CodeGen.Models
{
    [Serializable]
    public class InstructionInfo
    {
        public long Offset { get; set; }
        public int Size { get; set; }
        public OperandInfo OperandInfo { get; set; }
    }
}
