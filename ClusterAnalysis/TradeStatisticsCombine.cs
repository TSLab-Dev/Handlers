using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using TSLab.DataSource;
using TSLab.Script.Handlers.Options;

namespace TSLab.Script.Handlers.ClusterAnalysis
{
    [HandlerCategory(HandlerCategories.ClusterAnalysis)]
    [HelperName("Stacked Trading Statistics", Language = Constants.En)]
    [HelperName("Сложенная Торговая статистика", Language = Constants.Ru)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.TRADE_STATISTICS)]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.TRADE_STATISTICS)]
    [Description("Блок 'Сложенная Торговая статистика' складывает последние N, поданных на вход.")]
    [HelperDescription("", Constants.En)]
    public sealed class TradeStatisticsCombineHandler : BasePeriodIndicatorHandler,
                                                        IOneSourceHandler,
                                                        IStreamHandler,
                                                        INeedVariableId,
                                                        IContextUses,
                                                        ITradeStatisticsReturns
    {
        public ITradeStatisticsWithKind Execute(ITradeStatisticsWithKind input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            var runTime = Context.Runtime;
            var id = runTime != null
                         ? string.Join(".", runTime.TradeName, runTime.IsAgentMode, VariableId)
                         : VariableId;
            var stateId = string.Join(".", input.StateId, Period);
            var tradeStatistics = Context.GetTradeStatistics(stateId,
                () => new TradeStatisticsCombine(id, stateId, Period, input));
            return new TradeStatisticsWithKind(tradeStatistics, input.Kind, input.WidthPercent);
        }

        public string VariableId { get; set; }

        public IContext Context { get; set; }
    }

    public sealed class TradeStatisticsCombine : BaseTradeStatistics<TradeHistogramSettings>, ITradeStatistics
    {
        private readonly ITradeStatisticsWithKind m_input;

        public TradeStatisticsCombine(string id, string stateId, int period, ITradeStatisticsWithKind input)
            : base(id, stateId)
        {
            m_input = input;
            m_histogramSettings = new TradeHistogramSettings(input.TradeHistogramsCache,
                TimeFrameKind.FromMidnightToNow, input.GetTimeFrameUnit(), new Interval(period, DataIntervals.MINUTE));
            CalculateHistograms();
        }

        public override bool HasStaticTimeline => m_input.HasStaticTimeline;

        protected override IReadOnlyList<ITradeHistogram> CalculateHistograms()
        {
            var histograms = new List<ITradeHistogram>();

            var queue = new Queue<ITradeHistogram>();
            var period = m_histogramSettings.Interval.Value;
            foreach (var histogram in m_input.GetHistograms())
            {
                if (queue.Count >= period)
                    queue.Dequeue();
                queue.Enqueue(histogram);

                histograms.Add(new CombinedHistogram(m_histogramSettings, queue.First(), queue.Last()));
            }

            Parallel.ForEach(histograms.Where(item => !item.IsCalculated), (item) => item.Recalculate());

            m_histograms = histograms;
            return histograms;
        }

        public TimeSpan TimeFrameShift => m_input.TimeFrameShift;

        public TimeFrameUnit GetTimeFrameUnit() => m_input.GetTimeFrameUnit();

        public override ITradeHistogram GetLastHistogram()
        {
            return m_histograms.LastOrDefault();
        }
    }

    public class CombinedHistogram : BaseRefreshableTradeHistogram<TradeHistogramSettings>
    {
        public override int RealFirstBarIndex { get; }

        internal CombinedHistogram(TradeHistogramSettings histogramSettings, ITradeHistogram first, ITradeHistogram last)
            : base(histogramSettings)
        {
            RealFirstBarIndex = first.FirstBarIndex;
            FirstBarIndex = last.FirstBarIndex;
            LastBarIndex = last.LastBarIndex;
            LowDate = first.LowDate;
            HighDate = last.HighDate;
        }

        protected CombinedHistogram()
        {
        }

        protected override IReadOnlyList<ICachedTradeHistogram> GetCachedTradeHistograms()
        {
            return TradeHistogramsCache.GetHistograms(RealFirstBarIndex, LastBarIndex);
        }
    }
}
