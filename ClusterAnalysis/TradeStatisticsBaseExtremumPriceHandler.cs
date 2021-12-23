using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using TSLab.Script.Handlers.Options;

// ReSharper disable once CheckNamespace
namespace TSLab.Script.Handlers
{
    //[HandlerCategory(HandlerCategories.ClusterAnalysis)]
    // Не стоит навешивать на абстрактные классы атрибуты категорий и описания входов/выходов. Это снижает гибкость управления в перспективе.

    /// <summary>
    /// Базовый класс для расчета экстремумов торговой стаистистики.
    /// На вход подается Торговая Статистика, на выход идут числа.
    /// </summary>
    public abstract class TradeStatisticsBaseExtremumPriceHandler : ITradeStatisticsBaseExtremumPriceHandler
    {
        protected sealed class Extremum
        {
            public Extremum(ITradeHistogramBar bar, double value, double price)
            {
                Bar = bar;
                Value = value;
                Price = price;
            }

            public ITradeHistogramBar Bar { get; }
            public double Value { get; }
            public double Price { get; }
        }

        public string VariableId { get; set; }

        public IContext Context { get; set; }

        /// <summary>
        /// \~english Extremum kind (minimum, maximum).
        /// \~russian Вид экстремума (минимум, максимум).
        /// </summary>
        [HelperName("Extremum kind", Constants.En)]
        [HelperName("Вид экстремума", Constants.Ru)]
        [Description("Вид экстремума (минимум, максимум)")]
        [HelperDescription("Extremum kind (minimum, maximum).", Constants.En)]
        [HandlerParameter(true, nameof(ExtremumPriceMode.Minimum))]
        public ExtremumPriceMode PriceMode { get; set; }

        [HelperName("Min Bar, %", Constants.En)]
        [HelperName("Минимальный бар, %", Constants.Ru)]
        [HandlerParameter(true, "0", Min = "0", Max = "100", Step = "1", EditorMin = "0")]
        public double MinBarPct { get; set; }

        [HelperName("Max Bar, %", Constants.En)]
        [HelperName("максимальный бар, %", Constants.Ru)]
        [HandlerParameter(true, "100", Min = "0", Max = "100", Step = "1", EditorMin = "0")]
        public double MaxBarPct { get; set; } = 100;

        public abstract IList<double> Execute(IBaseTradeStatisticsWithKind tradeStatistics);

        protected Extremum GetExtremum(IBaseTradeStatisticsWithKind tradeStatistics,
                                       IAggregatedHistogramBarsProvider aggregatedHistogramBarsProvider, int barIndex,
                                       ref double lastPrice)
        {
            var bars = aggregatedHistogramBarsProvider.GetAggregatedHistogramBars(barIndex);
            if (bars.Count == 0)
                return new Extremum(null, double.NaN, lastPrice);

            if (bars.Count == 1)
            {
                var bar = bars[0];
                return new Extremum(bar, Math.Abs(tradeStatistics.GetValue(bar)), lastPrice = bar.AveragePrice);
            }

            var minBar = (int)(bars.Count * MinBarPct / 100.0);
            var maxBar = (int)(bars.Count * MaxBarPct / 100.0);
            var count = maxBar - minBar;

            var reverse = PriceMode == ExtremumPriceMode.Maximum;
            var start = reverse ? bars.Count - minBar - 1 : minBar;
            var step = reverse ? -1 : 1;

            var extremumBar = bars[start];
            var extremumValue = Math.Abs(tradeStatistics.GetValue(extremumBar));

            for (int i = start, k = 0; k++ < count - 1;)
            {
                var bar = bars[i += step];
                var value = Math.Abs(tradeStatistics.GetValue(bar));
                if (extremumValue >= value)
                    continue;
                extremumBar = bar;
                extremumValue = value;
            }

            return new Extremum(extremumBar, extremumValue, lastPrice = extremumBar.AveragePrice);
        }

        protected virtual string GetParametersStateId()
        {
            return string.Join(".", PriceMode.ToString(), MinBarPct.ToString(CultureInfo.InvariantCulture),
                MaxBarPct.ToString(CultureInfo.InvariantCulture));
        }
    }
}
