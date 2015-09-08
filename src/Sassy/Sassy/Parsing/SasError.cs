namespace Sassy.Parsing
{
    public class SasError
    {
        public string Title { get; }
        public string Description { get; }

        public SasError(string title, string description = "")
        {
            Title = title;
            Description = description;
        }
    }
}