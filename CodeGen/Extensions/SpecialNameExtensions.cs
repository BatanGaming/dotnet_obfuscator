using System;
using System.Reflection;

namespace CodeGen.Extensions
{
    public static class SpecialNameExtensions
    {
        public static bool IsSpecialName(this Type type, char[] SpecialCharacters) {
            return type.IsSpecialName || type.Name.IndexOfAny(SpecialCharacters) != -1;
        }
        
        public static bool IsSpecialName(this FieldInfo field, char[] SpecialCharacters) {
            return field.IsSpecialName || field.Name.IndexOfAny(SpecialCharacters) != -1;
        }
        
        public static bool IsSpecialName(this MethodBase method, char[] SpecialCharacters) {
            return method.IsSpecialName || method.Name.IndexOfAny(SpecialCharacters) != -1;
        }
    }
}