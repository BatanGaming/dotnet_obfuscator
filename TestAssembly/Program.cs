using System;
using System.Collections.Generic;
using System.Linq;

namespace TestAssembly
{
    public static class Program
    {
        public static void Main(string[] args) {
            var obj = new TestClass<List<int>, int>();
            Console.WriteLine("Calling");
            obj.View(Enumerable.Range(0, 10).ToList());
            Console.WriteLine("Called");
        }
    }
}