using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CodeGen
{
    public class ConstructorDefinitionGenerator
    {
        private readonly ConstructorInfo _constructor;
        
        private static IEnumerable<string> StringifyParameters(IEnumerable<ParameterInfo> parameters) {
            return from parameter in parameters 
                select CommonGenerator.ResolveTypeName(parameter.ParameterType);
        }

        private string GetParameters() {
            var parameters = _constructor.GetParameters();
            return parameters.Length == 0
                ? "Type.EmptyTypes"
                : $@"new [] {{ {string.Join(',', StringifyParameters(parameters))} }}";
        }

        private string StringifyCallingConvention() {
            var array = _constructor.CallingConvention
                .ToString()
                .Split(',')
                .Select(s => $"CallingConventions.{s.Trim()}");
            return string.Join(" | ", array);
        }

        public ConstructorDefinitionGenerator(ConstructorInfo constructor) {
            _constructor = constructor;
        }

        public string Generate() {
            var code = $@"DefineConstructor(
                        {string.Join(" | ", AttributesGenerator.Generate(_constructor))},
                        {StringifyCallingConvention()},
                        {GetParameters()}
                        )";
            return code;
        }
    }
}