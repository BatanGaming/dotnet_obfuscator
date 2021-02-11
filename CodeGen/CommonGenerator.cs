using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CodeGen
{
    public static class CommonGenerator
    {
        private static readonly Dictionary<Type, string> _typesBuildersNames = new Dictionary<Type, string>();
        private static readonly Dictionary<FieldInfo, string> _fieldsBuildersNames = new Dictionary<FieldInfo, string>();
        private static readonly Dictionary<MethodBase, string> _methodsDefinitionsBuildersNames = new Dictionary<MethodBase, string>();
        private static readonly Dictionary<MethodBase, string> _methodsBodiesBuildersNames = new Dictionary<MethodBase, string>();

        public static string ResolveTypeName(Type type, bool isInsideGeneric = false) {
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

        public static string ResolveMethodBodyBuilderName(MethodInfo method) {
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
            _typesBuildersNames[type] = $"type_{type.Name}_builder";
            return _typesBuildersNames[type];
        }

        public static string GenerateFieldGeneratorName(FieldInfo field) {
            _fieldsBuildersNames[field] = $"type_{field.DeclaringType.Name}_field_{field.Name}_builder";
            return _fieldsBuildersNames[field];
        }

        public static string GenerateMethodDefinitionGeneratorName(MethodBase method) {
            _methodsDefinitionsBuildersNames[method] = $"type_{method.DeclaringType.Name}_method_{method.Name}_builder";
            return _methodsDefinitionsBuildersNames[method];
        }

        public static string GenerateMethodBodyGeneratorName(MethodBase method) {
            _methodsBodiesBuildersNames[method] = $"type_{method.DeclaringType.Name}_il_method_{method.Name}_builder";
            return _methodsBodiesBuildersNames[method];
        }
    }
}