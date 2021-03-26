using System.Text;

namespace CodeGen.Templates
{
    public class SimpleClass : BaseTemplate
    {
        public string Template { get; set; } = "DefineType($1, $2, $3)";

        public string TypeName { get; set; }
        public string Attributes { get; set; }
        public string BaseTypeName { get; set; }
        
        public string Overwrite() {
            var builder = new StringBuilder(Template);
            builder.Replace("$1", TypeName);
            builder.Replace("$2", Attributes);
            builder.Replace("$3", BaseTypeName);
            return builder.ToString();
        }
    }
}