using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using CodeGen;
using System.Linq;
using System.Text.Json;
using CodeGen.Generators;
using CodeGen.Models;

namespace TestProject
{
    public static class Program
    {
        public static void Main(string[] args) {
            var generator = new AssemblyGenerator(Assembly.LoadFile(Path.GetFullPath("TestAssembly.dll")));
            generator.GenerateAssembly();
        }
    }
}