using System;
using System.Collections.Generic;

namespace SmartDevelopment.Logging
{
    public interface ILogger
    {
        void Exception(Exception ex);

        void Trace(string message);

        void Debug(string message, Dictionary<string, string> extra = null);

        void Debug(Exception ex, Dictionary<string, string> extra = null);

        void Information(string message);

        void Warning(string message, Dictionary<string, string> extra = null);

        void Warning(Exception ex, Dictionary<string, string> extra = null);
    }

    public interface ILogger<TSource> : ILogger { }
}