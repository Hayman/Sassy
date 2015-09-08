using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Sassy.Parsing.Nodes;
using Sassy.Parsing.Tokens;

namespace Sassy.Parsing
{
    public sealed class SasParser
    {
        private string _filePath;
        private List<string> _importedFileNames;
        private SasLexer _lexer;
        private IList<SasStyleNode> _styleNodes;
        private IDictionary<string, SasStyleProperty> _styleVars;

        public static SasParser FromFilePath(string filePath, IDictionary<string, SasStyleProperty> variables)
        {
            var contents = File.ReadAllText(filePath, Encoding.UTF8);

            if (string.IsNullOrWhiteSpace(contents))
            {
                throw new ParseException(
                    new SasError(
                        $"Could not parse file: '{filePath}'",
                        "File does not exist or is empty"));
            }

            Console.WriteLine("Start parsing file");

            var parser = new SasParser
            {
                _filePath = filePath,
                _styleVars = new Dictionary<string, SasStyleProperty>()
            };

            // Transform variables into tokens
            foreach (var variable in variables)
            {
                var obj = variable.Value;
                if (obj != null)
                {
                    parser._styleVars[variable.Key] = obj;
                    break;
                }
            }

            parser._styleNodes = parser.ParseString(contents);

            return parser;
        }

        public IList<SasStyleNode> ParseString(string input)
        {
            _lexer = new SasLexer(input);
            _importedFileNames = new List<string>();

            var allStyleNodes = new List<SasStyleNode>();
            var styleNodesStack = new List<SasStyleNode>();
            var stylePropertiesStack = new List<SasStyleProperty>();

            while (_lexer.PeekToken().Type != SasTokenType.Eos)
            {
                if (Error != null)
                {
                    return null;
                }

                //check for import
                if (_lexer.PeekToken().Type.Equals(SasTokenType.Ref) &&
                    _lexer.PeekToken().ValueIsEqualsTo("@import"))
                {
                    if (styleNodesStack.Any())
                    {
                        // can't have vars inside styleNodes
                        throw new ParseException(new SasError("@import cannot be used inside style selectors"));
                    }

                    // Skip import token
                    _lexer.NextToken();

                    // Skip Whitespace
                    ConsumeTokensMatching(token => token.Type.Equals(SasTokenType.Space));

                    var fileNameComponents = new List<string>();
                    // Combine all following tokens until newline | ;
                    while (_lexer.PeekToken().Type != SasTokenType.NewLine &&
                           _lexer.PeekToken().Type != SasTokenType.SemiColon &&
                           _lexer.PeekToken().Type != SasTokenType.Eos)
                    {
                        fileNameComponents.Add(_lexer.NextToken().Value.ToString().Trim());
                    }

                    var fileName = string.Join(string.Empty, fileNameComponents);

                    if (fileName.StartsWith("$"))
                    {
                        var property = _styleVars[fileName];
                        if (property != null)
                        {
                            fileName = string.Join(string.Empty, property.Values);
                        }
                    }

                    if (string.IsNullOrEmpty(fileName))
                    {
                        throw new ParseException(new SasError("@import does not specify file to import"));
                    }

                    _importedFileNames.Add(fileName);
                    

                    SasParser parser = null;
                    try
                    {
                        var filePath = Path.Combine(new FileInfo(_filePath).Directory.ToString(), fileName);
                        parser = SasParser.FromFilePath(filePath, _styleVars);
                    }
                    catch (Exception exception)
                    {
                        throw new ParseException(new SasError(exception.Message, exception.StackTrace));
                    }

                    allStyleNodes.AddRange(parser._styleNodes);
                    foreach (var property in parser._styleVars)
                    {
                        _styleVars.Add(property);
                    }
                    _importedFileNames.AddRange(parser._importedFileNames);

                    ConsumeTokensMatching(token => _lexer.PeekToken().Type == SasTokenType.NewLine ||
                                                   _lexer.PeekToken().Type == SasTokenType.SemiColon);

                    continue;
                }

                var styleNodes = _styleNodes;
                if (styleNodes.Any())
                {
                    // Todo: flatten stylenodes?

                    //[allStyleNodes addObjectsFromArray:styleNodes];
                    //[styleNodesStack addObject:styleNodes];
                    //[self consumeTokenOfType:CASTokenTypeLeftCurlyBrace];
                    //[self consumeTokenOfType:CASTokenTypeIndent];
                    continue;
                }

                var styleVar = NextStyleVar();
                if (Error != null)
                {
                    return null;
                }

                if (styleVar != null)
                {
                    if (styleNodesStack.Any())
                    {
                        // Can't have var's inside styleNodes
                        throw new ParseException(new SasError(
                            "Variables cannot be declared inside style selectors",
                            $"Variable: {styleVar}"));
                    }

                    styleVar.ResolveExpressions();
                    _styleVars[styleVar.NameToken.Value.ToString()] = styleVar;
                    ConsumeTokensMatching(
                        token => token.Type.Equals(SasTokenType.Space) ||
                                 token.Type.Equals(SasTokenType.SemiColon));
                    continue;
                }

                // not a style group therefore must be a property
                SasStyleProperty styleProperty = null;
                var isStylePropertyParent = 
                    NextStylePropertyIsParent(ref styleProperty);
                if (Error != null)
                {
                    throw new ParseException(Error);
                }

                if (styleProperty != null)
                {
                    if (!styleNodesStack.Any())
                    {
                        throw new ParseException(new SasError(
                            "Invalid style property",
                            "Needs to be within a style node"));
                    }

                    styleProperty.ResolveExpressions();

                    if (stylePropertiesStack.Any())
                    {
                        var parent = stylePropertiesStack.Last();
                        parent.AddChildStyleProperty(styleProperty);
                    }
                    else
                    {
                        // Todo: Does this function correctly ???
                        // https://github.com/cloudkite/Classy/blob/master/Classy/Parser/CASParser.m#L288
                        styleNodesStack.Last().AddStyleProperty(styleProperty);
                    }

                    if (isStylePropertyParent)
                        stylePropertiesStack.Add(styleProperty);

                    continue;
                }

                var previousLength = -1;
                SasToken previousToken = null;
                var acceptableToken = ConsumeTokensMatching(token =>
                {
                    var popStack = token.Type == SasTokenType.Outdent ||
                                   token.Type == SasTokenType.RightCurlyBrace;

                    // Ensure we don't double pop
                    var alreadyPopped = _lexer.Length > 0 &&
                                        previousLength > 0 &&
                                        previousToken.Type.Equals(SasTokenType.Outdent) &&
                                        token.Type.Equals(SasTokenType.RightCurlyBrace);

                    if (!alreadyPopped && popStack)
                    {
                        if (stylePropertiesStack.Any())
                        {
                            stylePropertiesStack.RemoveAt(stylePropertiesStack.Count - 1);
                        }
                        else if (styleNodesStack.Any())
                        {
                            styleNodesStack.RemoveAt(styleNodesStack.Count - 1);
                        }
                    }

                    previousLength = _lexer.Length;
                    previousToken = token;

                    return popStack ||
                           token.IsWhitespace() ||
                           token.Type.Equals(SasTokenType.SemiColon);
                });

                if (!acceptableToken)
                {
                    throw new ParseException(new SasError(
                        $"Unexpected token {_lexer.NextToken()}",
                        "Token does not belong in current context"));
                }
            }

            return allStyleNodes;
        }

        private bool NextStylePropertyIsParent(ref SasStyleProperty styleProperty)
        {
            throw new NotImplementedException();
        }

        private bool ConsumeTokensMatching(Func<SasToken, bool> matchFunc)
        {
            var anyMatches = false;
            while (matchFunc(_lexer.PeekToken()))
            {
                anyMatches = true;
                _lexer.NextToken();
            }
            return anyMatches;
        }

        private SasStyleProperty NextStyleVar()
        {
            throw new NotImplementedException(
                "https://github.com/cloudkite/Classy/blob/master/Classy/Parser/CASParser.m#L383");
        }

        public SasError Error { get; set; }
    }
}