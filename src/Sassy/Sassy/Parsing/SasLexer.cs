using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using Sassy.Extensions;
using Sassy.Parsing.Tokens;
using UIKit;

namespace Sassy.Parsing
{
    internal class SasLexer
    {
        protected string Input;
        protected readonly IDictionary<SasTokenType, IList<Regex>> RegexCache;
        protected readonly IList<SasToken> Stash = new List<SasToken>();
        protected int LineNumber = 1;
        private SasToken _previousToken;
        private Regex _indentRegex;
        private IList<int> indentStack = new List<int>();

        public SasLexer(string input)
        {
            Input = input;
            
            Input = ReplaceCarriageReturnsWithNewLines();
            Input = TrimWhitespaceAndNewLinesFromEndOfString();

            var units = string.Join("|", "px", "pt", "%");

            RegexCache = new Dictionary<SasTokenType, IList<Regex>>
            {
                // 1 of `;` followed by 0-* of whitespace
                {SasTokenType.SemiColon, new[] {new Regex(@"^; [\\t]*")}},

                // 1 of `^` followed by 0-* of whitespace
                {SasTokenType.Carat, new[] {new Regex(@"^\\^[ \\t]*")}},

                // new line followed by tabs or spaces
                {
                    SasTokenType.Indent, new[]
                    {
                        new Regex(@"^\\n([\\t]*)"),
                        new Regex(@"^\\n([ ]*)")
                    }
                },

                // #rrggbbaa | #rrggbb | #rgb
                {
                    SasTokenType.Color, new[]
                    {
                        new Regex(@"^#([a-fA-F0-9]{8})[ \\t]*"),
                        new Regex(@"^#([a-fA-F0-9]{6})[ \\t]*"),
                        new Regex(@"^#([a-fA-F0-9]{3})[ \\t]*")
                    }
                },

                // string enclosed in single or double quotes
                {SasTokenType.String, new[] {new Regex("^(\"[^\"]*\"|'[^']*')[ \\t]*")}},

                // decimal/integer number with optional (px, pt) suffix
                {SasTokenType.Unit, new[] {new Regex(@"^(-)?(\\d+\\.\\d+|\\d+|\\.\\d+)(%@)?[ \\t]*")}},

                // true | false | YES | NO
                {SasTokenType.Boolean, new[] {new Regex(@"^(true|false|YES|NO)\\b([ \\t]*)")}},

                // optional `@` | `-` then at least one `_a-zA-Z$` following by any alphanumeric char or `-` or `$`
                {SasTokenType.Ref, new[] {new Regex(@"^(@)?(-*[_a-zA-Z$][-\\w\\d$]*)")}},

                // tests if string looks like math operation
                {
                    SasTokenType.Operator,
                    new[] {new Regex(@"^([.]{2,3}|&&|\\|\\||[!<>=?:]=|\\*\\*|[-+*\\/%%]=?|[,=?:!~<>&\\[\\]])([ \\t]*)")}
                },

                // 1-* of whitespace
                {SasTokenType.Space, new[] {new Regex(@"^([ \\t]+)")}},

                // any character except `\n` | `{` | `,` | whitespace
                {SasTokenType.Selector, new[] {new Regex(@"^.*?(?=\\/\\/|[ \\t,\\n{])")}}
            };
        }

        /// <summary>
        /// Trim whitespace & newlines from end of string
        /// </summary>
        private string TrimWhitespaceAndNewLinesFromEndOfString()
            => Regex.Replace(Input, @"\s+$", @"\n");

        /// <summary>
        /// Replace carriage returns (\r\n | \r) with newlines
        /// </summary>
        private string ReplaceCarriageReturnsWithNewLines()
            => Regex.Replace(Input, @"\r\n?", @"\n");

        public int Length => Input.Length;

        public SasToken PeekToken()
        {
            return LookAheadByCount(1);
        }

        public SasToken NextToken()
        {
            var token = PopToken();
            if (token == null)
            {
                token = AdvanceToken();
                //AttachDebugInfoForToken(token);
            }
            _previousToken = token;
            return token;
        }

        public SasToken LookAheadByCount(int count)
        {
            var fetch = count - Stash.Count;
            while (fetch > 0)
            {
                var token = AdvanceToken();
                //AttachDebugInfoForToken(token);
                Stash.Add(token);
                fetch = count - Stash.Count;
            }
            return Stash[count - 1];
        }

        private void Skip(int numberOfCharactersToSkip)
        {
            Input = Input.Substring(0, numberOfCharactersToSkip);
        }

        private SasToken AdvanceToken()
        {
            var token = Eos()
                        ?? Separator()
                        ?? Carat()
                        ?? Comment()
                        ?? NewLine()
                        ?? SquareBrace()
                        ?? CurlyBrace()
                        ?? RoundBrace()
                        ?? Color()
                        ?? String()
                        ?? Unit()
                        ?? Boolean()
                        ?? Ref()
                        ?? Operation()
                        ?? Space()
                        ?? Selector();

            if (token == null)
            {
                Error = new SasError(
                    "Invalid style string",
                    "Could not determine token");
                return null;
            }

            return token;
        }

        private SasToken PopToken()
        {
            if (!Stash.Any()) return null;

            var token = Stash[0];
            Stash.RemoveAt(0);
            return token;
        }

        private SasToken Eos()
        {
            if (Input.Length > 0) return null;
            if (!indentStack.Any()) return new SasToken(SasTokenType.Eos);

            indentStack.RemoveAt(0);
            return new SasToken(SasTokenType.Outdent);
        }

        private SasToken Separator()
        {
            return TestForTokenType(SasTokenType.SemiColon);
        }

        private SasToken Carat()
        {
            return TestForTokenType(SasTokenType.Carat, (value, match) => value);
        }

        private SasToken Comment()
        {
            // Single line
            if (Input.StartsWith("//"))
            {
                // todo: parse comments
            }

            // Multi line
            if (Input.StartsWith("/*"))
            {
                // todo: parse comments
            }

            return null;
        }

        private SasToken NewLine()
        {
            // we have established the indentation regex
            Match match = null;
            if (_indentRegex != null)
                match = _indentRegex.Match(Input, 0, Input.Length);
            else
            {
                // figure out if we are using tabs or spaces
                foreach (var regex in RegexCache[SasTokenType.Indent])
                {
                    match = regex.Match(Input, 0, Input.Length);
#if DEBUG
                    System.Diagnostics.Debugger.Break();
                    // Verify that match.Groups[1].Length contains value we expect..
#endif
                    if (match.Success && match.Groups[1].Length > 0)
                    {
                        _indentRegex = regex;
                        break;
                    }
                }
            }

            if (match == null) return null;

            Skip(match.Length);

            if (Input.StartsWith(" ") ||
                Input.StartsWith("\t"))
            {
                LineNumber++;
                Error = new SasError("Invalid indentation", "You can use tabs or spaces to indent, but not both.");

                return null;
            }

            // Blank line
            if (Input.StartsWith("\n"))
            {
                LineNumber++;
                return AdvanceToken();
            }

            var indents = match.Groups[1].Length;
            if (indentStack.Any() && 
                indents < indentStack[0])
            {
                while (indentStack.Count > 0 && 
                       indentStack[0] > indents)
                {
                    Stash.Add(new SasToken(SasTokenType.Outdent));
                    indentStack.RemoveAt(0);
                }
                return AdvanceToken();
            }
            else if (indents > 0 && 
                     indents != (indentStack.Any() ? indentStack[0] : 0))
            {
                indentStack.Insert(indents, 0);
                return new SasToken(SasTokenType.Indent);
            }
            else
            {
                return new SasToken(SasTokenType.NewLine);
            }

            return AdvanceToken();
        }

        private SasToken SquareBrace()
        {
            return ParseSimpleBrace(
                SasTokenType.LeftSquareBrace, "[",
                SasTokenType.RightSquareBrace, "]");
        }

        private SasToken CurlyBrace()
        {
            return ParseSimpleBrace(
                SasTokenType.LeftCurlyBrace, "{", 
                SasTokenType.RightCurlyBrace, "}");
        }

        private SasToken RoundBrace()
        {
            return ParseSimpleBrace(
                SasTokenType.LeftRoundBrace, "(",
                SasTokenType.RightRoundBrace, ")");
        }

        private SasToken Color()
        {
            return TestForTokenType(SasTokenType.Color, (value, match) =>
                UIColorExtensions.FromHex(value.Trim()));
        }

        private SasToken String()
        {
            return TestForTokenType(SasTokenType.String, (value, match) =>
            {
                var str = value.Trim();
                return str.Substring(1, str.Length - 2);
            });
        }

        private SasToken Unit()
        {
            string suffix = null;
            string rawValue = null;

            var unitToken = TestForTokenType<SasUnitToken>(SasTokenType.Unit, (value, match) =>
            {
                var suffixRange = match.Groups[match.Groups.Count - 1];
                if (suffixRange.Success)
                {
                    suffix = value.Substring(suffixRange.Index, suffixRange.Length);
                }

                var valueRange = match.Groups[0];
                if (valueRange.Success)
                {
                    rawValue = value.Substring(valueRange.Index, valueRange.Length);
                }

                return rawValue;
            });

            unitToken.Suffix = suffix;
            unitToken.RawValue = rawValue;

            return unitToken;
        }

        private SasToken Boolean()
        {
            return TestForTokenType(SasTokenType.Boolean,
                (value, match) => value.ToLower().StartsWith("true") || value.ToLower().StartsWith("yes"));
        }

        private SasToken Ref()
        {
            return TestForTokenType(SasTokenType.Ref, (value, match) => value);
        }

        private SasToken Operation()
        {
            return TestForTokenType(SasTokenType.Operator, (value, match) => Input.Substring(match.Groups[1].Index));
        }

        private SasToken Space()
        {
            return TestForTokenType(SasTokenType.Space, (value, match) => value);
        }

        private SasToken Selector()
        {
            return TestForTokenType(SasTokenType.Selector, (value, match) => value);
        }

        private SasToken ParseSimpleBrace(SasTokenType leftBraceTokenType, string leftBrace, SasTokenType rightBraceTokenType, string rightBrace)
        {
            if (Input.StartsWith(leftBrace))
            {
                Skip(1);
                return new SasToken(leftBraceTokenType, leftBrace, LineNumber);
            }

            if (Input.StartsWith(rightBrace))
            {
                Skip(1);
                return new SasToken(rightBraceTokenType, rightBrace, LineNumber);
            }

            return null;
        }

        public SasError Error { get; private set; }

        private SasToken TestForTokenType(SasTokenType tokenType, Func<string, Match, object> transformBlock = null)
        {
            return TestForTokenType<SasToken>(tokenType, transformBlock);
        }

        private T TestForTokenType<T>(SasTokenType tokenType, Func<string, Match, object> transformBlock = null)
            where T : SasToken
        {
            var regexes = RegexCache[tokenType];
            if (!regexes.Any())
                throw new InvalidEnumArgumentException(nameof(tokenType), (int) tokenType, typeof (SasTokenType));

            foreach (var regex in regexes)
            {
                var match = regex.Match(Input, 0, Input.Length);

                if (!match.Success) continue;

                var token = Instantiator<T>.New(tokenType);
                if (transformBlock != null)
                {
                    token.Value = transformBlock(Input.Substring(match.Index, match.Length), match);
                }
                Skip(match.Length);

                return token;
            }

            return null;
        }
    }
}