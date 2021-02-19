using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;

namespace $NAMESPACE
{
    public static class Program
    {
        public static void Main(string[] args) {
            // MAIN PART START

            var assembly_name = new AssemblyName("$ASSEMBLY_NAME");
            var assembly_builder = AssemblyBuilder.DefineDynamicAssembly(assembly_name, AssemblyBuilderAccess.Run);
            var module_builder = assembly_builder.DefineDynamicModule(assembly_name.Name + ".dll");

            // MAIN PART END
            
            // TYPES START

            $TYPES

            // TYPES END
            
            // FIELDS START
            
            $FIELDS
            
            // FIELDS END

            // METHODS DEFINITIONS START
            
            $METHODS_DEFINITIONS
            
            // METHODS DEFINITIONS END
            
            // METHODS BODIES START
            
            $METHODS_BODIES
            
            // METHODS BODIES END

            // CREATING TYPES

            $CREATED_TYPES
        }
    }
}