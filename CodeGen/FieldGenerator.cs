using System.Reflection;

namespace CodeGen
{
    public class FieldGenerator
    {
        private readonly FieldInfo _field;
        
        public FieldGenerator(FieldInfo field) {
            _field = field;
        }

        public string Generate() {
            var code = $@"DefineField(
                        ""{_field.Name}"", 
                        typeof({CommonGenerator.ResolveTypeName(_field.FieldType)}),
                        {AttributesGenerator.Generate(_field)}
                        )";
            return code;
        }
    }
}