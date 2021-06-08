using System.Collections.Generic;

namespace CodeGen
{
    public static class DuplicatesFixer
    {
        private static readonly List<string> Strings = new List<string>();

        public static string Fix(string str) {
            if (Strings.Contains(str)) {
                str = $"{str}_{Strings.Count}";
            }
            Strings.Add(str);
            return str;

        }
    }
}
