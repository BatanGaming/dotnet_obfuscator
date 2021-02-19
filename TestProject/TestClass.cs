using System;
using System.Collections;

namespace TestProject
{
    public class TestClass
    {
        /*public virtual int MethodSum(int a, int b) {
            FieldSum = GetMethod();
            FieldSum = (c, d) => { return 2; };
            return FieldSum(a, b);
        }*/

        public int a;
        public int b;

        public int Sum() {
            Method();
            return a + b;
        }

        public Func<int, int, int> FieldSum;

        public void Method() {
            // body
            // call field
        }

        public void Method2() {
            // Method(); [argN, arg1, target]
            // var m = GetMethod()
            // ldloc
            // target
            // 
            // m.Invoke(target, params)
            Method();
        }
    }
}
