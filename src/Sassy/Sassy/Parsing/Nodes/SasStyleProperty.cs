using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using Sassy.Extensions;
using Sassy.Parsing.Tokens;
using UIKit;

namespace Sassy.Parsing.Nodes
{
    public class SasStyleProperty
    {
        internal readonly static ICollection<string> KnownColorSchemes = new ReadOnlyCollection<string>(new List<string>
        {
            "rgb",
            "rgba",
            "hsl",
            "hsla"
        });

        public SasStyleProperty(SasToken nameToken, IEnumerable<SasToken> valueTokens)
        {
            NameToken = nameToken;
            ValueTokens = valueTokens;
        }

        internal IReadOnlyList<SasToken> ConsecutiveValuesOfTokenType(SasTokenType type)
        {
            var tokens = new List<SasToken>();

            foreach (var token in ValueTokens)
            {
                if (token.Type == type)
                {
                    tokens.Add(token);
                }
                else if (tokens.Any() &&
                         !token.IsWhitespace() &&
                         !token.ValueIsEqualsTo(","))
                {
                    return tokens;
                }
            }
            return tokens;
        }

        internal object ValueOfTokenType(SasTokenType type)
        {
            return ValueTokens?
                .Where(token => token.Type == type)
                .Select(token => token.Value)
                .FirstOrDefault();
        }

        public void AddChildStyleProperty(SasStyleProperty styleProperty)
        {
            if (ChildStyleProperties == null)
            {
                ChildStyleProperties = new List<SasStyleProperty>();
            }
            ChildStyleProperties.Add(styleProperty);
        }

        public Dictionary<string, string> Arguments { get; }

        public IList<SasStyleProperty> ChildStyleProperties { get; private set; }

        public string Name
            => NameToken.Value.ToString().AsCamelCased();

        public SasToken NameToken { get; }

        public IList<object> Values
            => ValueTokens?
                .Where(vt => vt.Value != null)
                .Select(vt => vt.Value)
                .ToList();

        public IEnumerable<SasToken> ValueTokens { get; }

        #region Transformations

        internal bool TransformValuesToCGSize(ref CGSize size)
        {
            return TryParse(ref size, SasTokenType.Unit);
        }

        internal bool TransformValuesToCGPoint(ref CGPoint point)
        {
            return TryParse(ref point, SasTokenType.Unit);
        }

        internal bool TransformValuesToCGRect(ref CGRect rect)
        {
            var tokens = ConsecutiveValuesOfTokenType(SasTokenType.Unit);
            if (tokens.Count == 4)
            {
                rect = new CGRect(
                    (double)tokens[0].Value,
                    (double)tokens[1].Value,
                    (double)tokens[2].Value,
                    (double)tokens[3].Value);

                return true;
            }
            return false;
        }

        internal bool TransformValuesToUIEdgeInsets(ref UIEdgeInsets insets)
        {
            var tokens = ConsecutiveValuesOfTokenType(SasTokenType.Unit);
            if (tokens.Count == 1)
            {
                var value = (float)tokens[0].Value;
                insets = new UIEdgeInsets(value, value, value, value);
                return true;
            }
            if (tokens.Count == 2)
            {
                var value1 = (float)tokens[0].Value;
                var value2 = (float)tokens[1].Value;
                insets = new UIEdgeInsets(value1, value2, value1, value2);
                return true;
            }
            if (tokens.Count == 4)
            {
                var value1 = (float)tokens[0].Value;
                var value2 = (float)tokens[1].Value;
                var value3 = (float)tokens[2].Value;
                var value4 = (float)tokens[3].Value;
                insets = new UIEdgeInsets(value1, value2, value3, value4);
                return true;
            }
            return false;
        }

        internal bool TransformValuesToUIOffset(ref UIOffset offset)
        {
            return TryParse(ref offset, SasTokenType.Unit);
        }

        internal bool TransformValuesToUIColor(ref UIColor color)
        {
            var colorValue = ValueOfTokenType(SasTokenType.Color) as UIColor;
            if (colorValue != null)
            {
                color = colorValue;
                return true;
            }

            var value = ValueOfTokenType(SasTokenType.Ref)
                        ?? ValueOfTokenType(SasTokenType.Selector)
                        ?? ValueOfTokenType(SasTokenType.String);

            value = value.ToString().ToLower();

            if (KnownColorSchemes.Contains(value))
            {
                var tokens = ConsecutiveValuesOfTokenType(SasTokenType.Unit);
                var alpha = 1.0f;

                if (tokens.Count < 3)
                    return false;
                else if (tokens.Count == 4)
                {
                    alpha = (float)tokens[3].Value;
                }

                if (value.Equals("rgb") ||
                    value.Equals("rgba"))
                {
                    color = UIColor.FromRGBA(
                        (float)tokens[0].Value,
                        (float)tokens[1].Value,
                        (float)tokens[2].Value,
                        alpha);
                }
                else
                {
                    color = UIColor.FromHSBA(
                        (float)tokens[0].Value / 360.0f,
                        (float)tokens[1].Value / 100.0f,
                        (float)tokens[2].Value / 100.0f,
                        alpha);
                }
            }

            value = value.ToString().AsCamelCased();
            var selector = new Selector($"Color{value}");
            var @class = new NSObject(Class.GetHandle(typeof(UIColor)));
            if (@class.RespondsToSelector(selector))
            {
                color = @class.PerformSelector(selector) as UIColor;
                return true;
            }

            return false;
        }

        private bool TryParse<TRef>(
            ref TRef obj,
            SasTokenType tokenType)
        {
            var tokens = ConsecutiveValuesOfTokenType(tokenType);
            if (tokens.Count == 0)
            {
                var value = (double)tokens[0].Value;
                obj = Instantiator<TRef>.New(value);
                return true;
            }
            if (tokens.Count == 2)
            {
                var val1 = (double)tokens[0].Value;
                var val2 = (double)tokens[1].Value;
                obj = Instantiator<TRef>.New(val1, val2);
                return true;
            }

            return false;
        }

        internal void ResolveExpressions()
        {
            throw new NotImplementedException(
                "https://github.com/cloudkite/Classy/blob/527ec2c98e25f8bdb706ffebf2408f1a43581714/Classy/Parser/CASStyleProperty.m#L311");
        }
        #endregion
    }
}