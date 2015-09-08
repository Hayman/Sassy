using System.Collections.Generic;
using System.ComponentModel;

namespace Sassy.Parsing.Tokens
{
    public class SasToken
    {
        internal static readonly Dictionary<SasTokenType, string> Factory = new Dictionary<SasTokenType, string>
        {
            {SasTokenType.Unknown, "unknown"},
            {SasTokenType.Indent, "indent"},
            {SasTokenType.Outdent, "outdent"},
            {SasTokenType.Eos, "EOS"},
            {SasTokenType.SemiColon, "semicolon"},
            {SasTokenType.Carat, "carat"},
            {SasTokenType.NewLine, "newline"},
            {SasTokenType.LeftSquareBrace, "left square brace"},
            {SasTokenType.RightSquareBrace, "right square brace"},
            {SasTokenType.LeftCurlyBrace, "left curly brace"},
            {SasTokenType.RightCurlyBrace, "right curly brace"},
            {SasTokenType.LeftRoundBrace, "left round brace"},
            {SasTokenType.RightRoundBrace, "right round brace"},
            {SasTokenType.Color, "color"},
            {SasTokenType.String, "string"},
            {SasTokenType.Unit, "unit"},
            {SasTokenType.Boolean, "boolean"},
            {SasTokenType.Ref, "ref"},
            {SasTokenType.Operator, "operator"},
            {SasTokenType.Space, "space"},
            {SasTokenType.Selector, "selector"}
        };

        public SasToken(SasTokenType type, object value = null, int lineNumber = -1)
        {
            Type = type;
            Value = value;
            LineNumber = lineNumber;
        }

        public SasTokenType Type { get; }

        public object Value { get; set; }

        public string Description
            => $"{StringForType(Type)} {Value}";

        public int LineNumber { get; }

        public static string StringForType(SasTokenType type)
        {
            if (Factory.ContainsKey(type))
                return Factory[type];

            throw new InvalidEnumArgumentException(nameof(type), (int) type, typeof (SasTokenType));
        }

        public bool ValueIsEqualsTo(object value)
        {
            return Value.Equals(value);
        }

        public bool IsWhitespace()
        {
            return
                Type == SasTokenType.Indent ||
                Type == SasTokenType.Outdent ||
                Type == SasTokenType.NewLine ||
                Type == SasTokenType.Space;
        }

        public bool IsPossiblySelector()
        {
            return
                Type == SasTokenType.Ref ||
                Type == SasTokenType.Carat ||
                Type == SasTokenType.LeftSquareBrace ||
                Type == SasTokenType.RightSquareBrace ||
                Type == SasTokenType.Selector ||
                Type == SasTokenType.NewLine ||
                Type == SasTokenType.Space ||
                Type == SasTokenType.Operator ||
                ValueIsEqualsTo(":") ||
                ValueIsEqualsTo(",");
        }

        public bool IsPossiblyVar()
        {
            return
                Type == SasTokenType.Indent ||
                Type == SasTokenType.Space ||
                Type == SasTokenType.Ref ||
                ValueIsEqualsTo("=");
        }

        public bool IsPossiblyExpression()
        {
            return
                Type == SasTokenType.Unit ||
                Type == SasTokenType.Space ||
                Type == SasTokenType.LeftRoundBrace ||
                Type == SasTokenType.RightRoundBrace ||
                Type == SasTokenType.Operator;
        }

        public bool IsPossiblyDelimiter()
        {
            return
                Type == SasTokenType.LeftCurlyBrace ||
                Type == SasTokenType.Indent;
        }
    }
}