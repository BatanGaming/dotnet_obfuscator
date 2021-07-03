using System;

namespace CodeGen.Models
{
    [Serializable]
    public class OperandInfo
    {
        public OperandTypeInfo? OperandType;
        public string OperandName;
        public TypeInfo[] Parameters;
        public TypeInfo[] GenericTypes;
        public TypeInfo[] DeclaringTypeGenericTypes;
    }
}
