using System;
using System.Collections.Generic;
using TSLab.Script.Handlers.Options;
using TSLab.Utils;

namespace TSLab.Script.Handlers.TSChannel
{
    [HandlerCategory(HandlerCategories.TSChannel)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.SECURITY, Name = Constants.SecuritySource)]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.STRING)]
    public class ReceiverHandler : ConstGenBase<string>, IStreamHandler, IContextUses, ICustomListValues
    {
        [HandlerParameter]
        public string ReceiverApiKey { get; set; }

        public IList<string> Execute(ISecurity sec)
        {
            MakeList(sec.Bars.Count, ReceiverApiKey);

            if (Context.IsOptimization)
                return this;
            
            var service = Locator.Current.GetInstance<ITSChannelService>();
            try
            {
                service.RegisterReceiverForUpdates(ReceiverApiKey, () => Context.Recalc("TSChannel", sec.SecurityDescription));
            }
            catch (Exception e)
            {
                Context.Log(e.Message, MessageType.Error);
            }

            return this;
        }

        public IContext Context { get; set; }

        public IEnumerable<string> GetValuesForParameter(string paramName)
        {
            return new[] { ReceiverApiKey };
        }
    }
}
