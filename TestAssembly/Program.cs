using System;
using System.Collections.Generic;

namespace TestAssembly
{
    public static class Program
    {
        public static void Main(string[] args) {
            var a = new List<int>();
            var b = new List<string>();
            a.Add(1);
            Console.WriteLine(a[0]);
            b.Add("123");
            Console.WriteLine(b[0]);
            var c = new Class1<List<int>>();
            c.Field.Add(2);
            Console.WriteLine(c.Field[0]);
            var d = c.CreateNew();
            d.Add(3);
            Console.WriteLine(d[0]);
        }
    }
}