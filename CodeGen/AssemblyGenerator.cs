using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Parser;

namespace CodeGen
{
    public class AssemblyGenerator
    {
        protected readonly Assembly _assembly;
        private const string ResultFile = "ProgramResult.cs";

        private void CreateResultFile() {
            if (File.Exists(ResultFile)) {
                File.Delete(ResultFile);
            }
            File.Copy("Program.cs", ResultFile);
        }

        protected static void WriteSection(string sectionKey, string text) {
            var prevText = File.ReadAllText(ResultFile);
            var newText = prevText.Replace(sectionKey, text);
            File.WriteAllText(ResultFile, newText);
        }
        
        private void GenerateTypes() {
            var builder = new StringBuilder();
            var types = _assembly.DefinedTypes.ToList();
            var stack = new Stack<TypeInfo>();
            while (stack.Count != 0 || types.Count != 0) {
                if (stack.Count == 0) {
                    stack.Push(types[0]);
                }
                var type = stack.Peek();
                if (type.BaseType.Assembly == _assembly && CommonGenerator.ResolveCustomName(type.BaseType) == null) {
                    stack.Push(type.BaseType.GetTypeInfo());
                    continue;
                }

                stack.Pop();
                //todo switch by type
                var code = new ClassGenerator(type).Generate();
                builder.AppendLine($"var {CommonGenerator.GenerateTypeGeneratorName(type)} = module_builder.{code};");
                types.Remove(type);
            }
            WriteSection("$TYPES", builder.ToString());
        }

        private void GenerateFields() {
            var builder = new StringBuilder();
            foreach (var type in _assembly.DefinedTypes.ToList()) {
                foreach (var field in type.GetFields().Where(f => f.DeclaringType == type)) {
                    var code = new FieldGenerator(field).Generate();
                    builder.AppendLine(
                        $"var {CommonGenerator.GenerateFieldGeneratorName(field)} = {CommonGenerator.ResolveCustomName(type)}.{code};");
                }
            }
            WriteSection("$FIELDS", builder.ToString());
        }

        private void GenerateProperties() {
        }

        protected virtual void GenerateMethodsDefinitions() {
            var builder = new StringBuilder();
            foreach (var type in _assembly.DefinedTypes.ToList()) {
                foreach (var method in type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)) {
                    var code = new MethodDefinitionGenerator(method).Generate();
                    builder.AppendLine(
                        $"var {CommonGenerator.GenerateMethodDefinitionGeneratorName(method)} = {CommonGenerator.ResolveCustomName(type)}.{code};");
                }
            }
            WriteSection("$METHODS_DEFINITIONS", builder.ToString());
        }

        protected virtual void GenerateMethodBodies() {
            var builder = new StringBuilder();
            foreach (var type in _assembly.DefinedTypes.ToList()) {
                foreach (var method in type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)) {
                    var code = new MethodBodyGenerator(method).Generate();
                    builder.AppendLine(
                        $@"var {CommonGenerator.ResolveMethodBodyBuilderName(method)} = {CommonGenerator.ResolveCustomName(method)}.GetILGenerator();");
                    builder.AppendLine(code);
                }
            }

            WriteSection("$METHODS_BODIES", builder.ToString());
        }

        public AssemblyGenerator(Assembly assembly) {
            _assembly = assembly;
        }

        public void GenerateAssembly() {
            CreateResultFile();
            GenerateTypes();
            GenerateFields();
            GenerateMethodsDefinitions();
            GenerateMethodBodies();
        }
    }
}