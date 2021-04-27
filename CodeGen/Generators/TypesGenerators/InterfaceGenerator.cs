using System;
using CodeGen.Templates;

namespace CodeGen.Generators.TypesGenerators
{
    public class InterfaceGenerator : Generator
    {
        private readonly Type _type;

        public InterfaceGenerator(Type type) {
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