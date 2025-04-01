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
                return m_source + ".Aligned";
            }

            public string Id => m_source.Id;

            public string Name => m_source.Name;

            public string FullName => m_source.FullName;

            public string Comment => m_source.Comment;

            public string Currency => m_source.Currency;

            public IDataSourceTradePlace TradePlace => m_source.TradePlace;

            public string DSName => m_source.DSName + ".Aligned";

            public string ProviderName => m_source.ProviderName + ".Aligned";

            public double LotSize => m_source.LotSize;

            public double LotTick => m_source.LotTick;

            public double Margin => m_source.Margin;

            public int Decimals => m_source.Decimals;

            public int BalanceDecimals => m_source.BalanceDecimals;

            public int BalancePriceDecimals => m_source.BalancePriceDecimals;

            public double Tick => m_source.Tick;

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

            public double ReducedPrice(double price, CalcSourceParams? calcParams)
            {
                return m_source.ReducedPrice(price, calcParams);
            }

            public double CalculatePnL(double entryPrice, double exitPrice, double lots)
            {
                return m_source.CalculatePnL(entryPrice, exitPrice, lots);
            }

            public double CalculatePnL(double entryPrice, double exitPrice, double lots, CalcSourceParams? calcParams)
            {
                return m_source.CalculatePnL(entryPrice, exitPrice, lots, calcParams);
            }

            public double GetCostPrice(CalcSourceParams? calcParams)
            {
                return m_source.GetCostPrice(calcParams);
            }
        }
    }
}
