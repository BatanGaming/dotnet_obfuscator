using System.Linq;

namespace CodeGen.Generators
{
    public static class AttributesGenerator
    {
        public static string Generate(object attributes) {
            return string.Join(" | ", 
                from attribute in attributes.ToString().Split(',')
                select $"{attributes.GetType().FullName}.{attribute.Trim()}"
            );
        }
    }
}