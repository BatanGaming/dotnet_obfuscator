using System;

namespace CodeGen
{
    public class ClassGenerator
    {
        private readonly Type _type;

        private string GetBaseTypeName() {
            var typeName = CommonGenerator.ResolveCustomName(_type.BaseType);
            return typeName ?? $"typeof({_type.BaseType.FullName})";
        }
        
        public ClassGenerator(Type type) {
            _type = type;
        }

        public string Generate() {
            return $@"DefineType(
                ""{_type.Name}"", 
                {string.Join(" | ", AttributesGenerator.Generate(_type))}, 
                {GetBaseTypeName()})";
        }
    }
}