using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Generator
{
    public class AssemblyGenerator
    {
        private readonly Assembly _assembly;
        private readonly Dictionary<string, Type> _userTypes = new Dictionary<string, Type>();
        private readonly Dictionary<string, FieldInfo> _userFields = new Dictionary<string, FieldInfo>();
        private readonly Dictionary<string, MethodBase> _userMethods = new Dictionary<string, MethodBase>();

        private readonly StringBuilder _builder = new StringBuilder();

        private string GetVarName(object obj) {
            return obj switch
            {
                Type t => _userTypes.FirstOrDefault(type => type.Value == t).Key,
                FieldInfo f => _userFields.FirstOrDefault(field => field.Value == f).Key,
                MethodBase m => _userMethods.FirstOrDefault(method => method.Value == m).Key,
                _ => null
            };
        }

        private void GenerateMainPart() {
            _builder.AppendLine("// MAIN PART START");

            _builder.AppendLine($@"var assembly_name = new AssemblyName(""{_assembly.GetName().Name}"");");
            _builder.AppendLine(@"var assembly_builder = AssemblyBuilder.DefineDynamicAssembly(assembly_name, AssemblyBuilderAccess.Run);");
            _builder.AppendLine(@"var module_builder = assembly_builder.DefineDynamicModule(assembly_name.Name + "".dll"");");
            _builder.AppendLine();
        }

        private void GenerateTypes() {
            _builder.AppendLine("// GENERATING TYPES");
            var types = _assembly.GetTypes().ToList();
            var stack = new Stack<Type>();
            while (stack.Count != 0 || types.Count != 0) {
                if (stack.Count == 0) {
                    stack.Push(types[0]);
                    continue;
                }

                var type = stack.Peek();
                if (type.BaseType.Assembly == _assembly && GetVarName(type.BaseType) == null) {
                    stack.Push(type.BaseType);
                    continue;
                }

                stack.Pop();
                var (varName, code) = new TypeGenerator(type).Generate("module_builder", GetVarName);
                _userTypes[varName] = type;
                _builder.AppendLine(code);
                types.Remove(type);
            }

            _builder.AppendLine();
        }

        private void GenerateFields() {
            _builder.AppendLine("// GENERATING FIELDS");
            foreach (var (typeVarName, type) in _userTypes) {
                foreach (var field in type.GetFields().Where(f => f.DeclaringType == type)) {
                    var (varName, code) = new FieldGenerator(field).Generate(typeVarName, GetVarName);
                    _userFields[varName] = field;
                    _builder.AppendLine(code);
                }
            }

            _builder.AppendLine();
        }

        private void GenerateConstructorsDefinitions() {
            _builder.AppendLine("// GENERATING CONSTRUCTORS DEFINITIONS");
            foreach (var (typeVarName, type) in _userTypes) {
                var constructors = type.GetConstructors();
                for (var i = 0; i < constructors.Length; i++) {
                    var constructorName = $"constructor_{typeVarName}_{i}_builder";
                    _userMethods[constructorName] = constructors[i];
                    _builder.AppendLine(
                        new ConstructorDefinitionGenerator(constructors[i], GetVarName).Generate(typeVarName,
                            constructorName));
                }
            }

            _builder.AppendLine();
        }

        private void GenerateMethodsDefinitions() {
            _builder.AppendLine("// GENERATING METHODS DEFINITIONS");
            foreach (var (typeVarName, type) in _userTypes) {
                foreach (var method in type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)) {
                    var (varName, code) = new MethodDefinitionGenerator(method, GetVarName).Generate(typeVarName);
                    _userMethods[varName] = method;
                    _builder.AppendLine(code);
                }
            }
            _builder.AppendLine();
        }

        private void GenerateMethodsBodies() {
            foreach(var (methodVarName, method) in _userMethods) {
                _builder.AppendLine($"// GENERATING METHOD BODY FOR {method.Name}({methodVarName})");
                var code = new MethodBodyGenerator(method).Generate(GetVarName(method), GetVarName);
                _builder.AppendLine(code);
            }
            _builder.AppendLine();
        }

        private void GenerateLastPart() {
            foreach (var (name, _) in _userTypes) {
                _builder.AppendLine($@"var {name}_type = {name}.CreateType();");
            }

            if (_assembly.EntryPoint != null) {
                var entryType = _userTypes.First(t => t.Value == _assembly.EntryPoint.DeclaringType).Key + "_type";
                _builder.AppendLine($@"{entryType}.GetMethod(""Main"").Invoke(null, new object?[] {{args}});");
            }
        }

        public AssemblyGenerator(string path) {
            if (!File.Exists(path)) {
                throw new FileNotFoundException();
            }
            _assembly = Assembly.LoadFile(path);
        }

        public string Generate() {
            GenerateMainPart();
            GenerateTypes();
            GenerateFields();
            GenerateConstructorsDefinitions();
            GenerateMethodsDefinitions();
            GenerateMethodsBodies();
            GenerateLastPart();
            return Regex.Replace(_builder.ToString().Replace("System.Void", "void"), @"new\s*\[\]\s*{\s*typeof\(void\)\s*}", "Type.EmptyTypes");
        }
    }
}
