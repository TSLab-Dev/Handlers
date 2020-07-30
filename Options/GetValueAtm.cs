using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using TSLab.DataSource;
using TSLab.Script.CanvasPane;
using TSLab.Script.Optimization;
using TSLab.Utils;

namespace TSLab.Script.Handlers.Options
{
    /// <summary>
    /// \~english Numerical estimate value at-the-money (only one point is processed using provided profile)
    /// \~russian Численный расчет значения точки на-деньгах  (вычисляется одна точка из профиля)
    /// </summary>
    [HandlerCategory(HandlerCategories.OptionsIndicators)]
    [HelperName("Get Value ATM (IntSer)", Language = Constants.En)]
    [HelperName("Значение на деньгах (IntSer)", Language = Constants.Ru)]
    [HandlerAlwaysKeep]
    [InputsCount(2)]
    [Input(0, TemplateTypes.INTERACTIVESPLINE, Name = "Profile")]
    [Input(1, TemplateTypes.DOUBLE, Name = "Moneyness")]
    [OutputType(TemplateTypes.DOUBLE)]
    [Description("Численный расчет значения точки на-деньгах  (вычисляется одна точка из профиля)")]
    [HelperDescription("Numerical estimate of value at-the-money (only one point of profile is returned)", Constants.En)]
    public class GetValueAtm : BaseContextHandler, IValuesHandlerWithNumber
    {
        ///// <summary>"GETVAL"</summary>
        //private const string MsgId = "GETVAL";

        private bool m_repeatLastValue;
        private OptimProperty m_result = new OptimProperty(0, false, double.MinValue, double.MaxValue, 1.0, 3);

        /// <summary>
        /// Локальное кеширующее поле
        /// </summary>
        private double m_prevValue = Double.NaN;

        #region Parameters
        /// <summary>
        /// \~english Handler should repeat last known value to avoid further logic errors
        /// \~russian При true будет находить и использовать последнее известное значение
        /// </summary>
        [HelperName("Repeat Last Value", Constants.En)]
        [HelperName("Повтор значения", Constants.Ru)]
        [Description("При true будет находить и использовать последнее известное значение")]
        [HelperDescription("Handler should repeat last known value to avoid further logic errors", Language = Constants.En)]
        [HandlerParameter(true, NotOptimized = false, IsVisibleInBlock = true, Default = "false", Name = "Repeat Last Value")]
        public bool RepeatLastValue
        {
            get { return m_repeatLastValue; }
            set { m_repeatLastValue = value; }
        }

        /// <summary>
        /// \~english Moneyness
        /// \~russian Денежность
        /// </summary>
        [ReadOnly(true)]
        [HelperName("Moneyness", Constants.En)]
        [HelperName("Денежность", Constants.Ru)]
        [Description("Денежность")]
        [HelperDescription("Moneyness", Language = Constants.En)]
        [HandlerParameter(true, NotOptimized = false, IsVisibleInBlock = true,
            Default = "0", Min = "-10000000", Max = "10000000", Step = "1")]
        public double Moneyness { get; set; }

        /// <summary>
        /// \~english Value ATM
        /// \~russian Значение на-деньгах
        /// </summary>
        [ReadOnly(true)]        
        [HelperName("Result", Constants.En)]
        [HelperName("Результат", Constants.Ru)]
        [Description("Значение на-деньгах")]
        [HelperDescription("Value ATM", Language = Constants.En)]
        [HandlerParameter(true, NotOptimized = false, IsVisibleInBlock = true)]
        public OptimProperty Result
        {
            get { return m_result; }
            set { m_result = value; }
        }

        /// <summary>
        /// \~english Print in main log
        /// \~russian Выводить в главный лог приложения
        /// </summary>
        [ReadOnly(true)]
        [HelperName("Print in Log", Constants.En)]
        [HelperName("Выводить в лог", Constants.Ru)]
        [Description("Выводить в главный лог приложения")]
        [HelperDescription("Print in main log", Language = Constants.En)]
        [HandlerParameter(true, NotOptimized = false, IsVisibleInBlock = true, Default = "False")]
        public bool PrintInLog { get; set; }
        #endregion Parameters

        public double Execute(InteractiveSeries profile, int barNum)
        {
            double res = Process(profile, Moneyness, barNum);
            return res;
        }

        public double Execute(InteractiveSeries profile, double moneyness, int barNum)
        {
            double res = Process(profile, moneyness, barNum);
            return res;
        }

        private double Process(InteractiveSeries profile, double moneyness, int barNum)
        {
            // В данном случае намеренно возвращаю Double.NaN
            double failRes = Double.NaN;
            if (m_repeatLastValue)
                failRes = Double.IsNaN(m_prevValue) ? Double.NaN : m_prevValue; // В данном случае намеренно возвращаю Double.NaN

            Dictionary<DateTime, double> results;
            #region Get cache
            // [2019-01-30] Перевожу на использование NotClearableContainer (PROD-6683)
            string key = m_variableId + "_results";
            var container = m_context.LoadObject(key) as NotClearableContainer<Dictionary<DateTime, double>>;
            if (container != null)
                results = container.Content;
            else
                results = m_context.LoadObject(key) as Dictionary<DateTime, double>; // Старая ветка на всякий случай

            if (results == null)
            {
                string msg = String.Format(RM.GetString("OptHandlerMsg.GetValueAtm.CacheNotFound"),
                    GetType().Name, key.GetHashCode());
                m_context.Log(msg, MessageType.Info);

                results = new Dictionary<DateTime, double>();
                container = new NotClearableContainer<Dictionary<DateTime, double>>(results);
                m_context.StoreObject(key, container);
            }
            #endregion Get cache

            int len = m_context.BarsCount;
            if (len <= 0)
                return failRes;

            // Вот так не работает. По всей видимости, это прямая индексация от утра
            //DateTime now = m_context.Runtime.GetBarTime(barNum);

            ISecurity sec = m_context.Runtime.Securities.FirstOrDefault();
            if ((sec == null) || (sec.Bars.Count <= barNum))
                return failRes;

            DateTime now = sec.Bars[barNum].Date;
            if (results.TryGetValue(now, out double rawRes) &&
                (barNum < len - 1)) // !!! ВАЖНО !!! На последнем баре ВСЕГДА заново делаем вычисления
            {
                m_prevValue = rawRes;
                return rawRes;
            }
            else
            {
                int barsCount = ContextBarsCount;
                if (barNum < barsCount - 1)
                {
                    // Если история содержит осмысленное значение, то оно уже содержится в failRes
                    return failRes;
                }
                else
                {
                    #region Process last bar(s)
                    if (profile == null)
                        return failRes;

                    SmileInfo profInfo = profile.GetTag<SmileInfo>();
                    if ((profInfo == null) || (profInfo.ContinuousFunction == null))
                        return failRes;

                    double f = profInfo.F;
                    double dT = profInfo.dT;
                    if (!DoubleUtil.IsPositive(f))
                    {
                        string msg = String.Format(RM.GetString("OptHandlerMsg.FutPxMustBePositive"), GetType().Name, f);
                        m_context.Log(msg, MessageType.Error);
                        // [GLSP-1557] В данном случае намеренно возвращаю Double.NaN, чтобы предотвратить распространение
                        //             заведомо неправильных данных по системе (их попадание в дельта-хеджер и т.п.).
                        return Double.NaN;
                    }

                    if (!DoubleUtil.IsPositive(dT))
                    {
                        string msg = String.Format(RM.GetString("OptHandlerMsg.TimeMustBePositive"), GetType().Name, dT);
                        m_context.Log(msg, MessageType.Error);
                        // [GLSP-1557] В данном случае намеренно возвращаю Double.NaN, чтобы предотвратить распространение
                        //             заведомо неправильных данных по системе (их попадание в дельта-хеджер и т.п.).
                        return Double.NaN;
                    }

                    double effectiveF;
                    if (DoubleUtil.IsZero(moneyness))
                        effectiveF = f;
                    else
                        effectiveF = f * Math.Exp(moneyness * Math.Sqrt(dT));
                    if (profInfo.ContinuousFunction.TryGetValue(effectiveF, out rawRes))
                    {
                        m_prevValue = rawRes;
                        results[now] = rawRes;
                    }
                    else
                    {
                        if (barNum < len - 1)
                        {
                            // [GLSP-1557] Не последний бар? Тогда использую failRes
                            rawRes = failRes;
                        }
                        else
                        {
                            // [GLSP-1557] В данном случае намеренно возвращаю Double.NaN, чтобы предотвратить распространение
                            //             заведомо неправильных данных по системе (их попадание в дельта-хеджер и т.п.).
                            return Double.NaN;
                        }
                    }
                    #endregion Process last bar(s)

                    m_result.Value = rawRes;
                    //m_context.Log(MsgId + ": " + m_result.Value, MessageType.Info, PrintInLog);

                    return rawRes;
                }
            }
        }
    }
}
