using System;
using System.Text.Json.Serialization;

namespace CodeGen.Models
{
    [Serializable]
    public class SerializableLocalVariableInfo
    {
        public TypeInfo Info { get; set; }
        public bool IsPinned { get; set; }
    }
}