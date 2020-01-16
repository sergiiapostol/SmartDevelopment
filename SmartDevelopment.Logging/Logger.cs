using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace SmartDevelopment.Logging
{
    public class Logger<TSource> : ILogger<TSource>
    {
        private const string MessageKey = "message";

        private readonly Microsoft.Extensions.Logging.ILogger<TSource> _logger;

        private readonly LoggerSettings _loggerSettings;

        public Logger(Microsoft.Extensions.Logging.ILogger<TSource> logger, LoggerSettings settings)
        {
            _logger = logger;
            _loggerSettings = settings;
        }

        private static Dictionary<string, string> BuildProperties(Dictionary<string, string> extra = null)
        {
            var @params = new Dictionary<string, string>();

            if (extra?.Count > 0)
                @params["Extra"] = JsonConvert.SerializeObject(extra);

            return @params;
        }

        private static Dictionary<string, string> ToDictionary(IDictionary source)
        {
            return source?.Keys.Cast<object>().ToDictionary(key => key.ToString(), key => source[key]?.ToString());
        }

        private readonly EventId _eventId = new EventId(1);

        public void Exception(Exception ex)
        {
            var extra = _loggerSettings.ExceptionDetailed ? ToDictionary(ex.Data) : null;
            var state = PrepareStateObject(BuildProperties(extra), null);

            _logger.Log(LogLevel.Error, _eventId, state, ex, ExceptionMessageGetter);
        }

        public void Exception(string ex)
        {
            _logger.LogError(ex);
        }

        public void Trace(string message)
        {
            var state = PrepareStateObject(BuildProperties(), message);

            _logger.Log(LogLevel.Trace, _eventId, state, null, MessageGetter);
        }

        public void Warning(string message, Dictionary<string, string> extra = null)
        {
            var extraData = _loggerSettings.WarningDetailed ? extra : null;
            var state = PrepareStateObject(BuildProperties(extraData), message);

            _logger.Log(LogLevel.Warning, _eventId, state, null, MessageGetter);
        }

        public void Warning(Exception ex, Dictionary<string, string> extra = null)
        {
            var extraData = _loggerSettings.WarningDetailed ? ToDictionary(ex.Data) : null;
            var state = PrepareStateObject(BuildProperties(extraData), null);

            _logger.Log(LogLevel.Warning, _eventId, state, ex, ExceptionMessageGetter);
        }

        public void Debug(string message, Dictionary<string, string> extra = null)
        {
            var state = PrepareStateObject(BuildProperties(extra), message);

            _logger.Log(LogLevel.Debug, _eventId, state, null, MessageGetter);
        }

        public void Information(string message, Dictionary<string, string> extra = null)
        {
            var state = PrepareStateObject(BuildProperties(extra), message);

            _logger.Log(LogLevel.Information, _eventId, state, null, MessageGetter);
        }

        private IReadOnlyList<KeyValuePair<string, object>> PrepareStateObject(Dictionary<string, string> extra,
            string message)
        {
            var state = extra == null
                ? new Dictionary<string, object>()
                : extra.Where(v => !string.IsNullOrEmpty(v.Value)).ToDictionary(v => v.Key, v => (object)v.Value);
            if (!string.IsNullOrEmpty(message))
                state[MessageKey] = message;

            return state.ToImmutableList();
        }

        public void Debug(Exception ex, Dictionary<string, string> extra = null)
        {
            _logger.LogDebug(ex, ex.Message);
        }

        private Func<IReadOnlyList<KeyValuePair<string, object>>, Exception, string> MessageGetter => (state, _) => state.FirstOrDefault(v => v.Key.Equals(MessageKey)).Value?.ToString();

        private Func<IReadOnlyList<KeyValuePair<string, object>>, Exception, string> ExceptionMessageGetter => (_,
            exception) => exception.Message;
    }
}