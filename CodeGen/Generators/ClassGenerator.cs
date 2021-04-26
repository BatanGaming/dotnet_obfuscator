using System;
using CodeGen.Templates;

namespace CodeGen.Generators
{
    public class ClassGenerator
    {
        private readonly Type _type;

        public ClassGenerator(Type type) {
            _type = type;
        }

        public string Generate() {
            return new SimpleClass {
                TypeName = _type.FullName,
                Attributes = _type.Attributes,
                BaseTypeName = CommonGenerator.ResolveTypeName(_type.BaseType)
            }.Overwrite();
        }
    }
}