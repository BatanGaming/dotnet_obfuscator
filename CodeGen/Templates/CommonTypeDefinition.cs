using System;
using System.Linq;
using System.Reflection;
using System.Text;
using CodeGen.Generators;

namespace CodeGen.Templates
{
    public class CommonTypeDefinition : Template
    {
        public string Template => "DefineType($typeName, $attributes)";
        
        public string TemplateForNested => "DefineNestedType($typeName, $attributes)";

        public string TypeName { get; set; }
        public TypeAttributes Attributes { get; set; }

        public bool IsNested { get; set; }

        public string Overwrite() {
            var builder = new StringBuilder(IsNested ? TemplateForNested : Template);
            builder.Replace("$typeName", $@"""{TypeName}""");
            builder.Replace("$attributes", AttributesGenerator.Generate(Attributes));

            return builder.ToString();
        }
    }
}