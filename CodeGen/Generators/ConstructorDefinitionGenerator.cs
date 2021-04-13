using System.Reflection;
using CodeGen.Templates;

namespace CodeGen.Generators
{
    public class ConstructorDefinitionGenerator
    {
        private readonly ConstructorInfo _constructor;

        public ConstructorDefinitionGenerator(ConstructorInfo constructor) {
            _constructor = constructor;
        }

        public string Generate() {
            var code = new ConstructorDefinition {
                Attributes = _constructor.Attributes,
                CallingConvention = _constructor.CallingConvention,
                Parameters = _constructor.GetParameters()
            }.Overwrite();
            return code;
        }
    }
}