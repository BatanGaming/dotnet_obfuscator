namespace CodeGen.Templates
{
    public interface BaseTemplate
    {
        public string Template { get; set; }

        public string Overwrite();
    }
}