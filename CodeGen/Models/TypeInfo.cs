using System;

namespace CodeGen.Models
{
    [Serializable]
    public class TypeInfo
    {
        public string Name;
        public TypeInfo[] GenericArguments;
        public bool IsByRef;
        public bool IsPointer;
        public bool IsArray;
    }
}