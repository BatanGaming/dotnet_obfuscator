using System.Reflection;
using System.Text;
using CodeGen.Generators;

namespace CodeGen.Templates
{
    public class SimpleClass : Template
    {
        public string Template => "DefineType($typeName, $attributes, $baseTypeName)";

        public string TypeName { get; set; }
        public TypeAttributes Attributes { get; set; }
        public string BaseTypeName { get; set; }
        
        public string Overwrite() {
            var builder = new StringBuilder(Template);
            builder.Replace("$typeName", $@"""{TypeName}""");
            builder.Replace("$attributes", AttributesGenerator.Generate(Attributes));
            builder.Replace("$baseTypeName", BaseTypeName);
            return builder.ToString();
        }
    }
}