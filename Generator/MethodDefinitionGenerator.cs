using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Generator
{
    internal class MethodDefinitionGenerator
    {
        private readonly MethodInfo _method;
        private readonly Func<object, string> _objectResolver;

        private IEnumerable<string> GetParameters() {
            var parameters = _method.GetParameters();
            if (parameters.Length == 0) {
                return null;
            }
            return from parameter in parameters
                select _objectResolver(parameter.ParameterType) ?? $"typeof({parameter.ParameterType.FullName})";
        }

        private string GetReturnValue() {
            return _objectResolver(_method.ReturnType) ?? $"typeof({_method.ReturnType.FullName})";
        }

        public MethodDefinitionGenerator(MethodInfo method, Func<object, string> objectResolver) {
            _method = method;
            _objectResolver = objectResolver;
        }

        public (string, string) Generate(string typeVarName) {
            var methodVarName = $"method_{_method.Name}_builder";
            var code = $@"var {methodVarName} = {typeVarName}.DefineMethod(""{_method.Name}"", {string.Join(" | ", AttributesParser.Parse(_method))}, {GetReturnValue()}, new [] {{{string.Join(',', GetParameters() ?? new[] { "typeof(System.Void)" })}}});";
            return (methodVarName, code);
        }
    }
}
