using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Threading;

// ReSharper disable InconsistentNaming
namespace TSLab.Script.Handlers
{
    // ReSharper disable once ConvertToStaticClass
    internal sealed class Logging
    {
        private static volatile bool s_LoggingEnabled = true;
        private static volatile bool s_LoggingInitialized;
        private static volatile bool s_AppDomainShutdown;

        private const string TraceSourceHandlersName = "TSLab.Script.Handlers";
        private static TraceSource s_DataCommonTraceSource;

        private Logging()
        {
        }

        private static object s_InternalSyncObject;

        private static object InternalSyncObject
        {
            get
            {
                if (s_InternalSyncObject == null)
                {
                    object o = new Object();
                    Interlocked.CompareExchange(ref s_InternalSyncObject, o, null);
                }
                return s_InternalSyncObject;
            }
        }

        internal static bool On
        {
            get
            {
                if (!s_LoggingInitialized)
                {
                    InitializeLogging();
                }
                return s_LoggingEnabled;
            }
        }

        internal static TraceSource Handlers
        {
            get
            {
                if (!s_LoggingInitialized)
                {
                    InitializeLogging();
                }
                if (!s_LoggingEnabled)
                {
                    return null;
                }
                return s_DataCommonTraceSource;
            }
        }

        private static void InitializeLogging()
        {
            lock (InternalSyncObject)
            {
                if (!s_LoggingInitialized)
                {
                    bool loggingEnabled;
                    s_DataCommonTraceSource = new TraceSource(TraceSourceHandlersName);

                    try
                    {
                        loggingEnabled = s_DataCommonTraceSource.Switch.ShouldTrace(TraceEventType.Critical);
                    }
                    catch (SecurityException)
                    {
                        // These may throw if the caller does not have permission to hook up trace listeners.
                        // We treat this case as though logging were disabled.
                        Close();
                        loggingEnabled = false;
                    }
                    if (loggingEnabled)
                    {
                        AppDomain currentDomain = AppDomain.CurrentDomain;
                        currentDomain.UnhandledException += UnhandledExceptionHandler;
                        currentDomain.DomainUnload += AppDomainUnloadEvent;
                        currentDomain.ProcessExit += ProcessExitEvent;
                    }
                    s_LoggingEnabled = loggingEnabled;
                    s_LoggingInitialized = true;
                }
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Logging functions must work in partial trust mode")]
        private static void Close()
        {
            s_DataCommonTraceSource?.Close();
        }
        /// <devdoc>
        ///    <para>Logs any unhandled exception through this event handler</para>
        /// </devdoc>
        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Exception(Handlers, "UnhandledExceptionHandler", e);
        }

        private static void ProcessExitEvent(object sender, EventArgs e)
        {
            Close();
            s_AppDomainShutdown = true;
        }
        /// <devdoc>
        ///    <para>Called when the system is shutting down, used to prevent additional logging post-shutdown</para>
        /// </devdoc>
        private static void AppDomainUnloadEvent(object sender, EventArgs e)
        {
            Close();
            s_AppDomainShutdown = true;
        }

        /// <devdoc>
        ///    <para>Confirms logging is enabled, given current logging settings</para>
        /// </devdoc>
        private static bool ValidateSettings(TraceSource traceSource, TraceEventType traceLevel)
        {
            if (!s_LoggingEnabled)
            {
                return false;
            }
            if (!s_LoggingInitialized)
            {
                InitializeLogging();
            }
            if (traceSource == null || !traceSource.Switch.ShouldTrace(traceLevel))
            {
                return false;
            }
            if (s_AppDomainShutdown)
            {
                return false;
            }
            return true;
        }

        internal static void PrintLine(TraceSource traceSource, TraceEventType eventType, int id, string msg)
        {
            traceSource.TraceEvent(eventType, id, msg);
        }

        internal static void Exception(TraceSource traceSource, string method, Exception e)
        {
            if (!ValidateSettings(traceSource, TraceEventType.Error))
            {
                return;
            }

            string infoLine = $"Exception in {method}:{e.Message}";
            if (!string.IsNullOrWhiteSpace(e.StackTrace))
            {
                infoLine += "\r\n" + e.StackTrace;
            }
            PrintLine(traceSource, TraceEventType.Error, 0, infoLine);
        }

        internal static void PrintInfo(TraceSource traceSource, string objectName, string msg)
        {
            if (!ValidateSettings(traceSource, TraceEventType.Warning))
            {
                return;
            }

            var msg1 = $"[{objectName}] {msg}";
            PrintLine(traceSource, TraceEventType.Warning, 0, msg1);
        }

        internal static void PrintWarning(TraceSource traceSource, string msg)
        {
            if (!ValidateSettings(traceSource, TraceEventType.Warning))
            {
                return;
            }
            PrintLine(traceSource, TraceEventType.Warning, 0, msg);
        }

        internal static void PrintError(TraceSource traceSource, string msg)
        {
            if (!ValidateSettings(traceSource, TraceEventType.Error))
            {
                return;
            }
            PrintLine(traceSource, TraceEventType.Error, 0, msg);
        }
    }
}
