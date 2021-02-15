using System;
using System.Collections.Generic;
using System.Reflection;

namespace TestProject
{
    [Serializable]
    public class SerializableMethodInfo
    {
        public string Name { get; set; }
        public int ReturnTypeToken { get; set; }
        public MethodAttributes Attributes { get; set; }
        public CallingConventions CallingConventions { get; set; }
        public int[] ParameterTypesTokens { get; set; }
        public int[] LocalVariablesTypesTokens { get; set; }
        public int OwnerTypeToken { get; set; }
        public List<SerializableInstruction> Instructions { get; set; }
        public long[] CustomMethodsOffsets { get; set; }
    }
}
