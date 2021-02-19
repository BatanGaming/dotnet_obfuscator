using System;

namespace TestProject
{
    class TestClass2 : TestClass
    {
        private Func<int, int, int> FieldSum;
        /*public override int MethodSum(int a, int b) {
            return base.MethodSum(a, b) + FieldSum(a,b);
        }*/
    }
}
