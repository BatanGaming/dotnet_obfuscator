namespace CodeGen
{
    public static class Extensions
    {
        public static string Capitalize(this string str) {
            return char.ToUpper(str[0]) + str.Substring(1);
        }
    }
}