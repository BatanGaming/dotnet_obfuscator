using System.Linq;
using System.Numerics;

namespace TestAssembly
{
    public class Class1
    {
        private int _a;

        public Class1(int a) {
            _a = a;
        }
        
        
        
        public static BigInteger Factorial(int n) {
            BigInteger result = 1;
            for (var i = 2; i <= n; ++i) {
                result *= i;
            }

            return result;
        }
        
        public BigInteger SumFactorial(int n) {
            var list = Enumerable.Range(1, n);
            return list.Aggregate<int, BigInteger>(0, (current, i) => current + Factorial(i));
        }

        public virtual int get() {
            return _a;
        }
    }
}