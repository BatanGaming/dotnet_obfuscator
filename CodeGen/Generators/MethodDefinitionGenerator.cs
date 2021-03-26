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

        private static IEnumerable<string> StringifyParameters(IEnumerable<ParameterInfo> parameters) {
            return from parameter in parameters 
                select CommonGenerator.ResolveTypeName(parameter.ParameterType);
        }

        private string GetParameters() {
            var parameters = _method.GetParameters();
            return parameters.Length == 0
                ? "Type.EmptyTypes"
                : $@"new [] {{ {string.Join(',', StringifyParameters(parameters))} }}";
        }

        public string Generate() {
            var code = $@"DefineMethod(
                        ""{_method.Name}"",
                        {string.Join(" | ", AttributesGenerator.Generate(_method))},
                        {CommonGenerator.ResolveTypeName(_method.ReturnType)},
                        {GetParameters()}
                        )";
            return code;
        }
    }
}