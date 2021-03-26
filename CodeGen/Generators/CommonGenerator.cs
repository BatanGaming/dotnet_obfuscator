using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using CodeGen.Extensions;

namespace CodeGen.Generators
{
    public static class CommonGenerator
    {
        private static readonly Dictionary<Type, string> _typesBuildersNames = new Dictionary<Type, string>();
        private static readonly Dictionary<FieldInfo, string> _fieldsBuildersNames = new Dictionary<FieldInfo, string>();
        private static readonly Dictionary<MethodBase, string> _methodsDefinitionsBuildersNames = new Dictionary<MethodBase, string>();
        private static readonly Dictionary<MethodBase, string> _methodsBodiesBuildersNames = new Dictionary<MethodBase, string>();

        private static readonly Lazy<Dictionary<string, string>> _templates =
            new Lazy<Dictionary<string, string>>(InitTemplates);
        
        private static int _duplicates = 0;
        private static Dictionary<string, string> InitTemplates() {
            using var reader = new StreamReader("templates.json");
            var json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        }
        public static string FixSpecialName(string name) {
            return name.Replace(".", "").Replace("<", "").Replace(">", "");
        }

        public static string ResolveTypeName(Type type) {
            var custom = ResolveCustomName(type);
            if (custom != null) {
                return custom;
            }
            
            var codeDomProvider = CodeDomProvider.CreateProvider("C#");
            var typeReferenceExpression =
                new CodeTypeReferenceExpression(new CodeTypeReference(type));
            using var writer = new StringWriter();
            codeDomProvider.GenerateCodeFromExpression(typeReferenceExpression, writer, new CodeGeneratorOptions());
            return $"typeof({writer.GetStringBuilder()})";
        }

        public static string ResolveMethodBodyBuilderName(MethodBase method) {
            return _methodsBodiesBuildersNames.ContainsKey(method) ? _methodsBodiesBuildersNames[method] : null;
        }

        public static string ResolveCustomName(object obj) {
            return obj switch {
                Type t => _typesBuildersNames.ContainsKey(t) ? _typesBuildersNames[t] : null,
                FieldInfo f => _fieldsBuildersNames.ContainsKey(f) ? _fieldsBuildersNames[f] : null,
                MethodBase m => _methodsDefinitionsBuildersNames.ContainsKey(m) ? _methodsDefinitionsBuildersNames[m] : null,
                _ => null
            };
        }
        
        public static string GenerateTypeGeneratorName(Type type) {
            var name = type.IsSpecialName()
                ? FixSpecialName(type.Name)
                : type.Name;
            var resultName = $"type_{name}_builder";
            if (_typesBuildersNames.ContainsValue(resultName)) {
                resultName += $"_{_duplicates++}";
            }
            _typesBuildersNames[type] = resultName;
            return _typesBuildersNames[type];
        }

        public static string GenerateFieldGeneratorName(FieldInfo field) {
            var name = field.IsSpecialName()
                ? FixSpecialName(field.Name)
                : field.Name;
            var declaringTypeName = field.DeclaringType.IsSpecialName()
                ? FixSpecialName(field.DeclaringType.Name)
                : field.DeclaringType.Name;
            var resultName = $"type_{declaringTypeName}_field_{name}_builder";
            if (_fieldsBuildersNames.ContainsValue(resultName)) {
                resultName += $"_{_duplicates++}";
            }
            _fieldsBuildersNames[field] = resultName;
            return _fieldsBuildersNames[field];
        }

        public static string GenerateMethodDefinitionGeneratorName(MethodBase method) {
            var name = method.IsSpecialName()
                ? FixSpecialName(method.Name)
                : method.Name;
            var declaringTypeName = method.DeclaringType.IsSpecialName()
                ? FixSpecialName(method.DeclaringType.Name)
                : method.DeclaringType.Name;
            var resultName = $"type_{declaringTypeName}_method_{name}_builder";
            if (_methodsDefinitionsBuildersNames.ContainsValue(resultName)) {
                resultName += $"_{_duplicates++}";
            }
            
            _methodsDefinitionsBuildersNames[method] = resultName;
            return _methodsDefinitionsBuildersNames[method];
        }

        public static string GenerateMethodBodyGeneratorName(MethodBase method) {
            var name = method.IsSpecialName()
                ? FixSpecialName(method.Name)
                : method.Name;
            var declaringTypeName = method.DeclaringType.IsSpecialName()
                ? FixSpecialName(method.DeclaringType.Name)
                : method.DeclaringType.Name;
            var resultName = $"type_{declaringTypeName}_il_method_{name}_builder";
            if (_methodsBodiesBuildersNames.ContainsValue(resultName)) {
                resultName += $"_{_duplicates++}";
            }
            
            _methodsBodiesBuildersNames[method] = resultName;
            return _methodsBodiesBuildersNames[method];
        }
        
        public static Type GetDelegateType(int parametersCount, bool returnValue) {
            return parametersCount switch {
                0 when returnValue => typeof(Func<>),
                0 when !returnValue => typeof(Action),
                1 when returnValue => typeof(Func<,>),
                1 when !returnValue => typeof(Action<>),
                2 when returnValue => typeof(Func<,,>),
                2 when !returnValue => typeof(Action<,>),
                3 when returnValue => typeof(Func<,,,>),
                3 when !returnValue => typeof(Action<,,>),
                4 when returnValue => typeof(Func<,,,,>),
                4 when !returnValue => typeof(Action<,,,>),
                5 when returnValue => typeof(Func<,,,,,>),
                5 when !returnValue => typeof(Action<,,,,>),
                6 when returnValue => typeof(Func<,,,,,,>),
                6 when !returnValue => typeof(Action<,,,,,>),
                7 when returnValue => typeof(Func<,,,,,,,>),
                7 when !returnValue => typeof(Action<,,,,,,>),
                8 when returnValue => typeof(Func<,,,,,,,,>),
                8 when !returnValue => typeof(Action<,,,,,,,>),
                9 when returnValue => typeof(Func<,,,,,,,,,>),
                9 when !returnValue => typeof(Action<,,,,,,,,>),
                10 when returnValue => typeof(Func<,,,,,,,,,,>),
                10 when !returnValue => typeof(Action<,,,,,,,,,>),
                11 when returnValue => typeof(Func<,,,,,,,,,,,>),
                11 when !returnValue => typeof(Action<,,,,,,,,,,>),
                12 when returnValue => typeof(Func<,,,,,,,,,,,,>),
                12 when !returnValue => typeof(Action<,,,,,,,,,,,>),
                13 when returnValue => typeof(Func<,,,,,,,,,,,,,>),
                13 when !returnValue => typeof(Action<,,,,,,,,,,,,>),
                14 when returnValue => typeof(Func<,,,,,,,,,,,,,,>),
                14 when !returnValue => typeof(Action<,,,,,,,,,,,,,>),
                15 when returnValue => typeof(Func<,,,,,,,,,,,,,,,>),
                15 when !returnValue => typeof(Action<,,,,,,,,,,,,,,>),
                16 when returnValue => typeof(Func<,,,,,,,,,,,,,,,,>),
                16 when !returnValue => typeof(Action<,,,,,,,,,,,,,,,>),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public static Type CloseDelegateType(Type delegateType, Type[] genericTypes) {
            return delegateType.MakeGenericType(genericTypes);
        }
    }
}