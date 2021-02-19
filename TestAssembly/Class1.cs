using System;
using System.Collections.Generic;

namespace TestAssembly
{
    public class Class1
    {
        public static int Factorial(int n) {
            return n == 1
                ? 1
                : n * Factorial(n - 1);
        }

        public void FactorialMany() {
            var list = new List<int> {1, 2, 3, 4, 5, 6, 7, 8, 9, 10};
            foreach (var i in list) {
                Console.WriteLine(Factorial(i));
            }
        }
    }
}