namespace CodeGen.Templates
{
    public interface Template
    {
        public string Template { get; }

        public string Overwrite();
    }
}