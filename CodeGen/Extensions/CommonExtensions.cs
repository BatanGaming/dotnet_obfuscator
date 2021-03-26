namespace CodeGen.Extensions
{
    public static class CommonExtensions
    {
        public static string Capitalize(this string str) {
            return char.ToUpper(str[0]) + str.Substring(1);
        }
    }
}