
// The source code below is included via a T4 template.
// The namespace must be defined in that template.

    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Text;
    
    /// <summary>
    /// Leightweight Log stub for Logging-SDK-agnostic logging.
    /// Users of this library can use this class as a leighweight redirect to whatever log technology is used for output.
    /// This allows to avoid creating complex logging abstractions (or taking dependencies on ILogger) for now.
    /// We copy this simply class to each assembly once, becasue we need to change the namespace to avoid ambuguity.
    /// 
    /// For example:
    /// 
    /// Library "Datadog.AutoInstrumentation.Profiler.Managed.dll" gets a copy of this file with the adjusted namespace:
    /// 
    /// <code>
    ///   namespace Datadog.AutoInstrumentation.Profiler.Managed
    ///   {
    ///       public static class Log
    ///       {
    ///       . . .
    ///       }
    ///   }
    /// </code>
    /// 
    /// Library "Datadog.AutoInstrumentation.Tracer.Managed.dll" gets a copy of this file with the adjusted namespace:
    /// 
    /// <code>
    ///   namespace Datadog.AutoInstrumentation.Tracer.Managed
    ///   {
    ///       public static class Log
    ///       {
    ///       . . .
    ///       }
    ///   }
    /// </code>  
    /// 
    /// Each librry can now make Log statements, for example:
    /// 
    /// <code>
    ///   Log.Info("DataExporter", "Data transport started", "size", _size, "otherAttribute", _otherAttribute);
    /// </code>  
    /// 
    /// Another composing library "Datadog.AutoInstrumentation.ProductComposer.dll" the uses the two above libraries uses some particular logging system.
    /// It wants to redirect the logs of its components accordingly.
    /// It creates a trivial adaper and configures the indirection:
    /// 
    /// <code>
    ///   namespace Datadog.AutoInstrumentation.ProductComposer
    ///   {
    ///       using ComposerLogAdapter = Datadog.AutoInstrumentation.ProductComposer.LogAdapter;
    ///       using ProfilerLog = Datadog.AutoInstrumentation.Profiler.Managed.Log;
    ///       using TracerLog = Datadog.AutoInstrumentation.Tracer.Managed.Log;
    ///       
    ///       internal static class LogAdapter
    ///       {
    ///           static LogAdapter()
    ///           {
    ///               // Redirect the logs from the libraries being composed to the coposer's processors:
    ///   
    ///               ProfilerLog.Configure.Error((component, msg, ex, data) => ComposerLogAdapter.Error("Profiler", component, msg, ex, data));
    ///               ProfilerLog.Configure.Info((component, msg, data) => ComposerLogAdapter.Info("Profiler", component, msg, data));
    ///               ProfilerLog.Configure.Debug((component, msg, data) => ComposerLogAdapter.Debug("Profiler", component, msg, data));
    ///               ProfilerLog.Configure.DebugLoggingEnabled(ComposerLogAdapter.IsDebugLoggingEnabled);
    ///   
    ///               TracerLog.Configure.Error((component, msg, rx, data) => ComposerLogAdapter.ErrorMessage("Tracer", component, msg, ex, data));
    ///               TracerLog.Configure.Info((component, msg, data) => ComposerLogAdapter.Info("Tracer", component, msg, data));
    ///               TracerLog.Configure.Debug((component, msg, data) => ComposerLogAdapter.Debug("Tracer", component, msg, data));
    ///               TracerLog.Configure.DebugLoggingEnabled(ComposerLogAdapter.IsDebugLoggingEnabled);
    ///           }
    ///   
    ///           public const bool IsDebugLoggingEnabled = true;
    ///           
    ///           public static void Error(string componentGroupName, string componentName, string message, Exception exception, params object[] dataNamesAndValues)
    ///           {
    ///               // Prepare a log line in any appropriate way. For example:
    ///               StringBuilder logLine = ProfilerLog.DefaultFormat.ConstructLogLine(
    ///                                               ProfilerLog.DefaultFormat.LogLevelMoniker_Error,
    ///                                               componentGroupName,
    ///                                               "::",
    ///                                               componentName,
    ///                                               useUtcTimestamp: false,
    ///                                               Log.DefaultFormat.ConstructErrorMessage(message, exception),
    ///                                               dataNamesAndValues);
    ///               // Persist logLine to file...
    ///           }
    ///   
    ///           public static void Info(string componentGroupName, string componentName, string message, params object[] dataNamesAndValues)
    ///           {
    ///               // Prepare a log line (e.g. like above) and persist it to file...
    ///           }
    ///
    ///           public static void Debug(string componentGroupName, string componentName, string message, params object[] dataNamesAndValues)
    ///           {
    ///               // Prepare a log line (e.g. like above) and persist it to file...
    ///           }
    ///       }
    ///   }
    /// </code>
    /// </summary>
    public static class Log
    {
        private static class DefaultHandlers
        {
            public const bool IsDebugLoggingEnabled = true;

            public static void Error(string componentName, string message, Exception exception, params object[] dataNamesAndValues)
            {
                string errorMessage = Log.DefaultFormat.ConstructErrorMessage(message, exception);

                Console.WriteLine();
                Console.WriteLine(Log.DefaultFormat.ConstructLogLine(Log.DefaultFormat.LogLevelMoniker_Error, componentName, useUtcTimestamp: false, errorMessage, dataNamesAndValues)
                                                   .ToString());
            }

            public static void Info(string componentName, string message, params object[] dataNamesAndValues)
            {
                Console.WriteLine();
                Console.WriteLine(Log.DefaultFormat.ConstructLogLine(Log.DefaultFormat.LogLevelMoniker_Info, componentName, useUtcTimestamp: false, message, dataNamesAndValues)
                                                   .ToString());
            }

            public static void Debug(string componentName, string message, params object[] dataNamesAndValues)
            {
                Console.WriteLine();
                Console.WriteLine(Log.DefaultFormat.ConstructLogLine(Log.DefaultFormat.LogLevelMoniker_Debug, componentName, useUtcTimestamp: false, message, dataNamesAndValues)
                                                   .ToString());
            }

        }  // class DefaultHandlers

        internal static class DefaultFormat
        {
            public const string TimestampPattern_Local = @"yyyy-MM-dd, HH\:mm\:ss\.fff \(zzz\)";
            public const string TimestampPattern_Utc = @"yyyy-MM-dd, HH\:mm\:ss\.fff";

            public const string LogLevelMoniker_Error = "ERROR";
            public const string LogLevelMoniker_Info = "INFO ";
            public const string LogLevelMoniker_Debug = "DEBUG";

            private const string NullWord = "null";
            private const string DataValueNotSpecifiedWord = "unspecified";

            private static readonly string s_procIdInfo = GetProcIdInfoString();

            public static string ConstructErrorMessage(string message, Exception exception)
            {
                if (message != null && exception != null)
                {
                    if (message.Length > 0 && message[message.Length - 1] == '.')
                    {
                        return message + " " + exception.ToString();
                    }
                    else
                    {
                        return message + ". " + exception.ToString();
                    }
                }
                else if (message != null && exception == null)
                {
                    return message;
                }
                else if (message == null && exception != null)
                {
                    return exception.ToString();
                }
                else
                {
                    return null;
                }
            }

            public static StringBuilder ConstructLogLine(string logLevelMoniker, string componentName, bool useUtcTimestamp, string message, params object[] dataNamesAndValues)
            {
                return ConstructLogLine(logLevelMoniker, componentName, null, null, useUtcTimestamp, message, dataNamesAndValues);
            }

            public static StringBuilder ConstructLogLine(string logLevelMoniker, 
                                                         string componentNamePart1, 
                                                         string componentNamePart2, 
                                                         string componentNamePart3,
                                                         bool useUtcTimestamp,
                                                         string message, 
                                                         params object[] dataNamesAndValues)
            {
                var logLine = new StringBuilder(capacity: 128);
                AppendLogLinePrefix(logLine, logLevelMoniker, useUtcTimestamp);
                AppendEventInfo(logLine, componentNamePart1, componentNamePart2, componentNamePart3, message, dataNamesAndValues);

                return logLine;
            }

            public static void AppendLogLinePrefix(StringBuilder targetBuffer, string logLevelMoniker, bool useUtcTimestamp)
            {
                targetBuffer.Append("[");
                AppendLogLinePrefixCore(targetBuffer, logLevelMoniker, useUtcTimestamp);
                targetBuffer.Append("] ");
            }

            public static void AppendLogLinePrefixCore(StringBuilder targetBuffer, string logLevelMoniker, bool useUtcTimestamp)
            {
                if (targetBuffer == null)
                {
                    return;
                }

                if (useUtcTimestamp)
                {
                    targetBuffer.Append(DateTimeOffset.UtcNow.ToString(TimestampPattern_Utc));
                    targetBuffer.Append(" UTC");
                }
                else
                {
                    targetBuffer.Append(DateTimeOffset.Now.ToString(TimestampPattern_Local));
                }

                if (logLevelMoniker != null)
                {
                    targetBuffer.Append(" | ");
                    targetBuffer.Append(logLevelMoniker);
                }

                if (s_procIdInfo != null)
                {
                    targetBuffer.Append(s_procIdInfo);
                }
            }

            public static void AppendEventInfo(StringBuilder targetBuffer, 
                                               string componentNamePart1,
                                               string componentNamePart2,
                                               string componentNamePart3, 
                                               string message, 
                                               params object[] dataNamesAndValues)
            {
                bool hasComponentName = false;

                if (! String.IsNullOrWhiteSpace(componentNamePart1))
                {
                    targetBuffer.Append(componentNamePart1);
                    hasComponentName = true;
                }

                if (! String.IsNullOrWhiteSpace(componentNamePart2))
                {
                    targetBuffer.Append(componentNamePart2);
                    hasComponentName = true;
                }

                if (! String.IsNullOrWhiteSpace(componentNamePart3))
                {
                    targetBuffer.Append(componentNamePart3);
                    hasComponentName = true;
                }

                if (hasComponentName)
                {
                    targetBuffer.Append(": ");
                }

                if (!String.IsNullOrWhiteSpace(message))
                {
                    targetBuffer.Append(message);

                    if (message.Length > 0 && message[message.Length - 1] == '.')
                    {
                        targetBuffer.Append(' ');
                    }
                    else
                    {
                        targetBuffer.Append(". ");
                    }
                }

                if (dataNamesAndValues != null && dataNamesAndValues.Length > 0)
                {
                    targetBuffer.Append("{");
                    for (int i = 0; i < dataNamesAndValues.Length; i += 2)
                    {
                        if (i > 0)
                        {
                            targetBuffer.Append(", ");
                        }

                        targetBuffer.Append('[');
                        QuoteIfString(targetBuffer, dataNamesAndValues[i]);
                        targetBuffer.Append(']');
                        targetBuffer.Append('=');

                        if (i + 1 < dataNamesAndValues.Length)
                        {
                            QuoteIfString(targetBuffer, dataNamesAndValues[i + 1]);
                        }
                        else
                        {
                            targetBuffer.Append(DataValueNotSpecifiedWord);
                        }
                    }

                    targetBuffer.Append("}");
                }
            }

            private static string GetProcIdInfoString()
            {
                const int MinPidWidth = 6;
                const int MaxPidWidth = 10;
                const string PIdPrefix = " | PId:";

                int maxInfoStringLen = MaxPidWidth + PIdPrefix.Length;

                try
                {
                    var pidStr = new StringBuilder(capacity: maxInfoStringLen + 1);

                    pidStr.Append(Process.GetCurrentProcess().Id);
                    while (pidStr.Length < MinPidWidth)
                    {
                        pidStr.Insert(0, ' ');
                    }

                    pidStr.Insert(0, PIdPrefix);

                    return pidStr.ToString();
                }
                catch
                {
                    return null;
                }
            }

            private static void QuoteIfString<T>(StringBuilder targetBuffer, T val)
            {
                if (val == null)
                {
                    targetBuffer.Append(NullWord);
                }
                else
                {
                    if (val is string strValue)
                    {
                        targetBuffer.Append('"');
                        targetBuffer.Append(strValue);
                        targetBuffer.Append('"');
                    }
                    else
                    {
                        targetBuffer.Append(val.ToString());
                    }
                }
            }
        }  // class DefaultFormat

        /// <summary>
        /// Use statements like <c>Log.Configure.Info(YourHandler)</c> to redirect logging to your destination.
        /// </summary>
        public static class Configure
        {
            /// <summary>
            /// Sets the handler delegate for processing Error log events.
            /// If <c>null</c> is specified, then Error log events will be ignored.
            /// </summary>
            public static void Error(Action<string, string, Exception, object[]> logEventHandler)
            {
                s_errorLogEventHandler = logEventHandler;
            }

            /// <summary>
            /// Sets the handler delegate for processing Info log events.
            /// If <c>null</c> is specified, then Error log events will be ignored.
            /// </summary>
            public static void Info(Action<string, string, object[]> logEventHandler)
            {
                s_infoLogEventHandler = logEventHandler;
            }

            /// <summary>
            /// Sets the handler delegate for processing Debug log events.
            /// If <c>null</c> is specified, then Error log events will be ignored.
            /// </summary>
            public static void Debug(Action<string, string, object[]> logEventHandler)
            {
                s_debugLogEventHandler = logEventHandler;
            }

            /// <summary>
            /// Sets whether Debug log events should be processed or ignored.
            /// </summary>
            public static void DebugLoggingEnabled(bool isDebugLoggingEnabled)
            {
                s_isDebugLoggingEnabled = isDebugLoggingEnabled;
            }
        }

        private static Action<string, string, Exception, object[]> s_errorLogEventHandler = DefaultHandlers.Error;
        private static Action<string, string, object[]> s_infoLogEventHandler = DefaultHandlers.Info;
        private static Action<string, string, object[]> s_debugLogEventHandler = DefaultHandlers.Debug;
        private static bool s_isDebugLoggingEnabled = DefaultHandlers.IsDebugLoggingEnabled;

        /// <summary>
        /// Gets whether debug log messages should be processed or ignored.
        /// Consider wrapping debug message invocations into IF statements that check for this
        /// value in order to avoid unnecessarily constructing debug message strings.
        /// </summary>
        public static bool IsDebugLoggingEnabled
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return s_isDebugLoggingEnabled; }
        }

        /// <summary>
        /// Logs an error.
        /// These need to be persisted well, so that the info is available for support cases.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Error(string componentName, string message, params object[] dataNamesAndValues)
        {
            Error(componentName, message, exception: null, dataNamesAndValues);
        }

        /// <summary>
        /// Logs an error.
        /// These need to be persisted well, so that the info is available for support cases.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Error(string componentName, Exception exception, params object[] dataNamesAndValues)
        {
            Error(componentName, message: null, exception, dataNamesAndValues);
        }

        /// <summary>
        /// Logs an error.
        /// These need to be persisted well, so that the info is available for support cases.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Error(string componentName, string message, Exception exception, params object[] dataNamesAndValues)
        {
            Action<string, string, Exception, object[]> logEventHandler = s_errorLogEventHandler;
            if (logEventHandler != null)
            {
                logEventHandler(componentName, message, exception, dataNamesAndValues);
            }
        }

        /// <summary>
        /// Logs an important info message.
        /// These need to be persisted well, so that the info is available for support cases.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Info(string componentName, string message, params object[] dataNamesAndValues)
        {
            Action<string, string, object[]> logEventHandler = s_infoLogEventHandler;
            if (logEventHandler != null)
            {
                logEventHandler(componentName, message, dataNamesAndValues);
            }
        }

        /// <summary>
        /// Logs a non-critical info message. Mainly used for for debugging during prototyping.
        /// These messages can likely be dropped in production.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Debug(string componentName, string message, params object[] dataNamesAndValues)
        {
            if (IsDebugLoggingEnabled)
            { 
                Action<string, string, object[]> logEventHandler = s_debugLogEventHandler;
                if (logEventHandler != null)
                {
                    logEventHandler(componentName, message, dataNamesAndValues);
                }
            }
        }
    }  // class Log
