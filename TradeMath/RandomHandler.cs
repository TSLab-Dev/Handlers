using System;
using System.Collections.Generic;
using System.ComponentModel;
using TSLab.DataSource;
using TSLab.Script.Handlers.Options;
using Random = System.Random;

namespace TSLab.Script.Handlers
{
    [HandlerCategory(HandlerCategories.TradeMath)]
    [HelperName("Random number", Language = Constants.En)]
    [HelperName("Случайное число", Language = Constants.Ru)]
    [Description("Случайное число в заданном диапазоне.")]
    [HelperDescription("Random number in the specified range.", Constants.En)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.SECURITY, Name = Constants.SecuritySource)]
    public sealed class RandomHandler : IDoubleReturns, IStreamHandler, IContextUses, INeedVariableId
    {
        public IContext Context { get; set; }

        public string VariableId { get; set; }

        [HelperName("Min value", Constants.En)]
        [HelperName("Мин. значение", Constants.Ru)]
        [HandlerParameter(true, "1")]
        public double MinValue { get; set; }

        [HelperName("Max value", Constants.En)]
        [HelperName("Макс. значение", Constants.Ru)]
        [HandlerParameter(true, "10")]
        public double MaxValue { get; set; }

        [HelperName("Precision", Constants.En)]
        [HelperName("Точность", Constants.Ru)]
        [Description("Число знаков после запятой.")]
        [HelperDescription("The number of decimal places.", Constants.En)]
        [HandlerParameter(true, "0")]
        public int Precision { get; set; }

        [HelperName("Seed", Constants.En)]
        [HelperName("Seed", Constants.Ru)]
        [Description(
            "Число, используемое для вычисления начального значения последовательности псевдослучайных чисел. (0 - не задано).")]
        [HelperDescription(
            "A number used to calculate a starting value for the pseudo-random number sequence. (0 - not set).",
            Constants.En)]
        [HandlerParameter(true, "0")]
        public int Seed { get; set; }

        [HelperName("Save history", Constants.En)]
        [HelperName("Сохранить историю", Constants.Ru)]
        [HandlerParameter(true, "true")]
        public bool SaveHistory { get; set; }

        public IList<double> Execute(ISecurity sec)
        {
            if (SaveHistory)
                return GetByHistory(sec);
            else
                return GetByRealtime(sec);
        }

        public IList<double> GetByRealtime(ISecurity sec)
        {
            var barsCount = Context.IsLastBarUsed ? Context.BarsCount : Context.BarsCount - 1;
            var array = new double[barsCount];
            for (var i = 0; i < barsCount; i++)
            {
                array[i] = GetNextValue();
            }

            return array;
        }

        private IList<double> GetByHistory(ISecurity sec)
        {
            var barsCount = Context.IsLastBarUsed ? Context.BarsCount : Context.BarsCount - 1;
            var array = new double[barsCount];

            var cashKey = GetType().Name + "_historyRnd_" + VariableId;
            var content = Context.LoadObject(cashKey) as NotClearableContainer<Dictionary<DateTime, double>>;
            var dic = content?.Content ?? new Dictionary<DateTime, double>();
            var bars = sec.Bars;

            for (var i = 0; i < barsCount; i++)
            {
                var date = bars[i].Date;
                if (!dic.TryGetValue(date, out var res) || Double.IsNaN(res))
                {
                    res = GetNextValue();
                    dic[date] = res;
                }

                array[i] = res;
            }

            Context.StoreObject(cashKey, new NotClearableContainer<Dictionary<DateTime, double>>(dic));
            return array;
        }

        private double GetNextValue()
        {
            var v = Random.NextDouble() * (MaxValue - MinValue) + MinValue;
            v = Math.Round(v, Precision);
            return v;
        }

        private Random m_random;

        private Random Random
        {
            get
            {
                if (m_random == null)
                {
                    var seed = Seed;
                    if (seed == 0)
                        seed = Environment.TickCount ^ GetHashCode();
                    m_random = new Random(seed);
                }

                return m_random;
            }
        }
    }
}