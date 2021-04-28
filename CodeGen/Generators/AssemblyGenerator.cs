using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using CodeGen.Generators.MembersGenerators;
using CodeGen.Generators.TypesGenerators;
using CodeGen.Templates;

namespace CodeGen.Generators
{
    public class AssemblyGenerator
    {
        private readonly Assembly _assembly;
        private const string ResultFile = "ProgramResult.cs";

        private void CreateResultFile() {
            if (File.Exists(ResultFile)) {
                File.Delete(ResultFile);
            }
            File.Copy("Program.cs", ResultFile);
        }

        private static void WriteSection(string sectionKey, string text) {
            var prevText = File.ReadAllText(ResultFile);
            var newText = prevText.Replace(sectionKey, text);
            File.WriteAllText(ResultFile, newText);
        }

        private static string GenerateType(Type type) {
            Generator generator = null;
            if (type.IsClass || (type.IsValueType && !type.IsEnum)) {
                generator = new ClassGenerator(type);
            }
            else if (type.IsInterface) {
                generator = new InterfaceGenerator(type);
            }

            return generator?.Generate();
        }

        private void GenerateRootTypesDefinitions() {
            var builder = new StringBuilder();
            foreach (var type in _assembly.DefinedTypes.Where(t => !t.IsNested)) {
                builder.AppendLine($"var {CommonGenerator.GenerateTypeGeneratorName(type)} = module_builder.{GenerateType(type)};");
            }
            WriteSection("$TYPES", builder.ToString());
        }

        private void GenerateNestedTypesDefinitions() {
            var builder = new StringBuilder();
            foreach (var type in _assembly.DefinedTypes.Where(t => t.IsNested)) {
                builder.AppendLine($"var {CommonGenerator.GenerateTypeGeneratorName(type)} = {CommonGenerator.ResolveCustomName(type.DeclaringType)}.{GenerateType(type)};");
            }
            WriteSection("$NESTED_TYPES", builder.ToString());
        }

        private void SetParents() {
            var builder = new StringBuilder();
            foreach (var type in _assembly.DefinedTypes
                .Where(t => !t.IsInterface && t.BaseType != typeof(object))
            ) {
                builder.AppendLine($"{CommonGenerator.ResolveCustomName(type)}.SetParent({CommonGenerator.ResolveTypeName(type.BaseType)});");
            }
            WriteSection("$PARENTS", builder.ToString());
        }

        private void AddInterfaceImplementations() {
            var builder = new StringBuilder();
            foreach (var type in _assembly.DefinedTypes) {
                foreach (var @interface in type.GetInterfaces()) {
                    builder.AppendLine(
                        $"{CommonGenerator.ResolveCustomName(type)}.AddInterfaceImplementation({CommonGenerator.ResolveTypeName(@interface)});"
                        );
                }
            }
            WriteSection("$INTERFACES_IMPLEMENTATIONS", builder.ToString());
        }

        private void GenerateFields() {
            var builder = new StringBuilder();
            foreach (var type in _assembly.DefinedTypes) {
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Where(f => f.DeclaringType == type)) {
                    var code = new FieldGenerator(field).Generate();
                    builder.AppendLine(
                        $"var {CommonGenerator.GenerateFieldGeneratorName(field)} = {CommonGenerator.ResolveCustomName(type)}.{code};");
                }
            }
            WriteSection("$FIELDS", builder.ToString());
        }

        private void GenerateConstructorsDefinitions() {
            var builder = new StringBuilder();
            foreach (var type in _assembly.DefinedTypes) {
                foreach (var constructor in type.GetConstructors(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)) {
                    var code = new ConstructorDefinitionGenerator(constructor).Generate();
                    builder.AppendLine(
                        $"var {CommonGenerator.GenerateMethodDefinitionGeneratorName(constructor)} = {CommonGenerator.ResolveCustomName(type)}.{code};");
                }
            }
            WriteSection("$CONSTRUCTORS_DEFINITIONS", builder.ToString());
        }

        private void GenerateConstructorsBodies() {
            var builder = new StringBuilder();
            foreach (var type in _assembly.DefinedTypes) {
                foreach (var constructor in type.GetConstructors(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)) {
                    var code = new ConstructorBodyGenerator(constructor).Generate();
                    builder.AppendLine(
                        $"var {CommonGenerator.ResolveMethodBodyBuilderName(constructor)} = {CommonGenerator.ResolveCustomName(constructor)}.GetILGenerator();");
                    builder.AppendLine(code);
                }
            }
            WriteSection("$CONSTRUCTORS_BODIES", builder.ToString());
        }
        
        private void GenerateMethodsDefinitions() {
            var builder = new StringBuilder();
            foreach (var type in _assembly.DefinedTypes) {
                foreach (var method in type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)) {
                    var code = new MethodDefinitionGenerator(method).Generate();
                    builder.AppendLine(
                        $"var {CommonGenerator.GenerateMethodDefinitionGeneratorName(method)} = {CommonGenerator.ResolveCustomName(type)}.{code};");
                }
            }
            WriteSection("$METHODS_DEFINITIONS", builder.ToString());
        }

        private void AddOverriding() {
            var builder = new StringBuilder();
            foreach (var type in _assembly.DefinedTypes.Where(t => !t.IsInterface)) {
                foreach (var map in type.GetInterfaces().Select(i => type.GetInterfaceMap(i))) {
                    for (var i = 0; i < map.InterfaceMethods.Length; ++i) {
                        var interfaceMethod = CommonGenerator.ResolveCustomName(map.InterfaceMethods[i])
                                              ?? new GetMethod {
                                                  Method = map.InterfaceMethods[i],
                                                  Type = map.InterfaceType
                                              }.Overwrite();
                        builder.AppendLine(
                            $"{CommonGenerator.ResolveCustomName(type)}.DefineMethodOverride({CommonGenerator.ResolveCustomName(map.TargetMethods[i])}, {interfaceMethod});"
                        );
                    }
                }
            }
            WriteSection("$METHODS_OVERRIDING", builder.ToString());
        }

        private void GenerateMethodBodies() {
            var builder = new StringBuilder();
            foreach (var type in _assembly.DefinedTypes.Where(t => !t.IsInterface)) {
                foreach (var method in type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)) {
                    var code = new MethodBodyGenerator(method).Generate();
                    builder.AppendLine(
                        $@"var {CommonGenerator.ResolveMethodBodyBuilderName(method)} = {CommonGenerator.ResolveCustomName(method)}.GetILGenerator();");
                    builder.AppendLine(code);
                }
            }

            WriteSection("$METHODS_BODIES", builder.ToString());
        }

        private void CreateTypes() {
            var builder = new StringBuilder();
            var types = _assembly.DefinedTypes;
            foreach (var type in types) {
                var generatorName = CommonGenerator.ResolveCustomName(type);
                builder.AppendLine(
                    $"var {generatorName.Replace("_builder", "")} = {generatorName}.CreateType();");
            }

            if (_assembly.EntryPoint != null) {
                var entryPoint = _assembly.EntryPoint;
                builder.AppendLine(
                    $@"{CommonGenerator.ResolveCustomName(entryPoint.DeclaringType)}.GetMethod(""{entryPoint.Name}"").Invoke(null, new [] {{args}});");
            }
            WriteSection("$CREATED_TYPES", builder.ToString());
        }

        private void SerializeMethodBodies() {
            var builder = new StringBuilder();
            foreach (var type in _assembly.DefinedTypes.Where(t => !t.IsInterface)) {
                foreach (var method in type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)) {
                    var body = new SerializableMethodBodyGenerator(method).Generate();
                    var serialized = JsonSerializer.Serialize(body);
                    var bytes = Encoding.ASCII.GetBytes(serialized);
                    var encoded = Convert.ToBase64String(bytes);
                    builder.AppendLine($@"{{@""{method.DeclaringType.FullName}#{method.Name}"", @""{encoded}""}},");
                }
            }
            WriteSection("$SERIALIZED_METHODS", builder.ToString());
        }
        
        private void WriteReferencedAssemblies() {
            var builder = new StringBuilder();
            foreach (var assembly in _assembly.GetReferencedAssemblies()) {
                builder.AppendLine($@"""{assembly.Name}"",");
            }
            WriteSection("$REFERENCED_ASSEMBLIES", builder.ToString());
        }

        public AssemblyGenerator(Assembly assembly) {
            _assembly = assembly;
        }

        public void GenerateAssembly() {
            CreateResultFile();
            GenerateRootTypesDefinitions();
            GenerateNestedTypesDefinitions();
            SetParents();
            AddInterfaceImplementations();
            GenerateFields();
            GenerateConstructorsDefinitions();
            GenerateConstructorsBodies();
            GenerateMethodsDefinitions();
            AddOverriding();
            GenerateMethodBodies();
            SerializeMethodBodies();
            WriteReferencedAssemblies();
            CreateTypes();
        }
    }
}