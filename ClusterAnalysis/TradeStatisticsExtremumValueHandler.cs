﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TSLab.Script.Handlers.Options;

namespace TSLab.Script.Handlers
{
    // TODO: английское описание
    [HandlerCategory(HandlerCategories.ClusterAnalysis)]
    [HelperName("Trade Statistics Extremum Value", Language = Constants.En)]
    [HelperName("Экстремальное значение торговой статистики", Language = Constants.Ru)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.TRADE_STATISTICS | TemplateTypes.LAST_CONTRACTS_TRADE_STATISTICS)]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.DOUBLE)]
    [Description("Блок 'Экстремальное значение торговой статистики' показывает максимальное значение торговой статистики при экстремальной цене.")]
    [HelperDescription("", Constants.En)]
    public sealed class TradeStatisticsExtremumValueHandler : ITradeStatisticsExtremumValueHandler
    {
        public string VariableId { get; set; }

        public IContext Context { get; set; }

        [HelperName("Min Bar, %", Constants.En)]
        [HelperName("Минимальный бар, %", Constants.Ru)]
        [HandlerParameter(true, "0", Min = "0", Max = "100", Step = "1", EditorMin = "0")]
        public double MinBarPct { get; set; }

        [HelperName("Max Bar, %", Constants.En)]
        [HelperName("максимальный бар, %", Constants.Ru)]
        [HandlerParameter(true, "100", Min = "0", Max = "100", Step = "1", EditorMin = "0")]
        public double MaxBarPct { get; set; } = 100;

        public IList<double> Execute(IBaseTradeStatisticsWithKind tradeStatistics)
        {
            var histograms = tradeStatistics.GetHistograms();
            var tradeHistogramsCache = tradeStatistics.TradeHistogramsCache;
            var barsCount = tradeHistogramsCache.Bars.Count;
            const double DefaultValue = double.NaN;

            if (histograms.Count == 0 || histograms.All(item => item.Bars.Count == 0))
                return new ConstGenBase<double>(barsCount, DefaultValue);

            double[] results = null;
            var runtime = Context?.Runtime;
            var canBeCached = tradeStatistics.HasStaticTimeline && barsCount > 1 && runtime != null;
            string id = null, stateId = null;
            DerivativeTradeStatisticsCacheContext context = null;
            var cachedCount = 0;
            var lastResult = DefaultValue;

            if (canBeCached)
            {
                id = string.Join(".", runtime.TradeName, runtime.IsAgentMode, VariableId);
                stateId = string.Join(".", tradeStatistics.StateId, MinBarPct, MaxBarPct);
                context = DerivativeTradeStatisticsCache.Instance.GetContext(id, stateId, tradeHistogramsCache);

                if (context != null)
                {
                    var cachedResults = context.Values;
                    cachedCount = Math.Min(cachedResults.Length, barsCount) - 1;

                    if (cachedResults.Length == barsCount)
                        results = cachedResults;
                    else
                        Buffer.BlockCopy(cachedResults, 0, results = new double[barsCount], 0, cachedCount * sizeof(double));

                    lastResult = results[cachedCount - 1];
                }
                else
                    results = new double[barsCount];
            }
            else
                results = Context?.GetArray<double>(barsCount) ?? new double[barsCount];

            tradeStatistics.GetHistogramsBarIndexes(out var firstBarIndex, out var lastBarIndex);
            for (var i = cachedCount; i < firstBarIndex; i++)
                results[i] = lastResult;

            var aggregatedHistogramBarsProvider = tradeStatistics.CreateAggregatedHistogramBarsProvider();
            for (var i = Math.Max(cachedCount, firstBarIndex); i <= lastBarIndex; i++)
            {
                var bars = aggregatedHistogramBarsProvider.GetAggregatedHistogramBars(i);
                if (MinBarPct > 0 || MaxBarPct < 100)
                {
                    var minBar = (int)(bars.Count * MinBarPct / 100.0);
                    var maxBar = (int)(bars.Count * MaxBarPct / 100.0);
                    bars = bars.Skip(minBar).Take(maxBar - minBar).ToList();
                }

                if (bars.Count > 0)
                {
                    double maxValue, minValue;
                    maxValue = minValue = tradeStatistics.GetValue(bars[0]);

                    foreach (var bar in bars.Skip(1))
                    {
                        var value = tradeStatistics.GetValue(bar);
                        if (maxValue < value)
                            maxValue = value;
                        else if (minValue > value)
                            minValue = value;
                    }
                    lastResult = Math.Abs(maxValue) >= Math.Abs(minValue) ? maxValue : minValue;
                }
                results[i] = lastResult;
            }
            for (var i = Math.Max(cachedCount, lastBarIndex + 1); i < barsCount; i++)
                results[i] = lastResult;

            if (canBeCached)
                DerivativeTradeStatisticsCache.Instance.SetContext(id, stateId, tradeHistogramsCache, results, context);

            return results;
        }
    }
}
