using System;

namespace DynamicAssembly
{
    public class Program
    {
        public static int Factorial(int n) {
            if (n == 0 || n == 1) {
                return 1;
            }

            return Factorial(n - 1) * n;
        }
        public static void Main(string[] args) {
            Console.WriteLine($"Factorial({args[0]}) = {Factorial(int.Parse(args[0]))}");
        }
    }
}
