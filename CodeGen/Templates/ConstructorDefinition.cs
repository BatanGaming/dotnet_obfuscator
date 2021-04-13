using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CodeGen.Generators;

namespace CodeGen.Templates
{
    public class ConstructorDefinition : Template
    {
        public string Template => @"DefineConstructor($attributes, $callingConventions, $parameters)";
        
        public MethodAttributes Attributes { get; set; }
        public CallingConventions CallingConvention { get; set; }
        public ParameterInfo[] Parameters { get; set; }
        
        private static IEnumerable<string> StringifyParameters(IEnumerable<ParameterInfo> parameters) {
            return from parameter in parameters 
                select CommonGenerator.ResolveTypeName(parameter.ParameterType);
        }

        private string StringifyParameters() {
            return Parameters.Length == 0
                ? "Type.EmptyTypes"
                : $@"new [] {{ {string.Join(',', StringifyParameters(Parameters))} }}";
        }
        
        private string StringifyCallingConvention() {
            var array = CallingConvention
                .ToString()
                .Split(',')
                .Select(s => $"CallingConventions.{s.Trim()}");
            return string.Join(" | ", array);
        }

        public string Overwrite() {
            var builder = new StringBuilder(Template);
            builder.Replace("$attributes", AttributesGenerator.Generate(Attributes));
            builder.Replace("$callingConventions", StringifyCallingConvention());
            builder.Replace("$parameters", StringifyParameters());
            return builder.ToString();
        }
    }
}