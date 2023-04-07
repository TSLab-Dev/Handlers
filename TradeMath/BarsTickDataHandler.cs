using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TSLab.DataSource;
using TSLab.Script.Handlers.Options;
using TSLab.Utils;

// ReSharper disable once CheckNamespace
namespace TSLab.Script.Handlers
{
    [HandlerCategory(HandlerCategories.TradeMath)]
    [HelperName("Bars tick data", Language = Constants.En)]
    [HelperName("Бары котировочных данных", Language = Constants.Ru)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.SECURITY, Name = "SECURITYSource")]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.SECURITY)]
    [Description("Предназначен для работы с кешируемыми данными из котировок. Строит бары с интервалом источника. Для работы используйте секундный интервал источника. Например, 60 секунд.")]
    [HelperDescription("Designed to work with cached data from quotes. Builds bars with the source interval. To work, use the second interval of the source. For example, 60 seconds.", Constants.En)]
    public sealed class BarsTickDataHandler : BarsConstructorBase, ISecurityReturns, IStreamHandler, ISecurityInputs, 
        IContextUses, INeedVariableName
    {
        [HelperName("Data quotes", Constants.En)]
        [HelperName("Данные котировок", Constants.Ru)]
        [HandlerParameter(true, nameof(BarsTickDataField.BuyCount))]
        public BarsTickDataField Field { get; set; }
        public IContext Context { get; set; }
        public string VariableName { get; set; }

        public ISecurity Execute(ISecurity security)
        {
            var newBars = new IDataBar[Context.BarsCount];
            for (int i = 0; i < newBars.Length; i++)
            {
                var bar = security.Bars[i];
                var trades = GetData(security, i, Field).ToList();
                if (trades.Count == 0)
                {
                    newBars[i] = i > 0 ? newBars[i - 1] : new DataBar(bar.Date, 0, 0, 0, 0);
                }
                else
                {
                    var open = trades.First();
                    var high = trades.Max();
                    var low = trades.Min();
                    var close = trades.Last();
                    newBars[i] = new DataBar(bar.Date, open, high, low, close);
                }
            }
            return new Security(newBars, VariableName, security);
        }

        private static IEnumerable<double> GetData(ISecurity source, int barNum, BarsTickDataField field)
        {
            var trades = source.GetTrades(barNum);
            switch (field)
            {
                case BarsTickDataField.BuyCount:
                    return trades.OfType<ITradeWithBidAsk>().Select(x => x.BuyCount);
                case BarsTickDataField.SellCount:
                    return trades.OfType<ITradeWithBidAsk>().Select(x => x.SellCount);
                case BarsTickDataField.BidQty:
                    return trades.OfType<ITradeWithBidAsk>().Select(x => x.BidQty);
                case BarsTickDataField.AskQty:
                    return trades.OfType<ITradeWithBidAsk>().Select(x => x.AskQty);
                case BarsTickDataField.OpenInterest:
                    return trades.Select(x => x.OpenInterest);
            }
            throw new NotImplementedException(field.ToString());
        }
    }

    public enum BarsTickDataField
    {
        [LocalizeDescription("BarsTickDataField.BuyCount")] // Количество заявок на Покупку
        BuyCount,
        [LocalizeDescription("BarsTickDataField.SellCount")] // Количество заявок на Продажу
        SellCount,
        [LocalizeDescription("BarsTickDataField.BidQty")] // Суммарный Спрос
        BidQty,
        [LocalizeDescription("BarsTickDataField.AskQty")] // Суммарное Предложение
        AskQty,
        [LocalizeDescription("BarsTickDataField.OpenInterest")] // Открытый интерес
        OpenInterest,
    }
}
