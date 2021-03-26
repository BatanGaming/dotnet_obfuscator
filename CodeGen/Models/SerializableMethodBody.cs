using System;
using System.Collections.Generic;

namespace CodeGen.Models
{
    [Serializable]
    public class SerializableMethodBody
    {
        public byte[] IlCode { get; set; }
        public List<InstructionInfo> Instructions { get; set; }
        public int MaxStackSize { get; set; }
        public List<SerializableLocalVariableInfo> LocalVariables { get; set; }
    }
}
