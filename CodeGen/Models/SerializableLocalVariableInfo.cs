using System;

namespace CodeGen.Models
{
    [Serializable]
    public class SerializableLocalVariableInfo
    {
        public TypeInfo Info;
        public bool IsPinned;
    }
}