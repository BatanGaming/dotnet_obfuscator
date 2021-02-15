using System.Reflection;

namespace CodeGen
{
    public class SerializableMethodGenerator
    {
        private readonly MethodInfo _method;

        public SerializableMethodGenerator(MethodInfo method) {
            _method = method;
        }

        public string Generate() {

        }
    }
}
