using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace TestProject
{
    public class TestClass
    {
        public static BigInteger OldFactorial(int n) {
            var result = 1;
            for (var i = 2; i < n; ++i) {
                result *= i;
            }

            return result;
        }

        public static BigInteger TestFactorial(int n) {
            return n == 1
                ? 1
                : n * TestFactorial(n - 1);
        }

        public BigInteger TestFactorialMany(int n) {
            var list = Enumerable.Range(1, n);
            BigInteger sum = 0;
            foreach (var i in list) {
                sum += TestFactorial(i);
            }

            return sum;
        }
        
        public static BigInteger Factorial(int n) {
            FieldFactorial = (Func<int, BigInteger>) Program.GetMethod(null);
            var result = FieldFactorial(n);
            FieldFactorial = null;
            return result;
        }

        private static Func<int, BigInteger> FieldFactorial;
        
        public static BigInteger OldFactorialMany(int n) {
            var list = Enumerable.Range(1, n);
            BigInteger sum = 0;
            foreach (var i in list) {
                sum += Factorial(i);
            }

            return sum;
        }

        public BigInteger FactorialMany(int n) {
            FieldFactorialMany = (Func<int, BigInteger>) Program.GetMethod(this);
            var result = FieldFactorialMany(n);
            FieldFactorialMany = null;
            return result;
        }

        private Func<int, BigInteger> FieldFactorialMany;
    }
}
