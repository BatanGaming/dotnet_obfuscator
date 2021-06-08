using System;
using System.Linq;
using System.Reflection;
using System.Text;
using CodeGen.Generators;

namespace CodeGen.Templates
{
    public class GetMethod : Template
    {
        public string Template { get; } = @"$type.GetMethod(""$methodName"", $parameters)$generic";
        public string TemplateForConstructor { get; } = @"$type.GetConstructor($parameters)";
        
        public MethodBase Method { get; set; }
        public Type Type { get; set; }
        
        public string Overwrite() {
            var builder = new StringBuilder(Method.IsConstructor ? TemplateForConstructor : Template);
            builder.Replace("$type", CommonGenerator.ResolveTypeName(Type));
            if (!Method.IsConstructor) {
                builder.Replace("$methodName", Method.Name);
            }
            var parameters = Method.GetParameters();
            if (parameters.Length == 0) {
                builder.Replace("$parameters", "Type.EmptyTypes");
            }
            else {
                var strParameters = string.Join(',',parameters.Select(t => CommonGenerator.ResolveTypeName(t.ParameterType)));
                builder.Replace("$parameters", $"new [] {{{strParameters}}}");
            }

            if (Method.IsGenericMethod) {
                var arguments = string.Join(',', Method.GetGenericArguments().Select(CommonGenerator.ResolveTypeName));
                builder.Replace("$generic", $".MakeGenericMethod({arguments})");
            }
            else {
                builder.Replace("$generic", "");
            }

            return builder.ToString();
        }
    }
}