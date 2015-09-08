namespace Sassy.Parsing.Tokens
{
    public enum SasTokenType
    {
        Unknown,
        Indent,
        Outdent,
        Eos,
        SemiColon,
        Carat,
        NewLine,
        LeftSquareBrace,
        RightSquareBrace,
        LeftCurlyBrace,
        RightCurlyBrace,
        LeftRoundBrace,
        RightRoundBrace,
        Color,
        String,
        Unit,
        Boolean,
        Ref,
        Operator,
        Space,
        Selector
    }
}