using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TSLab.DataSource;
using TSLab.Utils;

// ReSharper disable once CheckNamespace
namespace TSLab.Script.Handlers
{
    public abstract class BarsConstructorBase
    {
        #region ISecurity
        protected sealed class Security : DisposeHelper, ISecurity
        {
            private readonly ISecurity m_security;

            public Security(IReadOnlyList<IDataBar> bars, string symbol, ISecurity security)
                : this(bars, symbol, security, security.IntervalInstance)
            {
            }

            private Security(IReadOnlyList<IDataBar> bars, string symbol, ISecurity security, Interval interval)
            {
                Bars = bars;
                Symbol = symbol;
                Positions = new PositionsList(security);
                m_security = security;
                IntervalInstance = interval;
            }

            protected override void Dispose(bool disposing)
            {
            }

            public string Symbol { get; }
            public IDataSourceSecurity SecurityDescription => throw new NotSupportedException();
            public FinInfo FinInfo => throw new NotSupportedException();
            public IReadOnlyList<IDataBar> Bars { get; }
            public bool IsBarsLoaded => true;
            public IReadOnlyList<IQueueData> GetBuyQueue(int barNum) => throw new NotSupportedException();
            public IReadOnlyList<IQueueData> GetSellQueue(int barNum) => throw new NotSupportedException();
            public IReadOnlyList<ITrade> GetTrades(int barNum) => throw new NotSupportedException();
            public IReadOnlyList<ITrade> GetTrades(int firstBarIndex, int lastBarIndex) => throw new NotSupportedException();
            public int GetTradesCount(int firstBarIndex, int lastBarIndex) => throw new NotSupportedException();
            public IReadOnlyList<IReadOnlyList<ITrade>> GetTradesPerBar(int firstBarIndex, int lastBarIndex) => throw new NotSupportedException();
            public IList<double> OpenPrices => Bars.Select(item => item.Open).ToArray();
            public IList<double> ClosePrices => Bars.Select(item => item.Close).ToArray();
            public IList<double> HighPrices => Bars.Select(item => item.High).ToArray();
            public IList<double> LowPrices => Bars.Select(item => item.Low).ToArray();
            public IList<double> Volumes => Bars.Select(item => item.Volume).ToArray();
            public Interval IntervalInstance { get; }
            public int Interval => IntervalInstance.Value;
            public DataIntervals IntervalBase => IntervalInstance.Base;

            public DateTime SessionBegin => m_security.SessionBegin;

            public DateTime SessionEnd => m_security.SessionEnd;

            public double LotSize => m_security.LotSize;
            public double LotTick => m_security.LotTick;
            public double Margin => m_security.Margin;
            public double Tick => m_security.Tick;
            public int Decimals => m_security.Decimals;
            public IPositionsList Positions { get; }
            public ISecurity CompressTo(int interval) => CompressTo(new Interval(interval, DataIntervals.MINUTE));
            public ISecurity CompressTo(Interval interval) => CompressTo(interval, 0, 0, 0);
            public ISecurity CompressTo(Interval interval, int shift) => CompressTo(interval, shift, 0, 0);

            public ISecurity CompressTo(Interval interval, int shift, int adjustment, int adjShift)
            {
                var bars = BarUtils.CompressTo(Bars, interval, IntervalInstance, 0, shift, adjustment, adjShift);
                return CompressTo(interval, bars);
            }

            public ISecurity CompressToVolume(Interval interval)
            {
                var bars = BarUtils.CompressToVolume(Bars, interval);
                return CompressTo(interval, bars);
            }

            public ISecurity CompressToPriceRange(Interval interval)
            {
                var bars = BarUtils.CompressToPriceRange(Bars, interval, Tick);
                return CompressTo(interval, bars);
            }

            private ISecurity CompressTo(Interval interval, IReadOnlyList<IDataBar> bars) => new Security(bars, Symbol, this, interval);
            public IList<double> Decompress(IList<double> candles) => throw new NotSupportedException();

            public IList<TK> Decompress<TK>(IList<TK> candles, DecompressMethodWithDef method)
                where TK : struct
            {
                throw new NotSupportedException();
            }

            public void ConnectSecurityList(IGraphListBase iList)
            {
            }

            public void ConnectDoubleList(IGraphListBase list, IDoubleHandlerWithUpdate handler)
            {
            }

            public double RoundPrice(double price) => m_security.RoundPrice(price);
            public double RoundShares(double shares) => m_security.RoundShares(shares);
            public CommissionDelegate Commission { get; set; }
            public ISecurity CloneAndReplaceBars(IEnumerable<IDataBar> newcandles) => throw new NotSupportedException();
            public ISecurity CloneAndReplaceBars(IReadOnlyList<IDataBar> newCandles) => throw new NotSupportedException();
            public string CacheName => throw new NotSupportedException();

            public void UpdateQueueData()
            {
            }

            public double InitDeposit { get; set; }
            public bool IsRealtime => false;
            public bool IsPortfolioReady => false;
            public bool SimulatePositionOrdering => false;
            public bool IsAligned => true;
        }
        #endregion

        #region IPositionsList
        protected sealed class PositionsList : IPositionsList
        {
            public PositionsList(ISecurity security)
            {
                Security = security;
            }

            public IEnumerator<IPosition> GetEnumerator() => Enumerable.Empty<IPosition>().GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool IsRealtime => throw new NotSupportedException();
            public ISecurity Security { get; }
            public int BarsCount => Security.Bars.Count;
            public bool HavePositions => false;
            public int ActivePositionCount => 0;

            public int PositionCount => 0;

            public IPosition GetLastPosition(int barNum) => null;
            public IPosition GetLastPositionActive(int barNum) => null;
            public IPosition GetLastLongPositionActive(int barNum) => null;
            public IPosition GetLastShortPositionActive(int barNum) => null;
            public IPosition GetLastPositionClosed(int barNum) => null;
            public IPosition GetLastLongPositionClosed(int barNum) => null;
            public IPosition GetLastShortPositionClosed(int barNum) => null;
            public IPosition GetLastForSignal(string signalName, int barNum) => null;
            public IPosition GetLastActiveForSignal(string signalName) => null;
            public IPosition GetLastClosedForSignal(string signalName, int barNum) => null;
            public IPosition GetLastActiveForSignal(string signalName, int barNum) => null;
            public IPosition GetLastForCloseSignal(string signalName, int barNum) => null;
            public IEnumerable<IPosition> GetActiveForBar(int barNum) => Enumerable.Empty<IPosition>();
            public IEnumerable<IPosition> GetClosedForBar(int barNum) => Enumerable.Empty<IPosition>();
            public IEnumerable<IPosition> GetClosedOrActiveForBar(int barNum) => Enumerable.Empty<IPosition>();

            #region Buy
            public void BuyAtMarket(int barNum, double shares, string signalName, string notes = null) => throw new NotSupportedException();
            public void BuyAtPrice(int barNum, double shares, double price, string signalName, string notes = null) => throw new NotSupportedException();
            public void BuyIfLess(int barNum, double shares, double price, string signalName, string notes = null) => throw new NotSupportedException();
            public void BuyIfLess(int barNum, double shares, double price, double? slippage, string signalName, string notes = null) => throw new NotSupportedException();
            public void BuyIfGreater(int barNum, double shares, double price, string signalName, string notes = null) => throw new NotSupportedException();
            public void BuyIfGreater(int barNum, double shares, double price, double? slippage, string signalName, string notes = null) => throw new NotSupportedException();
            #endregion

            #region Buy
            public void BuyAtMarket(int barNum, double shares, string signalName, string notes, PositionExecution execution) => throw new NotSupportedException();
            public void BuyAtPrice(int barNum, double shares, double price, string signalName, string notes, PositionExecution execution) => throw new NotSupportedException();
            public void BuyIfLess(int barNum, double shares, double price, string signalName, string notes, PositionExecution execution) => throw new NotSupportedException();
            public void BuyIfLess(int barNum, double shares, double price, double? slippage, string signalName, string notes, PositionExecution execution) => throw new NotSupportedException();
            public void BuyIfGreater(int barNum, double shares, double price, string signalName, string notes, PositionExecution execution) => throw new NotSupportedException();
            public void BuyIfGreater(int barNum, double shares, double price, double? slippage, string signalName, string notes, PositionExecution execution) => throw new NotSupportedException();
            #endregion

            #region Sell
            public void SellAtMarket(int barNum, double shares, string signalName, string notes = null) => throw new NotSupportedException();
            public void SellAtPrice(int barNum, double shares, double price, string signalName, string notes = null) => throw new NotSupportedException();
            public void SellIfGreater(int barNum, double shares, double price, string signalName, string notes = null) => throw new NotSupportedException();
            public void SellIfGreater(int barNum, double shares, double price, double? slippage, string signalName, string notes = null) => throw new NotSupportedException();
            public void SellIfLess(int barNum, double shares, double price, string signalName, string notes = null) => throw new NotSupportedException();
            public void SellIfLess(int barNum, double shares, double price, double? slippage, string signalName, string notes = null) => throw new NotSupportedException();
            #endregion

            #region Sell
            public void SellAtMarket(int barNum, double shares, string signalName, string notes, PositionExecution execution) => throw new NotSupportedException();
            public void SellAtPrice(int barNum, double shares, double price, string signalName, string notes, PositionExecution execution) => throw new NotSupportedException();
            public void SellIfGreater(int barNum, double shares, double price, string signalName, string notes, PositionExecution execution) => throw new NotSupportedException();
            public void SellIfGreater(int barNum, double shares, double price, double? slippage, string signalName, string notes, PositionExecution execution) => throw new NotSupportedException();
            public void SellIfLess(int barNum, double shares, double price, string signalName, string notes, PositionExecution execution) => throw new NotSupportedException();
            public void SellIfLess(int barNum, double shares, double price, double? slippage, string signalName, string notes, PositionExecution execution) => throw new NotSupportedException();
            #endregion

            #region Open
            public void OpenAtMarket(bool isLong, int barNum, double shares, string signalName, string notes = null) => throw new NotSupportedException();
            public void OpenAtPrice(bool isLong, int barNum, double shares, double price, string signalName, string notes = null) => throw new NotSupportedException();
            public void OpenIfLess(bool isLong, int barNum, double shares, double price, string signalName, string notes = null) => throw new NotSupportedException();
            public void OpenIfLess(bool isLong, int barNum, double shares, double price, double? slippage, string signalName, string notes = null) => throw new NotSupportedException();
            public void OpenIfGreater(bool isLong, int barNum, double shares, double price, string signalName, string notes = null) => throw new NotSupportedException();
            public void OpenIfGreater(bool isLong, int barNum, double shares, double price, double? slippage, string signalName, string notes = null) => throw new NotSupportedException();
            #endregion

            #region Open
            public void OpenAtMarket(bool isLong, int barNum, double shares, string signalName, string notes, PositionExecution execution) => throw new NotSupportedException();
            public void OpenAtPrice(bool isLong, int barNum, double shares, double price, string signalName, string notes, PositionExecution execution) => throw new NotSupportedException();
            public void OpenIfLess(bool isLong, int barNum, double shares, double price, string signalName, string notes, PositionExecution execution) => throw new NotSupportedException();
            public void OpenIfLess(bool isLong, int barNum, double shares, double price, double? slippage, string signalName, string notes, PositionExecution execution) => throw new NotSupportedException();
            public void OpenIfGreater(bool isLong, int barNum, double shares, double price, string signalName, string notes, PositionExecution execution) => throw new NotSupportedException();
            public void OpenIfGreater(bool isLong, int barNum, double shares, double price, double? slippage, string signalName, string notes, PositionExecution execution) => throw new NotSupportedException();
            #endregion

            public IPosition MakeVirtualPosition(int barNum, double shares, double price, string signalName, string notes = null) => throw new NotSupportedException();
        }
        #endregion
    }
}
