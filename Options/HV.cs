﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;

using TSLab.DataSource;
using TSLab.Diagnostics;
using TSLab.Script.Options;
using TSLab.Utils;

namespace TSLab.Script.Handlers.Options
{
    /// <summary>
    /// \~english Estimate HV with classic method
    /// \~russian Оценка исторической волатильности по формулам из учебника
    /// </summary>
    [HandlerCategory(HandlerCategories.OptionsIndicators)]
    [HelperName("HV (from book)", Language = Constants.En)]
    [HelperName("HV (из учебника)", Language = Constants.Ru)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.SECURITY | TemplateTypes.OPTION_SERIES | TemplateTypes.OPTION, Name = Constants.AnyOption)]
    [OutputType(TemplateTypes.DOUBLE)]
    [Description("Оценка исторической волатильности по формулам из учебника")]
    [HelperDescription("Estimate HV with classic method", Constants.En)]
    public class HV : BaseContextHandler, IStreamHandler
    {
        /// <summary>
        /// Старайся не трогать DefaultPeriodInt!
        /// 990 зашито повсюду и его изменение может привести к потере накопленной истории
        /// у всех клиентов.
        /// </summary>
        private const int DefaultPeriodInt = 990;
        /// <summary>
        /// Старайся не трогать DefaultMultDbl!
        /// 500 зашито повсюду и его изменение может привести к потере накопленной истории
        /// у всех клиентов.
        /// </summary>
        private const double DefaultMultDbl = 500;
        /// <summary>
        /// Старайся не трогать DefaultPeriodInt!
        /// 990 зашито повсюду и его изменение может привести к потере накопленной истории
        /// у всех клиентов.
        /// </summary>
        private const string DefaultPeriod = "990";
        private const string DefaultMult = "500";

        private int m_period = Int32.Parse(DefaultPeriod);
        private double m_annualizingMultiplier = Double.Parse(DefaultMult);

        /// <summary>
        /// Локальный кеш волатильностей
        /// </summary>
        private List<double> LocalHistory
        {
            get
            {
                // Локальный ключ можно всегда формировать вместе с периодом!
                string key = VariableId + "_historySigmas_" + m_period;
                List<double> hvSigmas = Context.LoadObject(key) as List<double>;
                if (hvSigmas == null)
                {
                    hvSigmas = new List<double>();
                    Context.StoreObject(key, hvSigmas);
                }

                return hvSigmas;
            }
        }

        #region Parameters
        /// <summary>
        /// \~english Should handler use all data including overnight gaps?
        /// \~russian При true будет учитывать все данные, включая ночные гепы
        /// </summary>
        [HelperName("Use All Data", Constants.En)]
        [HelperName("Все данные", Constants.Ru)]
        [Description("При true будет учитывать все данные, включая ночные гепы")]
        [HelperDescription("Should handler use all data including overnight gaps?", Language = Constants.En)]
        [HandlerParameter(true, NotOptimized = false, IsVisibleInBlock = true, Default = "false", Name = "Use All Data")]
        public bool UseAllData { get; set; }

        /// <summary>
        /// \~english Calculation period
        /// \~russian Период расчета исторической волатильности
        /// </summary>
        [HelperName("Period", Constants.En)]
        [HelperName("Период", Constants.Ru)]
        [Description("Период расчета исторической волатильности")]
        [HelperDescription("Calculation period", Language = Constants.En)]
        [HandlerParameter(true, NotOptimized = false, IsVisibleInBlock = true,
            Default = DefaultPeriod, Min = "2", Max = "10000000", Step = "1", EditorMin = "2")]
        public int Period
        {
            get { return m_period; }
            set { m_period = Math.Max(2, value); }
        }

        /// <summary>
        /// \~english Multiplier to convert volatility to annualized value
        /// \~russian Множитель для перевода волатильности в годовое исчисление
        /// </summary>
        [HelperName("Annualizing Multiplier", Constants.En)]
        [HelperName("Масштабный множитель", Constants.Ru)]
        [Description("Множитель для перевода волатильности в годовое исчисление")]
        [HelperDescription("Multiplier to convert volatility to annualized value", Language = Constants.En)]
        [HandlerParameter(true, NotOptimized = false, IsVisibleInBlock = true,
            Default = DefaultMult, Min = "0", Max = "10000000", Step = "1", Name = "Annualizing Multiplier")]
        public double AnnualizingMultiplier
        {
            get { return m_annualizingMultiplier; }
            set
            {
                if (DoubleUtil.IsPositive(value))
                    m_annualizingMultiplier = value;
            }
        }

        /// <summary>
        /// \~english Repeat calculation for all bars every execution
        /// \~russian Повторять вычисление для всех баров при каждом вызове
        /// </summary>
        [HelperName("Repeat", Constants.En)]
        [HelperName("Повторить", Constants.Ru)]
        [Description("Повторять вычисление для всех баров при каждом вызове")]
        [HelperDescription("Repeat calculation for all bars every execution", Language = Constants.En)]
        [HandlerParameter(true, NotOptimized = false, IsVisibleInBlock = true, Default = "true")]
        public bool Reset { get; set; }

        /// <summary>
        /// \~english Use global cache
        /// \~russian Использовать глобальный кеш
        /// </summary>
        [HelperName("Use Global Cache", Constants.En)]
        [HelperName("Использовать глобальный кеш", Constants.Ru)]
        [Description("Использовать глобальный кеш")]
        [HelperDescription("Use global cache", Language = Constants.En)]
        [HandlerParameter(Name = "Use Global Cache", NotOptimized = false, IsVisibleInBlock = true, Default = "false")]
        public bool UseGlobalCache { get; set; }

        /// <summary>
        /// \~english Permission to write to Global Cache
        /// \~russian Разрешить чтение/запись в глобальный кеш или только чтение?
        /// </summary>
        [HelperName("Allow Global Write", Constants.En)]
        [HelperName("Разрешить запись", Constants.Ru)]
        [Description("Разрешить чтение/запись в глобальный кеш или только чтение?")]
        [HelperDescription("Permission to write to Global Cache", Language = Constants.En)]
        [HandlerParameter(Name = "Allow Global Write", NotOptimized = false, IsVisibleInBlock = true, Default = "false")]
        public bool AllowGlobalReadWrite { get; set; }

        /// <summary>
        /// \~english Period to write to Global Cache
        /// \~russian Сохранять в глобальный кеш через каждые N баров
        /// </summary>
        [HelperName("Global Save Period", Constants.En)]
        [HelperName("Периодичность записи", Constants.Ru)]
        [Description("Сохранять в глобальный кеш через каждые N баров")]
        [HelperDescription("Period to write to Global Cache", Language = Constants.En)]
        [HandlerParameter(Name = "Global Save Period", NotOptimized = false, IsVisibleInBlock = true,
            Default = "2", Min = "1", Max = "10000000", Step = "1")]
        public int GlobalSavePeriod { get; set; }
        #endregion Parameters

        /// <summary>
        /// Метод под флаг TemplateTypes.OPTION, чтобы подключаться к источнику-БА
        /// </summary>
        public IList<double> Execute(IOption opt)
        {
            return Execute(opt.UnderlyingAsset);
        }

        /// <summary>
        /// Метод под флаг TemplateTypes.OPTION_SERIES, чтобы подключаться к источнику-БА
        /// </summary>
        public IList<double> Execute(IOptionSeries optSer)
        {
            return Execute(optSer.UnderlyingAsset);
        }

        /// <summary>
        /// Метод под флаг TemplateTypes.SECURITY, чтобы подключаться к источнику-БА
        /// </summary>
        public IList<double> Execute(ISecurity sec)
        {
            if (sec == null)
                return Constants.EmptyListDouble;

            Dictionary<DateTime, double> hvSigmas;
            #region Get cache
            int barLengthInSeconds = (int)sec.IntervalInstance.ToSeconds();
            string cashKey = GetGlobalCashKey(sec.Symbol, false, UseAllData, barLengthInSeconds, m_annualizingMultiplier, m_period);
            if (UseGlobalCache)
            {
                object globalObj = Context.LoadGlobalObject(cashKey, true);
                hvSigmas = globalObj as Dictionary<DateTime, double>;
                // PROD-3970 - 'Важный' объект
                if (hvSigmas == null)
                {
                    var container = globalObj as NotClearableContainer;
                    if ((container != null) && (container.Content != null))
                        hvSigmas = container.Content as Dictionary<DateTime, double>;
                }
                if (hvSigmas == null)
                {
                    hvSigmas = new Dictionary<DateTime, double>();
                    var container = new NotClearableContainer(hvSigmas);
                    Context.StoreGlobalObject(cashKey, container, true);
                }
            }
            else
            {
                object locObj = Context.LoadObject(cashKey);
                hvSigmas = locObj as Dictionary<DateTime, double>;
                // PROD-3970 - 'Важный' объект
                if (hvSigmas == null)
                {
                    var container = locObj as NotClearableContainer;
                    if ((container != null) && (container.Content != null))
                        hvSigmas = container.Content as Dictionary<DateTime, double>;
                }
                if (hvSigmas == null)
                {
                    hvSigmas = new Dictionary<DateTime, double>();
                    var container = new NotClearableContainer(hvSigmas);
                    Context.StoreObject(cashKey, container);
                }
            }
            #endregion Get cache

            List<double> historySigmas = LocalHistory;

            if (Reset || m_context.Runtime.IsFixedBarsCount)
                historySigmas.Clear();

            int len = Context.BarsCount;
            // Локальный ключ можно всегда формировать вместе с периодом!
            string logsLocObjKey = VariableId + "_logs_" + m_period;
            object logsLocObj = m_context.LoadObject(logsLocObjKey);
            LinkedList<KeyValuePair<DateTime, double>> logs = logsLocObj as LinkedList<KeyValuePair<DateTime, double>>;
            #region Get cache
            // PROD-3970 - 'Важный' объект
            if (logs == null)
            {
                var container = logsLocObj as NotClearableContainer;
                if ((container != null) && (container.Content != null))
                    logs = container.Content as LinkedList<KeyValuePair<DateTime, double>>;
            }
            if (logs == null)
            {
                logs = new LinkedList<KeyValuePair<DateTime, double>>();
                var container = new NotClearableContainer(logs);
                m_context.StoreObject(logsLocObjKey, container);
            }
            #endregion Get cache

            if (Reset || m_context.Runtime.IsFixedBarsCount)
                logs.Clear();

            if (len <= 0)
                return historySigmas;

            // Типа, кеширование?
            for (int j = historySigmas.Count; j < len; j++)
            {
                IDataBar bar;
                try
                {
                    // Наблюдал на реале странную картину (ВО ВРЕМЯ КЛИРИНГА!):
                    // Context.BarsCount == 3843 && sec.Bars.Count == 3842
                    bar = sec.Bars[j];
                    // если бар технический (то есть имеет нулевой объём), пропускаем его
                    // 2017-04-26 - На эксанте при запросе истории все бары имеют нулевой объём, но при этом большинство из них являются нормальными:
                    //              имеют ненулевой размах и осмысленные значения открытия/закрытия
                    if ((bar.Volume <= Double.Epsilon) && (DoubleUtil.AreClose(bar.High, bar.Low)))
                    {
                        // пишу NaN в такой ситуации
                        historySigmas.Add(Constants.NaN);
                        continue;
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Перехватываю aorEx и пишу NaN в такой ситуации
                    historySigmas.Add(Constants.NaN);
                    continue;
                }

                DateTime t = bar.Date;
                double v = bar.Close;
                double ln = Math.Log(v);

                Check.Assert(logs != null, "Каким образом переменная logs оказалась null?");
                logs.AddLast(new KeyValuePair<DateTime, double>(t, ln));

                double hv;
                if (TryEstimateHv(
                    logs, m_period, barLengthInSeconds, m_annualizingMultiplier,
                    UseAllData, out hv))
                {
                    double vol = hv;
                    historySigmas.Add(vol);

                    lock (hvSigmas)
                    {
                        hvSigmas[t] = vol;
                    }
                }
                else
                    historySigmas.Add(Constants.NaN);
            }

            // Попытку записи в глобальный кеш делаю только в самом конце всех вычислений
            // для экономии времени на сериализацию и запись
            lock (hvSigmas)
            {
                if (sec.Bars.Count > len - 1)
                {
                    // Наблюдал на реале странную картину (ВО ВРЕМЯ КЛИРИНГА!):
                    // Context.BarsCount == 3760 && sec.Bars.Count == 3759
                    bool success = IvOnF.TryWrite(m_context, UseGlobalCache, AllowGlobalReadWrite, GlobalSavePeriod,
                        cashKey, hvSigmas, sec.Bars[len - 1].Date, historySigmas[historySigmas.Count - 1]);
                }

                return new ReadOnlyCollection<double>(historySigmas);
            }
        }

        /// <summary>
        /// Сформировать уникальный ключ для ГЛОБАЛЬНОГО КЕША
        /// </summary>
        public static string GetGlobalCashKey(string symbol, bool returnPct, bool useAllData,
            int barLengthInSeconds, double multiplier = DefaultMultDbl, int period = DefaultPeriodInt)
        {
            string cashKey;
            if (period == DefaultPeriodInt)
            {
                if (DoubleUtil.AreClose(multiplier, DefaultMultDbl))
                {
                    // Короткое имя позволит в дальнейшем менее болезненно проходить следующие изменения в расписании работы ФОРТС.
                    cashKey = String.Intern(typeof(HV).Name + "_hvSigmas_" + symbol + "_Pct-" + returnPct + "_All-" + useAllData + "_" +
                       barLengthInSeconds);
                }
                else
                {
                    cashKey = String.Intern(typeof(HV).Name + "_hvSigmas_" + symbol + "_Pct-" + returnPct + "_All-" + useAllData + "_" +
                       barLengthInSeconds + "_" +
                       multiplier.ToString("E", CultureInfo.InvariantCulture).Replace('+', '~'));
                }
            }
            else
            {
                // Очень аккуратно с изменением этого ключа! Если поменять формат, люди потеряют накопленный кеш истории.
                cashKey = String.Intern(typeof(HV).Name + "_hvSigmas_" + symbol + "_Pct-" + returnPct + "_All-" + useAllData + "_" +
                   barLengthInSeconds + "_" + period + "_" +
                   multiplier.ToString("E", CultureInfo.InvariantCulture).Replace('+', '~'));
            }
            return cashKey;
        }

        /// <summary>
        /// Выполнение оценки исторической волатильности
        /// (в безразмерных величинах в годовом исчислении).
        /// При необходимости выполняется проверка на точное соответствие
        /// ожидаемому расстоянию между точками данных.
        /// </summary>
        /// <param name="data">список точек (время; ЛОГАРИФМ ЦЕНЫ)</param>
        /// <param name="period">период расчета</param>
        /// <param name="barLengthInSeconds">ожидаемое расстояние между точками данных</param>
        /// <param name="annualizingMultiplier">множитель для перевода в годовое исчисление</param>
        /// <param name="useAllData">проверять ли точное расстояние между точками?</param>
        /// <param name="res">хватило ли данных для выполнения вычислений</param>
        /// <returns>безразмерная волатильность в годовом исчислении (НЕ ПРОЦЕНТЫ!)</returns>
        public static bool TryEstimateHv(LinkedList<KeyValuePair<DateTime, double>> data, int period,
            int barLengthInSeconds, double annualizingMultiplier, bool useAllData, out double res)
        {
            res = 0;
            if (data.Count <= period)
                return false;

            var last = data.Last;
            if (last == null)
                return false;

            uint counter = 0;
            double sum = 0, sum2 = 0;
            if (barLengthInSeconds > 0)
            {
                while (counter < period)
                {
                    var prev = last.Previous;
                    if (prev == null)
                        break;

                    TimeSpan ts = last.Value.Key - prev.Value.Key;
                    if (useAllData || ((int)ts.TotalSeconds == barLengthInSeconds))
                    {
                        double r = last.Value.Value - prev.Value.Value;
                        sum += r;
                        sum2 += r * r;

                        counter++;
                    }

                    last = prev;
                }

                // Чистимся?
                if (last.Previous != null)
                {
                    last = last.Previous;
                    var first = data.First;
                    while ((first != null) && (first != last))
                    {
                        first = first.Next;
                        data.RemoveFirst();
                        //pxs.RemoveFirst();
                    }
                }
            }
            else
            {
                throw new NotImplementedException("BarLengthInSeconds must be above zero.");
            }

            if (counter < period)
                return false;

            double dispersion = sum2 / counter - sum * sum / counter / counter;
            double sigma = (dispersion > 0) ? Math.Sqrt(dispersion) : 0;

            res = sigma * annualizingMultiplier;
            return true;
        }
    }
}
