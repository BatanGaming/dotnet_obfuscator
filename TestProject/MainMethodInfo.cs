using System;
using System.Collections.Generic;
using System.Linq;

namespace TestProject
{
    [Serializable]
    public class MainMethodInfo
    {
        public string Name { get; set; }
        public int ReturnTypeToken { get; set; }
        public int[] ParameterTypesTokens { get; set; }
        public int OwnerTypeToken { get; set; }

        public override int GetHashCode() {
            var hash = new HashCode();
            hash.Add(Name);
            hash.Add(ReturnTypeToken);
            hash.Add(OwnerTypeToken);
            foreach (var parameter in ParameterTypesTokens) {
                hash.Add(parameter);
            }

            return hash.ToHashCode();
        }

        public override bool Equals(object? obj) {
            if (ReferenceEquals(this, obj)) {
                return true;
            }

            if (ReferenceEquals(this, null)) {
                return false;
            }

            if (ReferenceEquals(obj, null)) {
                return false;
            }

            if (GetType() != obj.GetType()) {
                return false;
            }

            var info = obj as MainMethodInfo;
            if (ParameterTypesTokens.Length != info.ParameterTypesTokens.Length) {
                return false;
            }

            if (ParameterTypesTokens.Where((t, i) => t != info.ParameterTypesTokens[i]).Any()) {
                return false;
            }

            return Name == info.Name && ReturnTypeToken == info.ReturnTypeToken &&
                   OwnerTypeToken == info.OwnerTypeToken;
        }

    }
}
