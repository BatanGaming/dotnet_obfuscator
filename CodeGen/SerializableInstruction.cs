using System;

namespace CodeGen
{
    [Serializable]
    public class SerializableInstruction
    {
        public short OpCodeValue { get; set; }
        public long? OperandToken { get; set; }
        public long Offset { get; set; }
    }
}
