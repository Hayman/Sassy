namespace Sassy.Parsing.Tokens
{
    public class SasUnitToken
        : SasToken
    {
        public SasUnitToken(SasTokenType tokenType, object value)
            : base(tokenType, value)
        {
        }

        public string Suffix { get; set; }

        public string RawValue { get; set; }
    }
}