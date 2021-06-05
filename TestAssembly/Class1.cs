using System;
using System.Collections.Generic;

namespace TestAssembly
{
    public class TestClass<T, TEntity> where T: IEnumerable<TEntity>
    {
        public void View(T list){
            foreach (var entity in list) {
                Console.WriteLine(entity);
            }
        }
    }
}