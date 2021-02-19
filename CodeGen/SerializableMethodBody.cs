using System.Collections.Generic;

namespace CodeGen
{
    public class SerializableMethodBody
    {
        public byte[] IlCode { get; set; }
        public List<InstructionInfo> Instructions { get; set; }
    }
}
