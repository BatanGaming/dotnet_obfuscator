using System;
using System.Linq;

namespace TestAssembly
{
    public static class Program
    {
        public static void Main(string[] args) {
            /*var c = new Class2(2,3);
            Console.WriteLine(c.get());
            Console.WriteLine(c.SumFactorial(10));*/
            var list = from i in Enumerable.Range(1, 100) where i % 2 == 0 select i;
            foreach (var i in list) {
                Console.WriteLine(i);
            }
        }
    }
}