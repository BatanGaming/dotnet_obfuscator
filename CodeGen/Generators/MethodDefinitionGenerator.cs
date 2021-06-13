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
                        ""{CommonGenerator.ResolveCustomName(_method)}"",
                        {string.Join(" | ", AttributesGenerator.Generate(_method.Attributes))}
                        )";
            return code;
        }
    }
}