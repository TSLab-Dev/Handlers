using System;
using TSLab.Script.Handlers.Options;
using TSLab.Utils;

namespace TSLab.Script.Handlers.TSChannel
{
    [HandlerCategory(HandlerCategories.TSChannel)]
    [InputsCount(2, 3)]
    [Input(0, TemplateTypes.STRING, Name = Constants.SecuritySource)]
    [Input(1, TemplateTypes.DOUBLE | TemplateTypes.INT | TemplateTypes.BOOL, Name = "Value")]
    [Input(2, TemplateTypes.STRING, Name = "Prefix")]
    [OutputsCount(0)]
    public class ValueSenderHandler : ITwoSourcesHandler, IContextUses, INeedVariableVisual, IValuesHandlerWithNumber
    {
        public void Execute(string apiKey, bool v, int bar)
        {
            Execute(apiKey, v, "", bar);
        }

        public void Execute(string apiKey, bool v, string prefix, int bar)
        {
            Execute(apiKey, Convert.ToDouble(v), prefix, bar);
        }

        public void Execute(string apiKey, double v, int bar)
        {
            Execute(apiKey, v, "", bar);
        }

        public void Execute(string apiKey, double v, string prefix, int bar)
        {
            if (Context.IsOptimization)
                return;

            var barsCount = Context.BarsCount;
            if (!Context.IsLastBarUsed)
                barsCount--;
            if (bar != barsCount - 1)
                return;
            var service = Locator.Current.GetInstance<ITSChannelService>();
            service.SetValue(apiKey, $"{prefix}{VariableVisual}", v);
        }

        public IContext Context { get; set; }

        public string VariableVisual { get; set; }
    }
}
