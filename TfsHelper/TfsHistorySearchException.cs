using System;

namespace TfsHelperLib
{
    public class TfsHistorySearchException : Exception
    {
        public TfsHistorySearchException() { }
        public TfsHistorySearchException(string message) : base(message) { }
        public TfsHistorySearchException(string message, Exception inner) : base(message, inner) { }
        protected TfsHistorySearchException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
