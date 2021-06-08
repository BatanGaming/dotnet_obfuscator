using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CodeGen.Generators
{
    public class MethodDefinitionGenerator
    {
        private readonly MethodInfo _method;

        public MethodDefinitionGenerator(MethodInfo method) {
            _method = method;
        }

        public string Generate() {
            var code = $@"DefineMethod(
                        ""{_method.Name}"",
                        {string.Join(" | ", AttributesGenerator.Generate(_method.Attributes))}
                        )";
            return code;
        }
    }
}