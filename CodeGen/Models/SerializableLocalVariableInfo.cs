using System;

namespace CodeGen.Models
{
    [Serializable]
    public class SerializableLocalVariableInfo
    {
        public string TypeName { get; set; }
        public bool IsPinned { get; set; }
    }
}