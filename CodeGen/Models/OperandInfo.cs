using System;

namespace CodeGen.Models
{
    [Serializable]
    public class OperandInfo
    {
        public OperandTypeInfo? OperandType { get; set; }
        public string OperandName { get; set; }
        public TypeInfo[] Parameters { get; set; }
        public TypeInfo[] GenericTypes { get; set; }
        public TypeInfo[] DeclaringTypeGenericTypes { get; set; }
    }
}
