using System.Reflection;

namespace CodeGen.Generators.MembersGenerators
{
    public class FieldGenerator
    {
        private readonly FieldInfo _field;
        
        public FieldGenerator(FieldInfo field) {
            _field = field;
        }

        public string Generate() {
            var code = $@"DefineField(
                        ""{CommonGenerator.ResolveCustomName(_field)}"", 
                        {CommonGenerator.ResolveTypeName(_field.FieldType)},
                        {AttributesGenerator.Generate(_field.Attributes)}
                        )";
            return code;
        }
    }
}