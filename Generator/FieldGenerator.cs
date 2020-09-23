using System;
using System.Reflection;

namespace Generator
{
    internal class FieldGenerator
    {
        private readonly FieldInfo _field;

        public FieldGenerator(FieldInfo field) {
            _field = field;
        }

        public (string, string) Generate(string parentTypeVarName, Func<object, string> objectResolver) {
            var varName = $"field_{_field.Name}_builder";
            var typeName = objectResolver(_field.FieldType) ?? $"typeof({_field.FieldType.FullName})";
            var code = $@"var {varName} = {parentTypeVarName}.DefineField(""{_field.Name}"", {typeName}, {AttributesParser.Parse(_field)});";
            return (varName, code);
        }
    }
}
