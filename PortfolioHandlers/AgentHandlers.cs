using System.Collections.Generic;
using System.Linq;
using TSLab.Script.Handlers.Options;
using TSLab.Utils;

namespace TSLab.Script.Handlers.PortfolioHandlers
{
    public interface IAgentTradingInfoService
    {
        double GetAgentLots(string symbol);
    }

    [HandlerCategory(HandlerCategories.Portfolio)]
    [HelperName("Agents lots", Language = Constants.En)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.SECURITY, Name = "SECURITYSource")]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.DOUBLE)]
#if !DEBUG
    [HandlerInvisible]
#endif
    public class AgentHandlers : IStreamHandler
    {

        public IList<double> Execute(ISecurity source)
        {
            var value = 0.0;
            var symbol = source?.Symbol;
            if (!string.IsNullOrEmpty(symbol))
            {
                var service = Locator.Current.GetInstance<IAgentTradingInfoService>();
                value = service.GetAgentLots(symbol);
            }
            return Enumerable.Repeat(value, source.Bars.Count).ToList();
        }
    }
}
