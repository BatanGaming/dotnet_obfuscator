using System;

namespace CodeGen
{
    public class NestedClassGenerator
    {
        private readonly Type _type;
        
        
        public NestedClassGenerator(Type type) {
            _type = type;
        }

        public string Generate() {
            return $@"DefineNestedType(
                ""{_type.Name}"", 
                {string.Join(" | ", AttributesGenerator.Generate(_type))}, 
                {CommonGenerator.ResolveTypeName(_type.BaseType)})";
        }
    }
}