using System;
using System.Reflection;

namespace CodeGen.Extensions
{
    public static class SpecialNameExtensions
    {
        private static readonly char[] SpecialCharacters = {'.', '<', '>'};
        
        public static bool IsSpecialName(this Type type) {
            return type.IsSpecialName || type.Name.IndexOfAny(SpecialCharacters) != -1;
        }
        
        public static bool IsSpecialName(this FieldInfo field) {
            return field.IsSpecialName || field.Name.IndexOfAny(SpecialCharacters) != -1;
        }
        
        public static bool IsSpecialName(this MethodBase method) {
            return method.IsSpecialName || method.Name.IndexOfAny(SpecialCharacters) != -1;
        }
    }
}