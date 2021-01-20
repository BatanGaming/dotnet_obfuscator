using System;

namespace Generator
{
    internal class TypeGenerator
    {
        private readonly Type _type;

        public TypeGenerator(Type type) {
            _type = type;
        }

        public (string, string) Generate(string moduleBuilderName, Func<object, string> objectResolver) {
            var varName = $"type_{_type.Name}_builder";
            var code = $@"var {varName} = {moduleBuilderName}.DefineType(""{_type.Name}"", {string.Join(" | ", AttributesParser.Parse(_type))}, {objectResolver(_type.BaseType) ?? $"typeof({_type.BaseType.FullName})"});";
            return (varName, code);
        }
    }
}
