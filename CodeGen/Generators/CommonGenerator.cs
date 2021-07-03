using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CodeGen.Generators
{
    public static class CommonGenerator
    {
        private static readonly Dictionary<Type, string> _typesBuildersNames = new Dictionary<Type, string>();
        private static readonly Dictionary<FieldInfo, string> _fieldsBuildersNames = new Dictionary<FieldInfo, string>();
        private static readonly Dictionary<MethodBase, string> _methodsDefinitionsBuildersNames = new Dictionary<MethodBase, string>();
        private static readonly Dictionary<MethodBase, string> _methodsBodiesBuildersNames = new Dictionary<MethodBase, string>();
        private static readonly Dictionary<PropertyInfo, string> _propertiesBuildersNames = new Dictionary<PropertyInfo, string>();

        private static readonly StringBuilder _nameBuilder = new StringBuilder("a");
        private static readonly IList<string> _forbiddenNames = new List<string> {"as", "do", "is", "if", "in"};

        private static void NextName() {
            var i = 1;
            _nameBuilder[^i]++;
            while (_nameBuilder[^i] > 'z') {
                _nameBuilder[^i++] = 'a';
                if (i > _nameBuilder.Length) {
                    _nameBuilder.Insert(0, 'a');
                    break;
                }
                _nameBuilder[^i]++;
            }
        }

        public static string GenerateUniqueName() {
            var result = _nameBuilder.ToString();
            while (_forbiddenNames.Contains(result)) {
                NextName();
                result = _nameBuilder.ToString();
            }
            NextName();
            return result;
        }

        public static bool CheckIfCustomGenericArgument(Type type) {
            return type.GetGenericArguments().Any(t =>
                ResolveCustomName(t) != null || (t.IsGenericType && (CheckIfCustomGenericArgument(t) || CheckIfCustomGenericArgument(t.GetGenericTypeDefinition()))));
        }

        public static string GetFullName(Type type) {
            if (type.FullName != null) {
                return type.FullName;
            }

            if (type.IsGenericParameter) {
                return type.Name;
            }

            return $"{type.Namespace}.{type.Name} {string.Join(',', type.GetGenericArguments().Select(GetFullName))}";
        }

        public static string ResolveTypeName(Type type) {
            var custom = ResolveCustomName(type);
            if (custom != null) {
                return custom;
            }

            if (type.HasElementType) {
                custom = ResolveCustomName(type.GetElementType());
                if (custom != null) {
                    var func = "";
                    if (type.IsArray) {
                        func = "MakeArrayType()";
                    }
                    else if (type.IsByRef) {
                        func = "MakeByRefType()";
                    }
                    else if (type.IsPointer) {
                        func = "MakePointerType()";
                    }

                    return $"{custom}.{func}";
                }
            }

            if (type.IsGenericType) {
                custom = ResolveCustomName(type.GetGenericTypeDefinition());
                if (custom != null) {
                    return $"{custom}.MakeGenericType({string.Join(',', type.GetGenericArguments().Select(ResolveTypeName))})";
                }
            }
            var generic = "";
            if (CheckIfCustomGenericArgument(type)) {
                generic = $".MakeGenericType({string.Join(',', type.GetGenericArguments().Select(ResolveTypeName))})";
                type = type.GetGenericTypeDefinition();
            }
            var codeDomProvider = CodeDomProvider.CreateProvider("C#");
            var typeReferenceExpression =
                new CodeTypeReferenceExpression(new CodeTypeReference(type));
            using var writer = new StringWriter();
            codeDomProvider.GenerateCodeFromExpression(typeReferenceExpression, writer, new CodeGeneratorOptions());

            return $"typeof({writer.GetStringBuilder()}){generic}";
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
            _typesBuildersNames[type] = GenerateUniqueName();
            return _typesBuildersNames[type];
        }

        public static string GeneratePropertyGeneratorName(PropertyInfo property) {
            _propertiesBuildersNames[property] = GenerateUniqueName();
            return _propertiesBuildersNames[property];
        }

        public static string GenerateFieldGeneratorName(FieldInfo field) {
            _fieldsBuildersNames[field] = GenerateUniqueName();
            return _fieldsBuildersNames[field];
        }

        public static string GenerateMethodDefinitionGeneratorName(MethodBase method) {

            _methodsDefinitionsBuildersNames[method] = GenerateUniqueName();
            return _methodsDefinitionsBuildersNames[method];
        }

        public static string GenerateMethodBodyGeneratorName(MethodBase method) {
            _methodsBodiesBuildersNames[method] = GenerateUniqueName();
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
            return delegateType.IsGenericTypeDefinition 
                ? delegateType.MakeGenericType(genericTypes) 
                : delegateType;
        }
    }
}