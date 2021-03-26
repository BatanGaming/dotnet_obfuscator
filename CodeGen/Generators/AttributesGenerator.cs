using System.Linq;

namespace CodeGen.Generators
{
    public static class AttributesGenerator
    {
        public static string Generate(object obj) {
            var attributes = obj.GetType().GetProperty("Attributes");
            return string.Join(" | ", 
                from attribute in attributes.GetValue(obj).ToString().Split(',')
                select $"{attributes.PropertyType.FullName}.{attribute.Trim()}"
            );
        }
    }
}