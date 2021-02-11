using System.Reflection;

namespace CodeGen
{
    public class LocalVariableGenerator
    {
        private readonly LocalVariableInfo _localVariable;

        public LocalVariableGenerator(LocalVariableInfo localVariable) {
            _localVariable = localVariable;
        }

        public string Generate() {
            return $@"DeclareLocal({CommonGenerator.ResolveTypeName(_localVariable.LocalType)}, {_localVariable.IsPinned.ToString().ToLower()})";
        }
    }
}