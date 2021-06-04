using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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
            var delegateTypeName = $"delegate_type_{CommonGenerator.FixSpecialName(_method.DeclaringType.Name)}_{CommonGenerator.FixSpecialName(_method.Name)}_{_method.ReturnType.Name}";
            var closedDelegateTypeName = delegateTypeName;
            builder.AppendLine($"var {delegateTypeName} = typeof({genericTypeName});");
            if (makeGenericString != null) {
                closedDelegateTypeName += "_closed";
                builder.AppendLine($"var {closedDelegateTypeName} = {delegateTypeName}.{makeGenericString};");
            }
            
            builder.AppendLine($@"{ilGeneratorName}.Emit(OpCodes.Call, typeof(MethodBase).GetMethod(""GetCurrentMethod"", Type.EmptyTypes));");
            if (_method.IsGenericMethodDefinition || _method.DeclaringType.IsGenericTypeDefinition) {
                builder.AppendLine(
                    $@"{ilGeneratorName}.Emit(OpCodes.Newobj, typeof(Dictionary<string, Type>).GetConstructor(Type.EmptyTypes));");
                
                foreach (var genericParameter in _method.GetGenericArguments().Concat(_method.DeclaringType.GetGenericArguments())) {
                    builder.AppendLine($@"{ilGeneratorName}.Emit(OpCodes.Dup);");
                    builder.AppendLine($@"{ilGeneratorName}.Emit(OpCodes.Ldstr, ""{genericParameter.Name}"");");
                    builder.AppendLine($@"{ilGeneratorName}.Emit(OpCodes.Ldtoken, {CommonGenerator.ResolveCustomName(genericParameter)});");
                    builder.AppendLine(
                        $@"{ilGeneratorName}.Emit(OpCodes.Call, typeof(Type).GetMethod(""GetTypeFromHandle"", new [] {{ typeof(RuntimeTypeHandle) }}));");
                    builder.AppendLine(
                        $@"{ilGeneratorName}.Emit(OpCodes.Callvirt, typeof(Dictionary<string, Type>).GetMethod(""Add""));");
                }
            }
            else {
                builder.AppendLine($"{ilGeneratorName}.Emit(OpCodes.Ldnull);");
            }
            builder.AppendLine(_method.IsStatic
                ? $"{ilGeneratorName}.Emit(OpCodes.Ldnull);"
                : $"{ilGeneratorName}.Emit(OpCodes.Ldarg_0);"
            );
            builder.AppendLine(@$"{ilGeneratorName}.Emit(OpCodes.Call, typeof(Program).GetMethod(""GetMethod"", new [] {{typeof(MethodInfo), typeof(Dictionary<string, Type>), typeof(object)}}));");

            builder.AppendLine($"{ilGeneratorName}.Emit(OpCodes.Castclass, {closedDelegateTypeName});");
            for (var i = 0; i < parameters.Count; ++i) {
                builder.AppendLine($"{ilGeneratorName}.Emit(OpCodes.Ldarg, {i});");
            }

            if (delegateType.GenericTypeArguments.Any(t => t.IsGenericParameter)) {
                builder.AppendLine($@"{ilGeneratorName}.Emit(OpCodes.Callvirt, TypeBuilder.GetMethod({closedDelegateTypeName}, {delegateTypeName}.GetMethod(""Invoke"")));");
            }
            else {
                builder.AppendLine($@"{ilGeneratorName}.Emit(OpCodes.Callvirt, {closedDelegateTypeName}.GetMethod(""Invoke""));");
            }
            builder.AppendLine($"{ilGeneratorName}.Emit(OpCodes.Ret);");

            return builder.ToString();

        }
    }
}