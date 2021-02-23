using System;

namespace TestAssembly
{
    public static class Program
    {
        public static void Main(string[] args) {
            var c = new Class2(2,3);
            Console.WriteLine(c.get());
            Console.WriteLine(c.SumFactorial(10));
        }
    }
}