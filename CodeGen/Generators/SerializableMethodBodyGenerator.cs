using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CodeGen.Models;
using Parser;

namespace CodeGen.Generators
{
    public class SerializableMethodBodyGenerator
    {
        private readonly MethodInfo _method;
        private readonly Module _module;

        private object SafeResolveToken(int token) {
            object result = null;
            try {
                result = _module.ResolveType(token);
            }
            catch (ArgumentException) { }
            if (result != null) {
                return result;
            }

            try {
                result = _module.ResolveMethod(token);
            }
            catch (ArgumentException) { }
            if (result != null) {
                return result;
            }

            try {
                _module.ResolveField(token);
            }
            catch (ArgumentException) { }

            return result;
        }

        private object ResolveToken(int token, OperandType operandType) {
            return operandType switch {
                OperandType.InlineMethod => _module.ResolveMethod(token, _method.DeclaringType.GetGenericArguments(), null),
                OperandType.InlineField => _module.ResolveField(token, _method.DeclaringType.GetGenericArguments(), null),
                OperandType.InlineSig => _module.ResolveSignature(token),
                OperandType.InlineString => _module.ResolveString(token),
                OperandType.InlineType => _module.ResolveType(token, _method.DeclaringType.GetGenericArguments(), null),
                OperandType.InlineTok => SafeResolveToken(token),
                var x when
                    x == OperandType.ShortInlineI ||
                    x == OperandType.ShortInlineBrTarget ||
                    x == OperandType.ShortInlineVar
                    => token,
                _ => null
            };
        }

        private static OperandTypeInfo? ConvertOperandType(OperandType type) {
            return type switch {
                OperandType.InlineField => OperandTypeInfo.Field,
                OperandType.InlineMethod => OperandTypeInfo.Method,
                OperandType.InlineSig => OperandTypeInfo.Signature,
                OperandType.InlineString => OperandTypeInfo.String,
                OperandType.InlineType => OperandTypeInfo.Type,
                _ => null
            };
        }

        private static string ResolveObjectName(object obj) {
            return obj switch {
                Type type => type.FullName,
                string str => str,
                FieldInfo field => $"{field.DeclaringType.FullName}#{field.Name}",
                MethodInfo method => $"{method.DeclaringType.FullName}#{method.Name}",
                ConstructorInfo constructor => $"{constructor.DeclaringType.FullName}#{constructor.Name}",
                _ => null
            };
        }

        public SerializableMethodBodyGenerator(MethodInfo method) {
            _method = method;
            _module = _method.Module;
        }

        public SerializableMethodBody Generate() {
            var parser = new IlParser(_method);
            var instructions = from instruction in parser.Parse()
                let operand = instruction.OperandToken != null
                    ? ResolveToken(instruction.OperandToken.Value, instruction.OpCode.OperandType)
                    : null
                let parameters = operand is MethodBase method
                    ? method.GetParameters().Select(p => ResolveObjectName(p.ParameterType)).ToArray()
                    : null
                let genericArguments = (operand is MethodBase method && !method.Name.Contains("ctor"))
                    ? method.GetGenericArguments().Select(ResolveObjectName).ToArray()
                    : null
                select new InstructionInfo {
                    Offset = instruction.Offset,
                    Size = instruction.OpCode.Size,
                    OperandInfo = new OperandInfo {
                        OperandType = ConvertOperandType(instruction.OpCode.OperandType),
                        OperandName = ResolveObjectName(operand),
                        ParametersTypesNames = parameters,
                        GenericTypesNames = genericArguments
                    }
                };
            var methodBody = _method.GetMethodBody();
            return new SerializableMethodBody {
                Instructions = instructions.ToList(),
                IlCode = methodBody.GetILAsByteArray(),
                MaxStackSize = methodBody.MaxStackSize,
                LocalVariables = methodBody.LocalVariables.Select(l => 
                    new SerializableLocalVariableInfo {
                        IsPinned = l.IsPinned, 
                        TypeName = l.LocalType.FullName
                    }).ToList()
            };
        }
    }
}
