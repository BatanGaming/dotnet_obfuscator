using System;
using System.Reflection;

namespace CodeGen.Extensions
{
    public static class CommonExtensions
    {
        public static string Capitalize(this string str) {
            return char.ToUpper(str[0]) + str.Substring(1);
        }

        public static MethodInfo[] GetAllMethods(this Type type) {
            return type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic |
                                   BindingFlags.Instance | BindingFlags.Static);
        }
        public static ConstructorInfo[] GetAllConstructors(this Type type) {
            return type.GetConstructors(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic |
                                   BindingFlags.Instance | BindingFlags.Static);
        }
        
        public static FieldInfo[] GetAllFields(this Type type) {
            return type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic |
                                   BindingFlags.Instance | BindingFlags.Static);
        }
    }
}