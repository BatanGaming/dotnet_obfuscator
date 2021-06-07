using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CodeGen.Extensions;
using CodeGen.Generators.MembersGenerators;
using CodeGen.Generators.TypesGenerators;
using CodeGen.Templates;
using Newtonsoft.Json;

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
            WriteSection("$ASSEMBLY_NAME", _assembly.GetName().Name);
        }

        private static void WriteSection(string sectionKey, string text) {
            var prevText = File.ReadAllText(ResultFile);
            var newText = prevText.Replace(sectionKey, text);
            File.WriteAllText(ResultFile, newText);
        }

        private void GenerateRootTypesDefinitions() {
            var builder = new StringBuilder();
            foreach (var type in _assembly.DefinedTypes.Where(t => !t.IsNested)) {
                builder.AppendLine($"var {CommonGenerator.GenerateTypeGeneratorName(type)} = module_builder.{new ClassGenerator(type).Generate()};");
            }
            WriteSection("$TYPES", builder.ToString());
        }

        private void GenerateNestedTypesDefinitions() {
            var builder = new StringBuilder();
            foreach (var type in _assembly.DefinedTypes.Where(t => t.IsNested)) {
                builder.AppendLine($"var {CommonGenerator.GenerateTypeGeneratorName(type)} = {CommonGenerator.ResolveCustomName(type.DeclaringType)}.{new ClassGenerator(type).Generate()};");
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

        private void SetGenericConstraintsForTypes() {
            var builder = new StringBuilder();
            foreach (var type in _assembly.DefinedTypes.Where(t => t.IsGenericTypeDefinition)) {
                var arguments = type.GetGenericArguments().Where(a => a.IsGenericParameter).ToList();
                var argumentsName = arguments.Select(a => $@"""{a.Name}""");
                var typeName = CommonGenerator.ResolveCustomName(type);
                var arrayName = $"{typeName}_generic_parameters";
                builder.AppendLine(
                    $"var {arrayName} = {typeName}.DefineGenericParameters({string.Join(',', argumentsName)});");
                for (var i = 0; i < arguments.Count; ++i) {
                    var genericArgumentName = CommonGenerator.GenerateTypeGeneratorName(arguments[i]);
                    builder.AppendLine($"var {genericArgumentName} = {arrayName}[{i}];");
                }
                foreach (var argument in arguments) {
                    var genericArgumentName = CommonGenerator.ResolveCustomName(argument);
                    if (argument.GenericParameterAttributes != GenericParameterAttributes.None) {
                        builder.AppendLine(
                            $"{genericArgumentName}.SetGenericParameterAttributes({AttributesGenerator.Generate(argument.GenericParameterAttributes)});");
                    }

                    var constraintTypes = argument.GetGenericParameterConstraints();
                    if (constraintTypes.Length != 0) {
                        if (constraintTypes.Any(t => t.IsInterface)) {
                            builder.AppendLine(
                                $"{genericArgumentName}.SetInterfaceConstraints({string.Join(',', constraintTypes.Where(t => t.IsInterface).Select(CommonGenerator.ResolveTypeName))});"
                            );
                        }

                        if (constraintTypes.Any(t => t.IsClass)) {
                            var baseType = constraintTypes.FirstOrDefault(t => t.IsClass);
                            builder.AppendLine(
                                $"{genericArgumentName}.SetBaseTypeConstraint({CommonGenerator.ResolveTypeName(baseType)});"
                            );
                        }
                    }
                }
            }
            WriteSection("$GENERIC_CONSTRAINTS_TYPES", builder.ToString());
        }
        
        private void GenerateEnumConstants() {
            var builder = new StringBuilder();
            foreach (var type in _assembly.DefinedTypes.Where(t => t.IsEnum)) {
                var underlyingTypeCast = $"({Enum.GetUnderlyingType(type).FullName})";
                foreach (var field in type
                    .GetFields()
                    .Where(f => f.Name != "value__")
                ) {
                    var value = field.GetRawConstantValue().ToString();
                    builder.AppendLine($"{CommonGenerator.ResolveCustomName(field)}.SetConstant({underlyingTypeCast}{value});");
                }
            }
            WriteSection("$ENUM_CONSTANTS", builder.ToString());
        }

        private void AddInterfaceImplementations() {
            var builder = new StringBuilder();
            foreach (var type in _assembly.DefinedTypes.Where(t => !t.IsEnum)) {
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
                foreach (var field in type.GetAllFields().Where(f => f.DeclaringType == type)) {
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
                foreach (var constructor in type.GetAllConstructors()) {
                    var code = new ConstructorDefinitionGenerator(constructor).Generate();
                    builder.AppendLine(
                        $"var {CommonGenerator.GenerateMethodDefinitionGeneratorName(constructor)} = {CommonGenerator.ResolveCustomName(type)}.{code};");
                }
            }
            WriteSection("$CONSTRUCTORS_DEFINITIONS", builder.ToString());
        }

        private void GenerateMethodsDefinitions() {
            var builder = new StringBuilder();
            foreach (var type in _assembly.DefinedTypes) {
                foreach (var method in type.GetAllMethods()) {
                    var code = new MethodDefinitionGenerator(method).Generate();
                    builder.AppendLine(
                        $"var {CommonGenerator.GenerateMethodDefinitionGeneratorName(method)} = {CommonGenerator.ResolveCustomName(type)}.{code};");
                }
            }
            WriteSection("$METHODS_DEFINITIONS", builder.ToString());
        }

        private void GenerateProperties() {
            var builder = new StringBuilder();
            foreach (var type in _assembly.DefinedTypes) {
                foreach (var property in type.GetAllProperties()) {
                    var propertyName = CommonGenerator.GeneratePropertyGeneratorName(property);
                    builder.AppendLine(
                        @$"var {propertyName} = {CommonGenerator.ResolveCustomName(property.DeclaringType)}.DefineProperty(""{property.Name}"", {AttributesGenerator.Generate(property.Attributes)}, {CommonGenerator.ResolveTypeName(property.PropertyType)}, null);");
                    if (property.CanWrite) {
                        builder.AppendLine(
                            $@"{propertyName}.SetSetMethod({CommonGenerator.ResolveCustomName(property.SetMethod)});");
                    }

                    if (property.CanRead) {
                        builder.AppendLine(
                            $@"{propertyName}.SetGetMethod({CommonGenerator.ResolveCustomName(property.GetMethod)});");
                    }
                }
            }
            WriteSection("$PROPERTIES", builder.ToString());
        }
        
        private void SetGenericConstraintsForMethods() {
            var builder = new StringBuilder();
            foreach (var type in _assembly.DefinedTypes) {
                foreach (var method in type.GetAllMethods().Where(m => m.IsGenericMethodDefinition)) {
                    var arguments = method.GetGenericArguments().Where(a => a.IsGenericParameter).ToList();
                    var argumentsName = arguments.Select(a => $@"""{a.Name}""");
                    var methodName = CommonGenerator.ResolveCustomName(method);
                    var arrayName = $"{methodName}_generic_parameters";
                    builder.AppendLine(
                        $"var {arrayName} = {methodName}.DefineGenericParameters({string.Join(',', argumentsName)});");
                    for (var i = 0; i < arguments.Count; ++i) {
                        var genericArgumentName = CommonGenerator.GenerateTypeGeneratorName(arguments[i]);
                        builder.AppendLine($"var {genericArgumentName} = {arrayName}[{i}];");
                    }
                    foreach (var argument in arguments) {
                        var genericArgumentName = CommonGenerator.ResolveCustomName(argument);
                        if (argument.GenericParameterAttributes != GenericParameterAttributes.None) {
                            builder.AppendLine(
                                $"{genericArgumentName}.SetGenericParameterAttributes({AttributesGenerator.Generate(argument.GenericParameterAttributes)});");
                        }

                        var constraintTypes = argument.GetGenericParameterConstraints();
                        if (constraintTypes.Length != 0) {
                            if (constraintTypes.Any(t => t.IsInterface)) {
                                builder.AppendLine(
                                    $"{genericArgumentName}.SetInterfaceConstraints({string.Join(',', constraintTypes.Where(t => t.IsInterface).Select(CommonGenerator.ResolveTypeName))});"
                                );
                            }

                            if (constraintTypes.Any(t => t.IsClass)) {
                                var baseType = constraintTypes.FirstOrDefault(t => t.IsClass);
                                builder.AppendLine(
                                    $"{genericArgumentName}.SetBaseTypeConstraint({CommonGenerator.ResolveTypeName(baseType)});"
                                );
                            }
                        }
                    }
                }
            }
            WriteSection("$GENERIC_CONSTRAINTS_METHODS", builder.ToString());
        }

        private void SetParameters() {
            var builder = new StringBuilder();
            foreach (var type in _assembly.DefinedTypes) {
                foreach (var method in type
                    .GetAllMethods()
                    .Where(m => m.GetParameters().Length != 0)) {
                    builder.AppendLine(
                        $"{CommonGenerator.ResolveCustomName(method)}.SetParameters({string.Join(',', method.GetParameters().Select(p => CommonGenerator.ResolveTypeName(p.ParameterType)))});");
                }
            }
            WriteSection("$METHODS_PARAMETERS", builder.ToString());
        }

        private void SetReturnTypes() {
            var builder = new StringBuilder();
            foreach (var type in _assembly.DefinedTypes) {
                foreach (var method in type
                    .GetAllMethods()
                    .Where(m => m.ReturnType != typeof(void))) {
                    builder.AppendLine(
                        $"{CommonGenerator.ResolveCustomName(method)}.SetReturnType({CommonGenerator.ResolveTypeName(method.ReturnType)});");
                }
            }
            WriteSection("$METHODS_RETURN_TYPES", builder.ToString());
        }

        private void AddOverriding() {
            var builder = new StringBuilder();
            foreach (var type in _assembly.DefinedTypes.Where(t => !t.IsInterface && !t.IsEnum)) {
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
                foreach (var method in type
                    .GetAllMethods()
                    .Concat(type
                        .GetAllConstructors()
                        .Cast<MethodBase>()
                    )
                ) {
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
            var alreadyCreatedTypes = new List<Type>();
            var types = new Stack<Type>(_assembly.DefinedTypes);
            while (types.Count != 0) {
                var type = types.Pop();
                if (alreadyCreatedTypes.Contains(type)) {
                    alreadyCreatedTypes.Remove(type);
                    continue;
                }
                if (CommonGenerator.ResolveCustomName(type.BaseType) != null && !alreadyCreatedTypes.Contains(type.BaseType)) {
                    types.Push(type);
                    types.Push(type.BaseType);
                    continue;
                }

                if (type.IsNested && CommonGenerator.ResolveCustomName(type.DeclaringType) != null &&
                    !alreadyCreatedTypes.Contains(type.DeclaringType)) {
                    types.Push(type);
                    types.Push(type.DeclaringType);
                    continue;
                }
                var generatorName = CommonGenerator.ResolveCustomName(type);
                builder.AppendLine(
                    $"var {generatorName.Replace("_builder", "")} = {generatorName}.{(type.IsEnum && !type.IsNested ? "CreateTypeInfo()" : "CreateType()")};");
                alreadyCreatedTypes.Add(type);
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
                foreach (var method in type.GetAllMethods().Concat(type.GetAllConstructors().Cast<MethodBase>())) {
                    var body = new SerializableMethodBodyGenerator(method).Generate();
                    var serialized = JsonConvert.SerializeObject(body);
                    var bytes = Encoding.ASCII.GetBytes(serialized);
                    var encoded = Convert.ToBase64String(bytes);
                    builder.AppendLine($@"{{@""{method.DeclaringType.FullName}#{method.Name} {string.Join(',', method.GetParameters().Select(p => CommonGenerator.GetFullName(p.ParameterType)))}"", @""{encoded}""}},");
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
            SetGenericConstraintsForTypes();
            AddInterfaceImplementations();
            GenerateFields();
            GenerateEnumConstants();
            GenerateConstructorsDefinitions();
            GenerateMethodsDefinitions();
            GenerateProperties();
            SetGenericConstraintsForMethods();
            SetParameters();
            SetReturnTypes();
            AddOverriding();
            GenerateMethodBodies();
            SerializeMethodBodies();
            WriteReferencedAssemblies();
            CreateTypes();
        }
    }
}