using System;
using System.Reflection;
using CodeGen.Templates;

namespace CodeGen.Generators.TypesGenerators
{
    public class ClassGenerator
    {
        private readonly Type _type;

        public ClassGenerator(Type type) {
            _type = type;
        }

        public string Generate() {
            return new CommonTypeDefinition {
                TypeName = CommonGenerator.ResolveCustomName(_type),
                Attributes = _type.Attributes,
                IsNested = _type.IsNested
            }.Overwrite();
        }
    }
}