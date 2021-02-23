using System;

namespace CodeGen
{
    public class ClassGenerator
    {
        private readonly Type _type;

        public ClassGenerator(Type type) {
            _type = type;
        }

        public string Generate() {
            return $@"DefineType(
                ""{_type.FullName}"", 
                {string.Join(" | ", AttributesGenerator.Generate(_type))}, 
                {CommonGenerator.ResolveTypeName(_type.BaseType)})";
        }
    }
}