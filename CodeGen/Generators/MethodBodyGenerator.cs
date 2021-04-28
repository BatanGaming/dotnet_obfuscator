using System.Linq;
using System.Reflection;
using System.Text;

namespace CodeGen.Generators
{
    public class MethodBodyGenerator
    {
        private readonly MethodInfo _method;

        public MethodBodyGenerator(MethodInfo method) {
            _method = method;
        }

        public string Generate() {
            var parameters = _method.GetParameters().Select(p => p.ParameterType).ToList();
            var hasReturnType = _method.ReturnType != typeof(void);
            var delegateType = CommonGenerator.CloseDelegateType(
                CommonGenerator.GetDelegateType(parameters.Count, hasReturnType),
                (hasReturnType
                    ? parameters.Concat(new[] {_method.ReturnType})
                    : parameters).ToArray()
            );
            var genericTypeName = delegateType.Name.StartsWith("Func")
                ? $"Func<{new string(',', delegateType.GenericTypeArguments.Length - 1)}>"
                : delegateType.GenericTypeArguments.Length == 0
                    ? "Action"
                    : $"Action<{new string(',', delegateType.GenericTypeArguments.Length - 1)}>";
            var makeGenericString = genericTypeName.StartsWith("Func") || delegateType.GenericTypeArguments.Length != 0
                ? $"MakeGenericType({string.Join(',', delegateType.GenericTypeArguments.Select(CommonGenerator.ResolveTypeName))})"
                : null;

            var ilGeneratorName = CommonGenerator.GenerateMethodBodyGeneratorName(_method);
            var builder = new StringBuilder();
            var fieldName = $"type_{CommonGenerator.FixSpecialName(_method.DeclaringType.Name)}_{CommonGenerator.FixSpecialName(_method.Name)}_field";
            var attributes = $"FieldAttributes.Private {(_method.IsStatic ? "| FieldAttributes.Static" : "")}";
            var delegateTypeName = $"delegate_type_{CommonGenerator.FixSpecialName(_method.DeclaringType.Name)}_{CommonGenerator.FixSpecialName(_method.Name)}_{_method.ReturnType.Name}";
            builder.AppendLine(
                $"var {delegateTypeName} = typeof({genericTypeName}){(makeGenericString != null ? $".{makeGenericString}" : "")};");
            builder.AppendLine(
                $@"var {fieldName} = {CommonGenerator.ResolveCustomName(_method.DeclaringType)}.DefineField(""{fieldName}"", {delegateTypeName}, {attributes});");
            if (_method.IsStatic) {
                builder.AppendLine($"{ilGeneratorName}.Emit(OpCodes.Ldnull);");
                builder.AppendLine($@"{ilGeneratorName}.Emit(OpCodes.Ldstr, ""{_method.DeclaringType.FullName}#{_method.Name}"");");
                builder.AppendLine(@$"{ilGeneratorName}.Emit(OpCodes.Call, typeof(Program).GetMethod(""GetMethod"", new [] {{typeof(object), typeof(string)}}));");
                builder.AppendLine($"{ilGeneratorName}.Emit(OpCodes.Castclass, {delegateTypeName});");
                builder.AppendLine($"{ilGeneratorName}.Emit(OpCodes.Stsfld, {fieldName});");
                builder.AppendLine($"{ilGeneratorName}.Emit(OpCodes.Ldsfld, {fieldName});");
                for (var i = 0; i < parameters.Count; ++i) {
                    builder.AppendLine($"{ilGeneratorName}.Emit(OpCodes.Ldarg, {i});");
                }
                builder.AppendLine($@"{ilGeneratorName}.Emit(OpCodes.Callvirt, {delegateTypeName}.GetMethod(""Invoke""));");
                builder.AppendLine($"{ilGeneratorName}.Emit(OpCodes.Ldnull);");
                builder.AppendLine($"{ilGeneratorName}.Emit(OpCodes.Stsfld, {fieldName});");
                builder.AppendLine($"{ilGeneratorName}.Emit(OpCodes.Ret);");
            }
            else {
                builder.AppendLine($"{ilGeneratorName}.Emit(OpCodes.Ldarg_0);");
                builder.AppendLine($"{ilGeneratorName}.Emit(OpCodes.Ldarg_0);");
                builder.AppendLine($@"{ilGeneratorName}.Emit(OpCodes.Ldstr, ""{_method.DeclaringType.FullName}#{_method.Name}"");");
                builder.AppendLine(@$"{ilGeneratorName}.Emit(OpCodes.Call, typeof(Program).GetMethod(""GetMethod"", new [] {{typeof(object), typeof(string)}}));");
                builder.AppendLine($"{ilGeneratorName}.Emit(OpCodes.Castclass, {delegateTypeName});");
                builder.AppendLine($"{ilGeneratorName}.Emit(OpCodes.Stfld, {fieldName});");
                builder.AppendLine($"{ilGeneratorName}.Emit(OpCodes.Ldarg_0);");
                builder.AppendLine($"{ilGeneratorName}.Emit(OpCodes.Ldfld, {fieldName});");
                for (var i = 1; i <= parameters.Count; ++i) {
                    builder.AppendLine($"{ilGeneratorName}.Emit(OpCodes.Ldarg, {i});");
                }
                builder.AppendLine($@"{ilGeneratorName}.Emit(OpCodes.Callvirt, {delegateTypeName}.GetMethod(""Invoke""));");
                builder.AppendLine($"{ilGeneratorName}.Emit(OpCodes.Ldarg_0);");
                builder.AppendLine($"{ilGeneratorName}.Emit(OpCodes.Ldnull);");
                builder.AppendLine($"{ilGeneratorName}.Emit(OpCodes.Stfld, {fieldName});");
                builder.AppendLine($"{ilGeneratorName}.Emit(OpCodes.Ret);");
            }

            return builder.ToString();

        }
    }
}