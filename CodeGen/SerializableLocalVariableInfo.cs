using System;

namespace CodeGen
{
    [Serializable]
    public class SerializableLocalVariableInfo
    {
        public string TypeName { get; set; }
        public bool IsPinned { get; set; }
    }
}