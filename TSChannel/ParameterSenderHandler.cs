using System;
using TSLab.Script.Handlers.Options;
using TSLab.Utils;

namespace TSLab.Script.Handlers.TSChannel
{
    [HandlerCategory(HandlerCategories.TSChannel)]
    [InputsCount(1, 2)]
    [Input(0, TemplateTypes.STRING, Name = Constants.SecuritySource)]
    [Input(1, TemplateTypes.STRING, Name = "Prefix")]
    [OutputsCount(0)]
    public class ParameterSenderHandler : IContextUses, INeedVariableVisual, IValuesHandlerWithNumber
    {
        /// <summary>
        /// \~english A value to return as output of a handler
        /// \~russian Значение на выходе блока
        /// </summary>
        [HandlerParameter(NotOptimized = false)]
        public double Value { get; set; }

        public void Execute(string apiKey, int bar)
        {
            Execute(apiKey, "", bar);
        }

        public void Execute(string apiKey, string prefix, int bar)
        {
            if (Context.IsOptimization)
                return;
            var barsCount = Context.BarsCount;
            if (!Context.IsLastBarUsed)
                barsCount--;
            if (bar != barsCount - 1)
                return;
            var service = Locator.Current.GetInstance<ITSChannelService>();
            service.SetValue(apiKey, $"{prefix}{VariableVisual}", Value);
        }

        public IContext Context { get; set; }

        public string VariableVisual { get; set; }

    }
}
