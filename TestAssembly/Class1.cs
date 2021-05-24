using System;

namespace TestAssembly
{
    public class Class1<T> where T: new()
    {
        public T Field;

        public Class1() {
            Field = new T();
        }

        public T CreateNew() {
            return new T();
        }
    }
}