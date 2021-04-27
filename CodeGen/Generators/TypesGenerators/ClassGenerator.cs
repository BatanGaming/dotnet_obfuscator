using System;
using CodeGen.Templates;

namespace CodeGen.Generators.TypesGenerators
{
    public class ClassGenerator : Generator
    {
        private readonly Type _type;

        public ClassGenerator(Type type) {
            _type = type;
        }

        public string Generate() {
            return new CommonTypeDefinition {
                TypeName = _type.FullName,
                Attributes = _type.Attributes,
                IsNested = _type.IsNested
            }.Overwrite();
        }
    }
}