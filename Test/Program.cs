using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Generator;

namespace Test
{
    class Program
    {
        static void Main(string[] args) {
            using var writer = new StreamWriter("code.txt");
            var generator = new AssemblyGenerator(@"D:\Projects\Packer\Generator\bin\Debug\netcoreapp3.1\Generator.dll");
            var code = generator.Generate();
            writer.Write(code);
        }
    }
}
