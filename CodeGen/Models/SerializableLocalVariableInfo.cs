using System;

namespace CodeGen.Models
{
    [Serializable]
    public class SerializableLocalVariableInfo
    {
        public TypeInfo Info { get; set; }
        public bool IsPinned { get; set; }
    }
}