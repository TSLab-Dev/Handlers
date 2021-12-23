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
    public class ChannelHandler : ConstGenBase<string>, IStreamHandler, ICustomListValues, IContextUses
    {
        [HandlerParameter]
        public string ChannelApiKey { get; set; }

        public IList<string> Execute(ISecurity sec)
        {
            var channelApiKey = ChannelApiKey;
            MakeList(sec.Bars.Count, channelApiKey);

            if (Context.IsOptimization)
                return this;
            try
            {
                var service = Locator.Current.GetInstance<ITSChannelService>();
                service.RegisterChannelForUpdates(sec, channelApiKey);
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
            return new[] { ChannelApiKey };
        }
    }
}
