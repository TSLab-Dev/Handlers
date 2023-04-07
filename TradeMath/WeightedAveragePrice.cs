using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TSLab.DataSource;
using TSLab.Script.Handlers.Options;
using TSLab.Utils;

namespace TSLab.Script.Handlers
{
    [HandlerCategory(HandlerCategories.TradeMath)]
    [HelperName("Weighted average bar price", Language = Constants.En)]
    [HelperName("Средневзвешенная цена бара", Language = Constants.Ru)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.SECURITY, Name = "SECURITYSource")]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.DOUBLE)]
    [Description("Блок считает средневзвешенную цену бара, основываясь на сделках по инструменту. " +
        "Для правильной работы используйте секундный график. 1 мин = 60 сек.")]
    [HelperDescription("The block contains the weighted average price of a bar, about on trades on the instrument. " +
        "For correct work, use the second chart. 1 min = 60 sec.", Constants.En)]
    public class WeightedAveragePrice : IStreamHandler, IContextUses
    {
        [HelperName("Direction trades", Constants.En)]
        [HelperName("Направление сделок", Constants.Ru)]
        [HandlerParameter(true, nameof(TradeDirection2.All))]
        public TradeDirection2 Direction { get; set; }
        
        public IContext Context { get; set; }

        public IList<double> Execute(ISecurity source)
        {
            var res = Context.GetArray<double>(Context.BarsCount);
            for (int i = 0; i < res.Length; i++)
            {
                var trades = GetTrades(source, i, Direction).ToList();
                var value = trades.Sum(x => x.Quantity * x.Price) / trades.Sum(x => x.Quantity);
                res[i] = DoubleUtil.IsNumber(value) ? value : i > 0 ? res[i - 1] : 0;
            }
            return res;
        }

        private static IEnumerable<ITrade> GetTrades(ISecurity source, int barNum, TradeDirection2 direction)
        {
            var tradesAll = source.GetTrades(barNum);
            switch (direction)
            {
                case TradeDirection2.Buys:
                    return tradesAll.Where(x => x.Direction == TradeDirection.Buy);
                case TradeDirection2.Sells:
                    return tradesAll.Where(x => x.Direction == TradeDirection.Sell);
                default:
                    return tradesAll;
            }
        }
    }

    public enum TradeDirection2
    {
        [LocalizeDescription("TradeDirection2.All")] // Все
        All,
        [LocalizeDescription("TradeDirection2.Buys")] // Покупки
        Buys,
        [LocalizeDescription("TradeDirection2.Sells")] // Продажи
        Sells,
    }
}
