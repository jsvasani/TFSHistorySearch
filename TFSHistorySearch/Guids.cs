// Guids.cs
// MUST match guids.h
using System;

namespace NoName.TFSHistorySearch
{
    static class GuidList
    {
        public const string guidTFSHistorySearchPkgString = "8f3b1e9a-9b0f-456c-b9ef-b7b79e9dda7b";
        public const string guidTFSHistorySearchCmdSetString = "9702779a-046a-47e7-825c-5813c6b9db2f";

        public static readonly Guid guidTFSHistorySearchCmdSet = new Guid(guidTFSHistorySearchCmdSetString);
    };
}