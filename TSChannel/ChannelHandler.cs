using System;
using System.Collections.Generic;
using System.Linq;
using TSLab.Script.Handlers.Options;
using TSLab.Utils;

namespace TSLab.Script.Handlers.TSChannel
{
    [HandlerCategory(HandlerCategories.TSChannel)]
    [InputsCount(1,2)]
    [Input(0, TemplateTypes.SECURITY, Name = Constants.SecuritySource)]
    [Input(1, TemplateTypes.BOOL, Name = "On/Off")]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.STRING)]
    public class ChannelHandler : ConstGenBase<string>, IStreamHandler, ICustomListValues, IContextUses, INeedVariableVisual
    {
        [ApiKeyHandlerProperty(ApiKeyHandlerPropertyMode.Transmitter)]
        [HandlerParameter(NotOptimized = false, Editor = "TransmitterApiKeyEditor")]
        public string ChannelApiKey { get; set; }

        public IList<string> Execute(ISecurity sec)
        {
            return ExecuteInternal(sec, true);
        }

        public IList<string> Execute(ISecurity sec, IList<bool> enabled)
        {
            var isEnabled = enabled.LastOrDefault();
            return ExecuteInternal(sec, isEnabled);
        }

        private IList<string> ExecuteInternal(ISecurity sec, bool isEnabled)
        {
            var channelApiKey = isEnabled ? ChannelApiKey : ""; // Making empty key to skip values
            MakeList(sec.Bars.Count, channelApiKey);

            if (Context.IsOptimization)
                return this;

            if (string.IsNullOrWhiteSpace(channelApiKey))
            {
                if (isEnabled)
                    Context.Log($"Transmitter ApiKey'{VariableVisual}' doesn't set.", MessageType.Error);
                return this;
            }

            try
            {
                var service = Locator.Current.GetInstance<ITSChannelService>();
                service.RegisterTransmitterForUpdates(sec, channelApiKey);
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

        public string VariableVisual { get; set; }
    }
}
