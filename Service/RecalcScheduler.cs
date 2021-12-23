using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Threading;
using TSLab.DataSource;
using TSLab.Script.Handlers.Options;
using TSLab.Script.Options;
using TSLab.Script.Realtime;
using TSLab.Utils.Profiling;

namespace TSLab.Script.Handlers.Service
{
    /// <summary>
    /// \~english Recalculate script by scheduler
    /// \~russian Автоматический принудительный пересчет скрипта в заданное время
    /// </summary>
    [HandlerCategory(HandlerCategories.ServiceElements)]
    [HandlerAlwaysKeep]
    [InputsCount(1)]
    [Input(0, TemplateTypes.SECURITY | TemplateTypes.OPTION | TemplateTypes.OPTION_SERIES)]
    [OutputType(TemplateTypes.DOUBLE)] // Номер вызова (может достигать ОГРОМНЫХ значений)
    public sealed class RecalcScheduler : BaseContextHandler, IStreamHandler
    {
        private readonly long m_id;
        private static long s_counter;
        /// <summary>Счетчики проблемных ситуаций, возникших с таймером [ PROD-5427 ]</summary>
        private static readonly ConcurrentDictionary<string, int> s_problemCounters = new ConcurrentDictionary<string, int>();

        public RecalcScheduler()
        {
            m_id = Interlocked.Increment(ref s_counter) - 1;
        }

        #region Parameters

        /// <summary>
        /// \~english Delay between calls (ms)
        /// \~russian Задержка между вызовами (мс)
        /// </summary>
        [Description("Время вызова пересчета")]
        [HelperDescription("Recalc time", Language = Constants.En)]
        [HandlerParameter(true, NotOptimized = false, IsVisibleInBlock = true, Editor = "TimeSpanEditor", Default = "9:59:58")]
        public TimeSpan RecalcTime { get; set; }

        #endregion Parameters

        public IList<double> Execute(IOption opt)
        {
            IList<double> res = Execute(opt.UnderlyingAsset);
            return res;
        }

        public IList<double> Execute(IOptionSeries optSer)
        {
            IList<double> res = Execute(optSer.UnderlyingAsset);
            return res;
        }

        public IList<double> Execute(ISecurity sec)
        {
            //Context.Log(String.Format("[Heartbeat.Execute ( ID:{0} )] I'm checking timer settings.", m_id), MessageType.Warning, false);

            // PROD-5496 - В режиме оптимизации отключаюсь
            if (Context.IsOptimization)
                return Constants.EmptyListDouble;
            var secrt = sec as ISecurityRt;
            if (secrt?.IsPortfolioActive != true)
                return Constants.EmptyListDouble;

            TimerInfo timerState;
            string cashKey = VariableId + "_timerState";
            {
                object localObj = Context.LoadObject(cashKey);
                timerState = localObj as TimerInfo;
                // PROD-3970 - 'Важный' объект
                if (timerState == null)
                {
                    if (localObj is NotClearableContainer container && container.Content != null)
                        timerState = container.Content as TimerInfo;
                }
            }

            if (timerState == null)
            {
                string msg = $"[RecalcScheduler({m_id})] Preparing new timer for agent '{Context.Runtime.TradeName}'...";
                Context.Log(msg);

                timerState = TimerInfo.Create(Context, secrt, RecalcTime);

                var container = new NotClearableContainer(timerState);
                Context.StoreObject(cashKey, container);
            }
            else if (timerState.Timer == null)
            {
                // PROD-5427 - Добавляю счетчик этого аварийного события и логгирую
                int problemCounter = 0;
                if (s_problemCounters.ContainsKey(Context.Runtime.TradeName))
                    problemCounter = s_problemCounters[Context.Runtime.TradeName];

                s_problemCounters[Context.Runtime.TradeName] = Interlocked.Increment(ref problemCounter);

                string msg =
                    $"[RecalcScheduler({m_id})] Timer is null in agent '{Context.Runtime.TradeName}'. Problem counter: {problemCounter}";
                Context.Log(msg, MessageType.Warning);

                if (problemCounter > 3)
                {
                    // Если проблема систематически повторяется -- выбрасываю ассерт для дальнейшего анализа ситуации
                    Contract.Assert(timerState.Timer != null, msg);
                }
            }
            else
            {
                // Если при изменении скрипта пересоздается агент, то контекст становится невалидным?
                if (!ReferenceEquals(Context, timerState.CallContext))
                {
                    // Если по какой-то причине изменился контекст, то создаём новый таймер...
                    string msg = $"[RecalcScheduler({m_id})] Replacing timer for agent '{Context.Runtime.TradeName}'...";
                    Context.Log(msg, MessageType.Warning);

                    // Создаём новый таймер. При этом используем НОВЫЙ m_id
                    timerState = TimerInfo.Create(Context, secrt, RecalcTime);
                    var container = new NotClearableContainer(timerState);
                    Context.StoreObject(cashKey, container);
                }
                else if (timerState.RecalcTime != RecalcTime) // RecalcTime is modified (by a control pane for example)
                {
                    timerState.ChangeTime(RecalcTime);
                }
            }

            int len = Context.BarsCount;
            double[] res = Context.GetArray<double>(len);
            if (len > 0)
                res[len - 1] = m_id;

            return res;
        }

        /// <summary>
        /// Метод должен принять на вход объект класса CallState и через его контекст сделать пересчет агента.
        /// </summary>
        /// <param name="state">объект класса CallState</param>
        private static void Recalculate(object state)
        {
            if (state is TimerInfo callState)
            {
                IContext context = callState.CallContext;
                //context?.Log(String.Format("[Heartbeat.Recalculate ( ID:{0} )] I'll call Recalc now...", callState.CallId), MessageType.Warning, false);

                // PROD-5496 - В режиме оптимизации отключаюсь
                if (context.IsOptimization)
                    return;
                if (callState.Security.IsPortfolioActive != true)
                {
                    context.Log("RecalcScheduler: recalculation canceled due to connection inactivity");
                    return;
                }
                context.Log("RecalcScheduler: call recalculate");
                context.Recalc(RecalcReasons.RecalcScheduler, callState.Security.SecurityDescription);
            }
        }

        /// <summary>
        /// Класс хранит таймер, контекст вызова и идентификатор вызова на котором таймер БЫЛ СОЗДАН.
        /// </summary>
        private sealed class TimerInfo : IDisposable
        {
            /// <summary>
            /// Контекст вызова кубика (всегда заполняется и не может быть null)
            /// </summary>
            public IContext CallContext { get; }

            public ISecurityRt Security { get; }

            public TimeSpan RecalcTime { get; private set; }

            /// <summary>
            /// Заполнить таймер сразу через конструктор не получится, но это нужно обязательно сделать сразу после его инициализации.
            /// </summary>
            public IThreadingTimerProfiler Timer { get; private set; }

            public static TimerInfo Create(IContext context, ISecurityRt security, TimeSpan recalcTime)
            {
                var delayMs = GetInterval(recalcTime);
                var timerInfo = new TimerInfo(context, security, recalcTime);
                var timer = TimerFactory.CreateThreadingTimer(Recalculate, timerInfo, delayMs, new TimeSpan(24, 0, 0));
                timerInfo.Timer = timer;
                return timerInfo;
            }

            private TimerInfo(IContext context, ISecurityRt security, TimeSpan recalcTime)
            {
                Debug.Assert(context != null, "context==null");
                CallContext = context;
                Security = security;
                RecalcTime = recalcTime;
            }

            private static TimeSpan GetInterval(TimeSpan time)
            {
                var now = DateTime.Now.TimeOfDay;
                if (time < now)
                    time = time.Add(TimeSpan.FromDays(1));
                var ts = time - now;
                return ts;
            }

            public void ChangeTime(TimeSpan recalcTime)
            {
                Timer?.Dispose();
                RecalcTime = recalcTime;
                Timer = TimerFactory.CreateThreadingTimer(Recalculate, this, GetInterval(recalcTime), new TimeSpan(24, 0, 0));
            }

            public void Dispose()
            {
                Timer?.Dispose();
            }
        }
    }
}
