using System;

namespace TestProject
{
    public class Test
    {
        public int Method2(MainMethodInfo info) {
            var mainInfo = new MainMethodInfo {
                OwnerTypeToken = info.OwnerTypeToken,
                ReturnTypeToken = info.ReturnTypeToken,
                ParameterTypesTokens = new[] { typeof(int).MetadataToken, typeof(int).MetadataToken, typeof(int).MetadataToken, typeof(int).MetadataToken, typeof(int).MetadataToken },
                Name = "method1"
            };
            var method = Program.GetMethod(mainInfo);
            return 2;
        }
    }
}
