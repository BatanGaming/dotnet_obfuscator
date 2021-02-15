using System;
using System.Collections.Generic;
using System.Reflection;
using Parser;

namespace CodeGen
{
    [Serializable]
    public class SerializableMethodInfo
    {
        public string Name { get; set; }
        public int ReturnTypeToken { get; set; }
        public MethodAttributes Attributes { get; set; }
        public CallingConventions CallingConventions { get; set; }
        public int[] ParameterTypesTokens { get; set; }
        public int OwnerTypeToken { get; set; }
        public List<Instruction> Instructions { get; set; }
    }
}
