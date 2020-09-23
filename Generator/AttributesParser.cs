using System.Linq;

namespace Generator
{
    internal static class AttributesParser
    {
        public static string Parse(object obj) {
            var attributes = obj.GetType().GetProperty("Attributes");
            return string.Join(" | ", 
                from attribute in attributes.GetValue(obj).ToString().Split(',')
                select $"{attributes.PropertyType.FullName}.{attribute.Trim()}"
                );
        }
    }
}
