using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Generator
{
    internal class ConstructorDefinitionGenerator
    {
        private readonly ConstructorInfo _constructor;

        private readonly Func<object, string> _objectResolver;

        private IEnumerable<string> GetParameters() {
            var parameters = _constructor.GetParameters();
            if (parameters.Length == 0) {
                return null;
            }
            return from parameter in parameters
                select _objectResolver(parameter.ParameterType) ?? $"typeof({parameter.ParameterType.FullName})";
        }

        private IEnumerable<string> GetConventions() {
            return from convention in _constructor.CallingConvention.ToString().Split(',')
                select $"{typeof(CallingConventions).FullName}.{convention.Trim()}";
        }

        public ConstructorDefinitionGenerator(ConstructorInfo constructor, Func<object, string> objectResolver) {
            _constructor = constructor;
            _objectResolver = objectResolver;
        }

        public string Generate(string typeVarName, string constructorName) {
            var code = $@"var {constructorName} = {typeVarName}.DefineConstructor({string.Join(" | ", AttributesParser.Parse(_constructor))}, {string.Join(" | ", GetConventions())}, new []{{{string.Join(',', GetParameters() ?? new[] {"typeof(void)"})}}});";
            return code;
        }
    }
}
