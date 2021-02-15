using System.Linq;
using System.Reflection;
using System.Text;

namespace CodeGen
{
    public class AssemblyWithJitGenerator : AssemblyGenerator
    {
        public AssemblyWithJitGenerator(Assembly assembly) : base(assembly) { }

        protected override void GenerateMethodsDefinitions() {
            
        }

        protected override void GenerateMethodBodies() {
            var builder = new StringBuilder();
            foreach (var type in _assembly.DefinedTypes.ToList()) {
                foreach (var method in type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)) {
                    var code = new SerializableMethodGenerator(method).Generate();
                    builder.AppendLine(code);
                }
            }

            WriteSection("$METHODS_BODIES_JIT", builder.ToString());
        }
    }
}
