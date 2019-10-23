using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TSLab.Script.Handlers.Options;
using TSLab.Utils;

namespace TSLab.Script.Handlers
{
    //[HandlerCategory(HandlerCategories.VolumeAnalysis)]
    // Не стоит навешивать на абстрактные классы атрибуты категорий и описания входов/выходов. Это снижает гибкость управления в перспективе.
    public abstract class BuysSellsHandler : IBuysSellsHandler
    {
        public IContext Context { get; set; }

        /// <summary>
        /// \~english Market volume quantity units (shares, lots, trades count)
        /// \~russian Режим отображения рыночной статистики (объем, лоты, количество сделок)
        /// </summary>
        [HelperName("Quantity units", Constants.En)]
        [HelperName("Единицы объема", Constants.Ru)]
        [Description("Режим отображения рыночной статистики (объем, лоты, количество сделок)")]
        [HelperDescription("Market volume quantity units (shares, lots, trades count)", Constants.En)]
        [HandlerParameter(true, nameof(QuantityMode.Quantity))]
        public QuantityMode QuantityMode { get; set; }

        public IList<double> Execute(ISecurity security)
        {
            if (security == null)
                throw new ArgumentNullException(nameof(security));

            var quantityMode = QuantityMode;
            if (quantityMode != QuantityMode.Quantity && quantityMode != QuantityMode.QuantityInLots && quantityMode != QuantityMode.TradesCount)
                throw new InvalidEnumArgumentException(nameof(QuantityMode), (int)quantityMode, quantityMode.GetType());

            var compressedBars = security.Bars;
            var compressedBarsCount = compressedBars.Count;

            if (compressedBarsCount == 0)
                return EmptyArrays.Double;

            var decompressedSecurity = Context.Runtime.Securities.First(item => item.SecurityDescription.Id == security.SecurityDescription.Id);
            var decompressedBars = decompressedSecurity.Bars;
            var decompressedBarsCount = decompressedBars.Count;
            var decompressedIndex = 0;
            var getValueFunc = GetValueFunc(decompressedSecurity);
            double[] results;

            if (ReferenceEquals(decompressedSecurity, security))
            {
                results = Context.GetArray<double>(decompressedBarsCount);
                while (decompressedIndex < decompressedBarsCount)
                    results[decompressedIndex] = getValueFunc(decompressedIndex++);
            }
            else
            {
                results = Context.GetArray<double>(compressedBarsCount);
                var lastCompressedIndex = compressedBarsCount - 1;
                double compressedResult;

                for (var compressedIndex = 0; compressedIndex < lastCompressedIndex; compressedIndex++)
                {
                    var nextCompressedDate = compressedBars[compressedIndex + 1].Date;
                    compressedResult = 0;

                    while (decompressedIndex < decompressedBarsCount && decompressedBars[decompressedIndex].Date < nextCompressedDate)
                        compressedResult += getValueFunc(decompressedIndex++);

                    results[compressedIndex] = compressedResult;
                }
                compressedResult = 0;
                while (decompressedIndex < decompressedBarsCount)
                    compressedResult += getValueFunc(decompressedIndex++);

                results[lastCompressedIndex] = compressedResult;
            }
            return results;
        }

        private Func<int, double> GetValueFunc(ISecurity security)
        {
            var tradeHistogramsCache = TradeHistogramsCaches.Instance.GetTradeHistogramsCache(Context, security, 0);
            var lotSize = security.LotSize;

            switch (QuantityMode)
            {
                case QuantityMode.Quantity:
                    return index => GetValue(tradeHistogramsCache.GetHistogram(index));
                case QuantityMode.QuantityInLots:
                    return index => GetValue(tradeHistogramsCache.GetHistogram(index)) / lotSize;
                default:
                    return index => GetCount(tradeHistogramsCache.GetHistogram(index));
            }
        }

        protected abstract double GetValue(ICachedTradeHistogram histogram);

        protected abstract int GetCount(ICachedTradeHistogram histogram);
    }
}
