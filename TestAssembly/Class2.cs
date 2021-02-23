namespace TestAssembly
{
    public class Class2 : Class1
    {
        private int _b;
        public Class2(int a, int b) : base(a) {
            _b = b;
        }

        public override int get() {
            return base.get() + _b;
        }
    }
}