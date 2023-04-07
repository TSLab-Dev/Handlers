using System;
using System.Collections.Generic;
using TSLab.Script.Handlers.Options;
using TSLab.Utils;

namespace TSLab.Script.Handlers.TSChannel
{
    [HandlerCategory(HandlerCategories.TSChannel)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.STRING, Name = Constants.SecuritySource)]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.BOOL)]
    [NotCacheable]
    public class IsReceiverOnline : ConstGenBase<bool>, IStreamHandler, IContextUses
    {
        public IList<bool> Execute(IList<string> receiver)
        {
            if (receiver.Count == 0)
                return Array.Empty<bool>();

            var receiverApiKey = receiver[0];
            var dsName = (string)Context.LoadObject(receiverApiKey);
            var service = Locator.Current.GetInstance<ITSChannelService>();
            var v = service.IsReceiverReady(receiverApiKey, dsName);
            MakeList(receiver.Count, v);
            return this;
        }

        public IContext Context { get; set; }
    }
}
