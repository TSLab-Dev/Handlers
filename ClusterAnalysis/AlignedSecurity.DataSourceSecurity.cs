using System;
using TSLab.DataSource;

// ReSharper disable once CheckNamespace
namespace TSLab.Script.Handlers
{
    public sealed partial class AlignedSecurity
    {
        private sealed class DataSourceSecurity : IDataSourceSecurity
        {
            private readonly IDataSourceSecurity m_source;

            public DataSourceSecurity(IDataSourceSecurity source)
            {
                m_source = source ?? throw new ArgumentNullException(nameof(source));
            }

            public string ToString(string format, IFormatProvider formatProvider)
            {
                throw new NotSupportedException();
            }

            public string Id => throw new NotSupportedException();

            public string Name => throw new NotSupportedException();

            public string FullName => throw new NotSupportedException();

            public string Comment => throw new NotSupportedException();

            public string Currency => throw new NotSupportedException();

            public IDataSourceTradePlace TradePlace => throw new NotSupportedException();

            public string DSName => m_source.DSName + ".Aligned";

            public double LotSize => throw new NotSupportedException();

            public double LotTick => throw new NotSupportedException();

            public double Margin => throw new NotSupportedException();

            public int Decimals => throw new NotSupportedException();

            public int BalanceDecimals => throw new NotSupportedException();

            public int BalancePriceDecimals => throw new NotSupportedException();

            public double Tick => throw new NotSupportedException();

            public double GetTick(double price)
            {
                throw new NotSupportedException();
            }

            public bool Expired => throw new NotSupportedException();

            public bool IsMoney => throw new NotSupportedException();

            public ActiveType ActiveType => throw new NotSupportedException();

            public bool IsOption => throw new NotSupportedException();

            public IDataSourceSecurity BaseSecurity => throw new NotSupportedException();

            public CalcSourceInfo CalcSecurity { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

            public double Strike => throw new NotSupportedException();

            public DateTime ExpirationDate => throw new NotSupportedException();

            public double ReducedPrice(double price) => m_source.ReducedPrice(price);

            public double ReducedPrice(double price, CalcSourceParams calcParams)
            {
                return m_source.ReducedPrice(price, calcParams);
            }

            public double CalculatePnL(double entryPrice, double exitPrice, double lots)
            {
                return m_source.CalculatePnL(entryPrice, exitPrice, lots);
            }

            public double CalculatePnL(double entryPrice, double exitPrice, double lots, CalcSourceParams calcParams)
            {
                return m_source.CalculatePnL(entryPrice, exitPrice, lots, calcParams);
            }

            public double GetCostPrice(CalcSourceParams calcParams)
            {
                return m_source.GetCostPrice(calcParams);
            }
        }
    }
}
