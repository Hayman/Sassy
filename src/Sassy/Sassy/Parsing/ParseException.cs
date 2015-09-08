using System;
using System.Runtime.Serialization;

namespace Sassy.Parsing
{
    public class ParseException
        : Exception
    {
        public ParseException(string message) : base(message)
        {
        }

        public ParseException(string message, Exception inner) : base(message, inner)
        {
        }

        public ParseException(SasError error)
            : base(error.Title)
        {
            Description = error.Description;
        }

        protected ParseException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }

        public string Description { get; protected set; }
    }
}