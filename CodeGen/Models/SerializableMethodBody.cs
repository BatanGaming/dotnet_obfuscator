using System;
using System.Collections.Generic;

namespace CodeGen.Models
{
    [Serializable]
    public class SerializableMethodBody
    {
        public byte[] IlCode;
        public InstructionInfo[] Instructions;
        public int MaxStackSize;
        public SerializableLocalVariableInfo[] LocalVariables;
    }
}
