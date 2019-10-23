using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TSLab.Utils.Profiling;

namespace TSLab.Script.Handlers
{
    internal sealed class HeartbeatManager
    {
        private sealed class Context
        {
            public IRuntime Runtime { get; set; }
            public string VariableId { get; set; }
            public string VariableName { get; set; }
            public TimeSpan Interval { get; set; }
            public DateTime CheckTime { get; set; }
            public bool IsBusy { get; set; }
        }

        public static readonly HeartbeatManager Instance = new HeartbeatManager();
        private static readonly Common.Logging.ILog s_log = Common.Logging.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly IDictionary<string, Context> m_contextsMap = new Dictionary<string, Context>();

        private HeartbeatManager()
        {
            var thread = ThreadProfiler.Create(ThreadStart);
            thread.IsBackground = true;
            thread.Name = nameof(HeartbeatManager) + "." + nameof(Thread);
            thread.Start();
        }

        public void Register(HeartbeatHandler handler)
        {
            var runtime = handler.Context.Runtime;
            var key = string.Join(".", runtime.TradeName, runtime.IsAgentMode, handler.VariableId);

            lock (m_contextsMap)
                if (m_contextsMap.TryGetValue(key, out var oldContext))
                {
                    oldContext.Runtime = runtime;
                    oldContext.VariableId = handler.VariableId;
                    oldContext.VariableName = handler.VariableName;
                    oldContext.Interval = handler.Interval;
                }
                else
                    m_contextsMap[key] = new Context
                    {
                        Runtime = runtime,
                        VariableId = handler.VariableId,
                        VariableName = handler.VariableName,
                        Interval = handler.Interval,
                    };
        }

        private void ThreadStart()
        {
            while (true)
            {
                Thread.Sleep(1);
                var utcNow = DateTime.UtcNow;

                lock (m_contextsMap)
                    foreach (var keyValuePair in m_contextsMap.ToArray())
                    {
                        var context = keyValuePair.Value;
                        var runtime = context.Runtime;

                        if (runtime.IsDisposedOrDisposing || (runtime.IsAgentMode && !runtime.IsRealTime))
                            m_contextsMap.Remove(keyValuePair.Key);
                        else if (!context.IsBusy && utcNow - context.CheckTime >= context.Interval)
                        {
                            if (!runtime.IsAgentMode && !runtime.IsRealTime)
                                m_contextsMap.Remove(keyValuePair.Key);
                            else
                            {
                                context.CheckTime = utcNow;
                                context.IsBusy = true;
                                Task.Factory.StartNewEx(() => Execute(context));
                            }
                        }
                    }
            }
        }

        private static void Execute(Context context)
        {
            try
            {
                var runtime = context.Runtime;
                if (runtime.IsDisposedOrDisposing)
                    return;

                runtime.Recalc(RecalcReasons.Heartbeat2, null);
            }
            catch (Exception ex)
            {
                s_log.Error(ex);
            }
            finally
            {
                context.IsBusy = false;
            }
        }
    }
}
