using System;

namespace CodeGen.Models
{
    [Serializable]
    public class TypeInfo
    {
        public string Name { get; set; }
        public TypeInfo[] GenericArguments { get; set; }
        public bool IsByRef { get; set; }
        public bool IsPointer { get; set; }
    }
}