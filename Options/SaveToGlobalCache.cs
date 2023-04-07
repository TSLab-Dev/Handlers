using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using TSLab.Script.Options;

namespace TSLab.Script.Handlers.Options
{
    [HandlerCategory(HandlerCategories.OptionsIndicators)]
    [HelperName("Save to Global Cache (old)", Language = Constants.En)]
    [HelperName("Сохранить в Глобальный Кеш (старый)", Language = Constants.Ru)]
    [HandlerAlwaysKeep]
    [InputsCount(2)]
    [Input(0, TemplateTypes.SECURITY | TemplateTypes.OPTION_SERIES | TemplateTypes.OPTION, Name = Constants.AnyOption)]
    [Input(1, TemplateTypes.DOUBLE, Name = "Indicator")]
    [OutputsCount(0)]
    [Description("Сохранить значение любого индикатора в Глобальный Кеш")]
    [HelperDescription("Save any indicator to Global Cache", Constants.En)]
    public class SaveToGlobalCache : BaseContextBimodal<double>, ICustomListValues
    {
        /// <summary>Префикс ключей для организации обмена данными через Глобальный Кеш</summary>
        public const string MqId = "GCMQ";

        #region Parameters
        
        [HelperName("Repeat last value", Constants.En)]
        [HelperName("Повтор значения", Constants.Ru)]
        [Description("При true будет находить и использовать последнее известное значение")]
        [HelperDescription("Handler should repeat last known value to avoid further logic errors", Language = Constants.En)]
        [HandlerParameter(true, NotOptimized = false, IsVisibleInBlock = true, Default = "false")]
        public bool RepeatLastValue { get; set; }

        [HelperName("Save to disk", Constants.En)]
        [HelperName("Сохранять на диск", Constants.Ru)]
        [Description("Сохранять значения в файл на диске для повторного использования между перезапусками программы")]
        [HelperDescription("Save to HDD to use indicator values across different program sessions", Language = Constants.En)]
        [HandlerParameter(NotOptimized = false, IsVisibleInBlock = true, Default = "true")]
        public bool SaveToStorage { get; set; }

        [HelperName("Values name", Constants.En)]
        [HelperName("Название значений", Constants.Ru)]
        [Description("Уникальное название индикатора для целей сохранения в Глобальный Кеш")]
        [HelperDescription("Unique indicator name to be used to store values in Global Cache", Language = Constants.En)]
        [HandlerParameter(true, NotOptimized = false, IsVisibleInBlock = true, Default = "")]
        public string ValuesName { get; set; }

        [HelperName("Maximum numbers", Constants.En)]
        [HelperName("Максимальное количество", Constants.Ru)]
        [Description("Максимальное количество сохраняемых значений. Если 0, то будет ограничиваться количеством баров")]
        [HelperDescription("The maximum number of values. If 0, then it will be limited by the number of bars", Language = Constants.En)]
        [HandlerParameter(true, NotOptimized = false, IsVisibleInBlock = true, Default = "0")]
        public int MaxValues { get; set; }

        #endregion Parameters

        protected override bool IsValid(double val)
        {
            return !Double.IsNaN(val);
        }

        #region Потоковые обработчики
        /// <summary>
        /// Обработчик под тип входных данных OPTION (с потоковой обработкой)
        /// </summary>
        public void Execute(IOption opt, IList<double> indicValues)
        {
            if ((opt == null) || (opt.UnderlyingAsset == null) ||
                (indicValues == null) || (indicValues.Count <= 0))
                return;

            ISecurity sec = opt.UnderlyingAsset;
            ExecuteStream(sec, sec.Symbol, indicValues);
        }

        /// <summary>
        /// Обработчик под тип входных данных OPTION_SERIES (с потоковой обработкой)
        /// </summary>
        public void Execute(IOptionSeries optSer, IList<double> indicValues)
        {
            if ((optSer == null) || (optSer.UnderlyingAsset == null) ||
                (indicValues == null) || (indicValues.Count <= 0))
                return;

            ISecurity sec = optSer.UnderlyingAsset;
            string expiry = optSer.ExpirationDate.ToString(IvOnF.DateFormat, CultureInfo.InvariantCulture);
            string symbolKey = String.Intern(String.Join("_", sec.Symbol, expiry));
            ExecuteStream(sec, symbolKey, indicValues);
        }

        /// <summary>
        /// Обработчик под тип входных данных SECURITY (с потоковой обработкой)
        /// </summary>
        public void Execute(ISecurity sec, IList<double> indicValues)
        {
            if ((sec == null) || (indicValues == null) || (indicValues.Count <= 0))
                return;

            ExecuteStream(sec, sec.Symbol, indicValues);
        }

        /// <summary>
        /// Общая реализация потоковой обработки для всех типов входных аргументов
        /// </summary>
        private void ExecuteStream(ISecurity sec, string symbolKey, IList<double> indicValues)
        {
            //var sw = Stopwatch.StartNew();
            if ((sec == null) || String.IsNullOrWhiteSpace(symbolKey) ||
                (indicValues == null) || (indicValues.Count <= 0))
                return;

            string tradeName = Context.Runtime.TradeName.Replace(Constants.HtmlDot, ".");
            if (String.IsNullOrWhiteSpace(ValuesName))
            {
                string msg = String.Format("[{0}:{1}] ValuesName is null or empty. Please, provide unique series name. ValuesName: '{2}'",
                    tradeName, GetType().Name, ValuesName ?? "NULL");
                m_context.Log(msg, MessageType.Warning, true);
                return;
            }

            string cashKey = SaveToGlobalCache.GetGlobalCashKey(tradeName, ValuesName, symbolKey);
            if (String.IsNullOrWhiteSpace(cashKey))
            {
                string msg = String.Format("[{0}:{1}] cashKey is null or empty. cashKey: '{2}'",
                    tradeName, GetType().Name, cashKey ?? "NULL");
                m_context.Log(msg, MessageType.Warning, true);
                return; // Constants.EmptyListDouble;
            }

            // По факту передаю управление в метод CommonExecute
            var updatedValues = CommonStreamExecute(m_variableId + "_" + cashKey, cashKey,
                sec, RepeatLastValue, true, true, new object[] { indicValues }, SaveToStorage, true, MaxValues);

            //if (basePrices.Count > 0)
            //{
            //    double px = basePrices[basePrices.Count - 1];
            //    double displayValue = FixedValue.ConvertToDisplayUnits(m_valueMode, px);
            //    m_displayPrice.Value = displayValue;
            //}

            //return new ReadOnlyCollection<double>(updatedValues);
            //m_context.Log($"SaveCache time: {sw.Elapsed}", MessageType.Info, true);
        }
        #endregion Потоковые обработчики

        #region Побарные обработчики
        /// <summary>
        /// Обработчик под тип входных данных OPTION (с побарным вызовом)
        /// </summary>
        public void Execute(IOption opt, double indicValue, int barNum)
        {
            if ((opt == null) || Double.IsNaN(indicValue) || Double.IsInfinity(indicValue))
                return;

            ISecurity sec = opt.UnderlyingAsset;
            ExecuteWithBarNumber(sec, sec.Symbol, indicValue, barNum);
        }

        /// <summary>
        /// Обработчик под тип входных данных OPTION_SERIES (с побарным вызовом)
        /// </summary>
        public void Execute(IOptionSeries optSer, double indicValue, int barNum)
        {
            if ((optSer == null) || (optSer.UnderlyingAsset == null) ||
                Double.IsNaN(indicValue) || Double.IsInfinity(indicValue))
                return;

            ISecurity sec = optSer.UnderlyingAsset;
            string expiry = optSer.ExpirationDate.ToString(IvOnF.DateFormat, CultureInfo.InvariantCulture);
            string symbolKey = String.Intern(String.Join("_", sec.Symbol, expiry));
            ExecuteWithBarNumber(sec, symbolKey, indicValue, barNum);
        }

        /// <summary>
        /// Обработчик под тип входных данных SECURITY (с побарным вызовом)
        /// </summary>
        public void Execute(ISecurity sec, double indicValue, int barNum)
        {
            if ((sec == null) || Double.IsNaN(indicValue) || Double.IsInfinity(indicValue))
                return;

            ExecuteWithBarNumber(sec, sec.Symbol, indicValue, barNum);
        }

        /// <summary>
        /// Общая реализация побарной обработки для всех типов входных аргументов
        /// </summary>
        private void ExecuteWithBarNumber(ISecurity sec, string symbolKey, double indicValue, int barNum)
        {
            if ((sec == null) || String.IsNullOrWhiteSpace(symbolKey) ||
                Double.IsNaN(indicValue) || Double.IsInfinity(indicValue))
                return;

            int len = m_context.BarsCount;
            if (len <= 0)
                return;

            // В режиме Лаборатории даже не стоит продолжать -- это может сломать ГК
            if (!Context.Runtime.IsAgentMode)
                return;

            if (len <= barNum)
            {
                string msg = String.Format("[{0}:{1}] (BarsCount <= barNum)! BarsCount:{2}; barNum:{3}",
                    m_context.Runtime.TradeName, GetType().Name, m_context.BarsCount, barNum);
                m_context.Log(msg, MessageType.Warning, true);
                barNum = len - 1;
            }

            string tradeName = Context.Runtime.TradeName.Replace(Constants.HtmlDot, ".");
            if (String.IsNullOrWhiteSpace(ValuesName))
            {
                string msg = String.Format("[{0}:{1}] ValuesName is null or empty. Please, provide unique series name. ValuesName: '{2}'",
                    tradeName, GetType().Name, ValuesName ?? "NULL");
                m_context.Log(msg, MessageType.Warning, true);
                return;
            }

            string cashKey = SaveToGlobalCache.GetGlobalCashKey(tradeName, ValuesName, symbolKey);
            if (String.IsNullOrWhiteSpace(cashKey))
            {
                string msg = String.Format("[{0}:{1}] cashKey is null or empty. cashKey: '{2}'",
                    tradeName, GetType().Name, cashKey ?? "NULL");
                m_context.Log(msg, MessageType.Warning, true);
                return;
            }

            DateTime now = sec.Bars[barNum].Date;
            double updatedIndicValue = CommonExecute(cashKey, now, RepeatLastValue, true, true, barNum, 
                new object[] { indicValue }, SaveToStorage, true, MaxValues);

            //// [2017-06-28] Отключаю вывод отладочных сообщений в лог агента.
            //if (barNum >= 0.9 * len)
            //{
            //    string msg = String.Format("[{0}:{1}] barNum:{2}; updatedIndicValue:{3}; now:{4}",
            //        tradeName, GetType().Name, barNum, updatedIndicValue, now.ToString("dd-MM-yyyy HH:mm:ss.fff"));
            //    m_context.Log(msg, MessageType.Info, false);
            //}

            //// Просто заполнение свойства для отображения на UI
            //int barsCount = ContextBarsCount;
            //if (barNum >= barsCount - 1)
            //{
            //    double displayValue = FixedValue.ConvertToDisplayUnits(m_valueMode, risk);
            //    m_displayRisk.Value = displayValue;
            //}

            //return updatedIndicValue;
        }
        #endregion Побарные обработчики

        protected override bool TryCalculate(Dictionary<DateTime, double> history, DateTime now, int barNum, object[] args, out double val)
        {
            object obj = args[0];
            if (obj is double)
            {
                // Ветка для побарного обработчика
                val = (double)obj;
                // Проверка IsValid делается автоматически внешним кодом в методе CommonExecute
                return true;
            }

            IList<double> indicValues = obj as IList<double>;
            if (indicValues != null)
            {
                val = indicValues[barNum];
                // Проверка IsValid делается автоматически внешним кодом в методе CommonExecute
                return true;
            }

            val = Double.NaN;
            return false;
        }

        /// <summary>
        /// Сформировать уникальный ключ, используя торговое имя агента,
        /// название серии данных и символ инструмента (префикс 'GCMQ_')
        /// </summary>
        /// <param name="tradeName">торговое имя агента</param>
        /// <param name="valuesName">название серии данных</param>
        /// <param name="symbolKey">тикер фьючерса; при работе с опционной серией к нему лучше прибавить дату экспирации СЕРИИ</param>
        /// <returns>уникальный ключ серии данных для обмена через Глобальный Кеш</returns>
        public static string GetGlobalCashKey(string tradeName, string valuesName, string symbolKey)
        {
            if (String.IsNullOrWhiteSpace(tradeName) || String.IsNullOrWhiteSpace(symbolKey) ||
                String.IsNullOrWhiteSpace(valuesName))
                return null;

            string res = String.Intern(String.Join("_", MqId, tradeName, valuesName, symbolKey));
            return res;
        }

        /// <summary>
        /// Это специальный паттерн для поддержки редактируемого строкового параметра
        /// </summary>
        public IEnumerable<string> GetValuesForParameter(string paramName)
        {
            if (paramName.Equals(nameof(ValuesName), StringComparison.InvariantCultureIgnoreCase) ||
                paramName.Equals("Values name", StringComparison.InvariantCultureIgnoreCase) ||
                paramName.Equals("Название значений", StringComparison.InvariantCultureIgnoreCase))
                return new[] { ValuesName ?? "" };
            else
                return new[] { "" };
        }
    }
}
