using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using TSLab.DataSource;
using TSLab.Utils;

// ReSharper disable once CheckNamespace
namespace TSLab.Script.Handlers
{
    public sealed partial class AlignedSecurity : DisposeHelper, ISecurity
    {
        private sealed class AlignedDataBar : IDataBar
        {
            private readonly IDataBar m_bar;

            public AlignedDataBar(IDataBar bar, int originalIndex)
            {
                m_bar = bar;
                OriginalIndex = originalIndex;
            }

            public int OriginalIndex { get; }

            public object Clone()
            {
                throw new NotSupportedException();
            }

            public void Store(BinaryWriter stream)
            {
                throw new NotSupportedException();
            }

            public void Restore(BinaryReader stream, int version)
            {
                throw new NotSupportedException();
            }

            public DateTime Date
            {
                get => m_bar.Date;
                set => m_bar.Date = value;
            }

            public long Ticks
            {
                get => m_bar.Ticks;
                set => m_bar.Ticks = value;
            }

            public double PotensialOpen => m_bar.PotensialOpen;

            public double Open => m_bar.Open;

            public double Low => m_bar.Low;

            public double High => m_bar.High;

            public double Close => m_bar.Close;

            public bool IsAdditional => m_bar.IsAdditional;

            public bool IsReadonly => m_bar.IsReadonly;

            public int TicksCount => m_bar.TicksCount;

            public int OriginalFirstIndex
            {
                get => -1;
                set => throw new NotSupportedException();
            }

            public int OriginalLastIndex
            {
                get => -1;
                set => throw new NotSupportedException();
            }

            public void Add(IBaseBar b2)
            {
                m_bar.Add(b2);
            }

            public double Volume => m_bar.Volume;

            public double Interest => m_bar.Interest;

            public TradeNumber FirstTradeId => m_bar.FirstTradeId;

            public IDataBar MakeAdditional(DateTime newTime, bool byOpen)
            {
                throw new NotSupportedException();
            }
        }
        private static readonly IReadOnlyList<AlignedDataBar> s_emptyBars = new AlignedDataBar[0];
        private static readonly IReadOnlyList<Trade> s_emptyTrades = new Trade[0];
        private readonly ISecurity m_security;
        private readonly TimeSpan m_timeFrame;
        private IReadOnlyList<AlignedDataBar> m_bars;

        public AlignedSecurity(ISecurity security, TimeSpan timeFrame)
        {
            if (timeFrame <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeFrame));

            m_security = security ?? throw new ArgumentNullException(nameof(security));
            m_timeFrame = timeFrame;
            SecurityDescription = new DataSourceSecurity(security.SecurityDescription);
        }

        protected override void Dispose(bool disposing)
        {
        }

        public string Symbol => m_security.Symbol;

        public IDataSourceSecurity SecurityDescription { get; }

        public FinInfo FinInfo => m_security.FinInfo;

        public IReadOnlyList<IDataBar> Bars
        {
            [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1113:CommaMustBeOnSameLineAsPreviousParameter", Justification = "Reviewed.")]
            get
            {
                if (m_bars != null)
                    return m_bars;

                // ReSharper disable PossibleMultipleWriteAccessInDoubleCheckLocking
                lock (this)
                {
                    if (m_bars != null)
                        return m_bars;

                    var originalBars = m_security.Bars;
                    if (originalBars.Count == 0)
                        return m_bars = s_emptyBars;

                    if (originalBars.Count == 1)
                        return m_bars = new[] { new AlignedDataBar(originalBars[0], 0) };

                    for (var i = 1; i < originalBars.Count; i++)
                        if (ReferenceEquals(originalBars[i - 1], originalBars[i]))
                            throw new InvalidOperationException($"There are the same bars at {i - 1} and {i} indexes.");

                    TimeFrameUtils.GetFirstBounds(m_timeFrame, originalBars[0].Date, out var firstDateTime,
                        out var lastDateTime);
                    var interval = GetInterval();
                    var lastClose = double.NaN;
                    var alignedBars = new List<AlignedDataBar>(originalBars.Count);

                    for (int i = 0, firstIndex = 0; i <= originalBars.Count; i++)
                    {
                        if (i == originalBars.Count || originalBars[i].Date >= lastDateTime)
                        {
                            var dateTime = firstDateTime;
                            for (int j = firstIndex, jMax = i - 1; j <= jMax; j++)
                            {
                                var originalBar = originalBars[j];
                                while (dateTime < originalBar.Date)
                                {
                                    alignedBars.Add(new AlignedDataBar(
                                        new DataBar(dateTime, lastClose, lastClose, lastClose, lastClose), -1));
                                    dateTime += interval;
                                }

                                alignedBars.Add(new AlignedDataBar(originalBar, j));
                                dateTime = originalBar.Date + interval;
                                lastClose = originalBar.Close;

                                if (j == jMax)
                                {
                                    while (dateTime < lastDateTime)
                                    {
                                        alignedBars.Add(new AlignedDataBar(
                                            new DataBar(dateTime, lastClose, lastClose, lastClose, lastClose), -1));
                                        dateTime += interval;
                                    }
                                }
                            }

                            if (i == originalBars.Count)
                                break;

                            TimeFrameUtils.GetBounds(m_timeFrame, originalBars[i].Date, ref firstDateTime,
                                ref lastDateTime);
                            firstIndex = i;
                        }
                    }

                    return m_bars = alignedBars;
                }
                // ReSharper restore PossibleMultipleWriteAccessInDoubleCheckLocking
            }
        }

        public bool IsBarsLoaded => m_bars != null;

        private TimeSpan GetInterval()
        {
            switch (IntervalBase)
            {
                case DataIntervals.MONTHS:
                    return TimeSpan.FromDays(Interval * 30);
                case DataIntervals.WEEKS:
                    return TimeSpan.FromDays(Interval * 7);
                case DataIntervals.DAYS:
                    return TimeSpan.FromDays(Interval);
                case DataIntervals.MINUTE:
                    return TimeSpan.FromMinutes(Interval);
                case DataIntervals.SECONDS:
                    return TimeSpan.FromSeconds(Interval);
                default:
                    throw new InvalidEnumArgumentException(nameof(IntervalBase), (int)IntervalBase, IntervalBase.GetType());
            }
        }

        public IReadOnlyList<IQueueData> GetBuyQueue(int barNum)
        {
            throw new NotSupportedException();
        }

        public IReadOnlyList<IQueueData> GetSellQueue(int barNum)
        {
            throw new NotSupportedException();
        }

        public IReadOnlyList<ITrade> GetTrades(int barNum)
        {
            var bars = Bars;
            var bar = (AlignedDataBar)bars[barNum];
            return bar.OriginalIndex >= 0 ? m_security.GetTrades(bar.OriginalIndex) : s_emptyTrades;
        }

        public IReadOnlyList<ITrade> GetTrades(int firstBarIndex, int lastBarIndex)
        {
            var bars = Bars.Skip(firstBarIndex).Take(lastBarIndex - firstBarIndex + 1).Cast<AlignedDataBar>()
                           .Where(item => item.OriginalIndex >= 0);
            var alignedDataBars = bars as AlignedDataBar[] ?? bars.ToArray();
            if (!alignedDataBars.Any())
                return s_emptyTrades;

            var firstBar = alignedDataBars.First();
            var lastBar = alignedDataBars.Last();
            return m_security.GetTrades(firstBar.OriginalIndex, lastBar.OriginalIndex);
        }

        public int GetTradesCount(int firstBarIndex, int lastBarIndex)
        {
            var bars = Bars.Skip(firstBarIndex).Take(lastBarIndex - firstBarIndex + 1).Cast<AlignedDataBar>()
                           .Where(item => item.OriginalIndex >= 0);
            var alignedDataBars = bars as AlignedDataBar[] ?? bars.ToArray();
            if (!alignedDataBars.Any())
                return 0;

            var firstBar = alignedDataBars.First();
            var lastBar = alignedDataBars.Last();
            return m_security.GetTradesCount(firstBar.OriginalIndex, lastBar.OriginalIndex);
        }

        public IReadOnlyList<IReadOnlyList<ITrade>> GetTradesPerBar(int firstBarIndex, int lastBarIndex)
        {
            var count = lastBarIndex - firstBarIndex + 1;
            var bars = Bars.Skip(firstBarIndex).Take(count).Cast<AlignedDataBar>()
                           .Where(item => item.OriginalIndex >= 0);

            var alignedDataBars = bars as AlignedDataBar[] ?? bars.ToArray();
            if (!alignedDataBars.Any())
            {
                var emptyTradesPerBar = new IReadOnlyList<Trade>[count];
                for (var i = 0; i < count; i++)
                    emptyTradesPerBar[i] = s_emptyTrades;

                return emptyTradesPerBar;
            }

            var firstBar = alignedDataBars.First();
            var lastBar = alignedDataBars.Last();
            var originalTradesPerBar = m_security.GetTradesPerBar(firstBar.OriginalIndex, lastBar.OriginalIndex);

            if (firstBarIndex == firstBar.OriginalIndex && lastBarIndex == lastBar.OriginalIndex)
                return originalTradesPerBar;

            var tradesPerBar = new IReadOnlyList<ITrade>[count];
            for (var i = firstBarIndex; i <= lastBarIndex; i++)
            {
                var barOriginalIndex = m_bars[i].OriginalIndex;
                tradesPerBar[i - firstBarIndex] = barOriginalIndex >= 0
                                                      ? originalTradesPerBar[barOriginalIndex - firstBar.OriginalIndex]
                                                      : s_emptyTrades;
            }

            return tradesPerBar;
        }

        public IList<double> OpenPrices => throw new NotSupportedException();

        public IList<double> ClosePrices => throw new NotSupportedException();

        public IList<double> HighPrices => throw new NotSupportedException();

        public IList<double> LowPrices => throw new NotSupportedException();

        public IList<double> Volumes => throw new NotSupportedException();

        public Interval IntervalInstance => m_security.IntervalInstance;

        public int Interval => m_security.Interval;

        public DataIntervals IntervalBase => m_security.IntervalBase;

        public double LotSize => m_security.LotSize;

        public double LotTick => m_security.LotTick;

        public double Margin => m_security.Margin;

        public double Tick => m_security.Tick;

        public int Decimals => m_security.Decimals;

        public IPositionsList Positions => throw new NotSupportedException();

        public ISecurity CompressTo(int interval)
        {
            throw new NotSupportedException();
        }

        public ISecurity CompressTo(Interval interval)
        {
            throw new NotSupportedException();
        }

        public ISecurity CompressTo(Interval interval, int shift)
        {
            throw new NotSupportedException();
        }

        public ISecurity CompressTo(Interval interval, int shift, int adjustment, int adjShift)
        {
            throw new NotSupportedException();
        }

        public ISecurity CompressToVolume(Interval interval)
        {
            throw new NotSupportedException();
        }

        public ISecurity CompressToPriceRange(Interval interval)
        {
            throw new NotSupportedException();
        }

        public IList<double> Decompress(IList<double> candles)
        {
            throw new NotSupportedException();
        }

        public IList<TK> Decompress<TK>(IList<TK> candles, DecompressMethodWithDef method)
            where TK : struct
        {
            throw new NotSupportedException();
        }

        public void ConnectSecurityList(IGraphListBase iList)
        {
            m_security.ConnectSecurityList(iList);
        }

        public void ConnectDoubleList(IGraphListBase list, IDoubleHandlerWithUpdate handler)
        {
            m_security.ConnectDoubleList(list, handler);
        }

        public double RoundPrice(double price) => m_security.RoundPrice(price);

        public double RoundShares(double shares) => m_security.RoundShares(shares);

        public CommissionDelegate Commission
        {
            set => m_security.Commission = value;
            get => m_security.Commission;
        }

        public ISecurity CloneAndReplaceBars(IEnumerable<IDataBar> newCandles) =>
            m_security.CloneAndReplaceBars(newCandles);

        public ISecurity CloneAndReplaceBars(IReadOnlyList<IDataBar> newCandles) =>
            m_security.CloneAndReplaceBars(newCandles);

        public string CacheName => m_security.CacheName;

        public void UpdateQueueData() => m_security.UpdateQueueData();

        public double InitDeposit
        {
            set => m_security.InitDeposit = value;
            get => m_security.InitDeposit;
        }

        public bool IsRealtime => m_security.IsRealtime;

        public bool IsPortfolioReady => m_security.IsPortfolioReady;

        public bool SimulatePositionOrdering => m_security.SimulatePositionOrdering;

        public bool IsAligned => true;
    }
}
