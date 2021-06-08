using System;
using System.Linq;
using System.Reflection;

namespace CodeGen.Generators.MembersGenerators
{
    public class FieldGenerator : Generator
    {
        private readonly FieldInfo _field;
        
        public FieldGenerator(FieldInfo field) {
            _field = field;
        }

        public string Generate() {
            var code = $@"DefineField(
                        ""{_field.Name}"", 
                        {CommonGenerator.ResolveTypeName(_field.FieldType)},
                        {AttributesGenerator.Generate(_field.Attributes)}
                        )";
            return code;
        }

        public static string GenerateDynamicField(string name, Type type, bool isStatic) {
            var genericTypeName = type.FullName.StartsWith("Func")
                ? $"Func<{new string(',', type.GenericTypeArguments.Length)}>"
                : type.GenericTypeArguments.Length == 0
                    ? "Action"
                    : $"Action<{new string(',', type.GenericTypeArguments.Length - 1)}>";
            var makeGenericString = genericTypeName.StartsWith("Func") || type.GenericTypeArguments.Length != 0
                ? $"MakeGenericType({string.Join(',', type.GenericTypeArguments.Select(CommonGenerator.ResolveTypeName))})"
                : null;
            
            var code = $@"DefineField(
                ""{name}"", 
                typeof({genericTypeName}){(makeGenericString != null ? $".{makeGenericString}" : "")},
                FieldAttributes.Private {(isStatic ? "| FieldAttributes.Static" : "")}
                )";
            return code;
        }
    }
}