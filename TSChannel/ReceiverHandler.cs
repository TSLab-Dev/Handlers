using System;
using System.Collections.Generic;
using System.Xml.Linq;
using TSLab.Script.Handlers.Options;
using TSLab.Utils;

namespace TSLab.Script.Handlers.TSChannel
{
    [HandlerCategory(HandlerCategories.TSChannel)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.SECURITY, Name = Constants.SecuritySource)]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.STRING)]
    public class ReceiverHandler : ConstGenBase<string>, IStreamHandler, IContextUses, ICustomListValues, INeedVariableVisual
    {
        [ApiKeyHandlerProperty(ApiKeyHandlerPropertyMode.Receiver)]
        [HandlerParameter(NotOptimized = false, IsTransparentSetValue = true, Editor = "ReceiverApiKeyEditor")]
        public string ReceiverApiKey { get; set; }

        [ApiKeyHandlerProperty(ApiKeyHandlerPropertyMode.Channel)]
        [HandlerParameter(NotOptimized = false, Editor = "ChannelApiKeyEditor")]
        public string ChannelApiKey { get; set; }

        public IList<string> Execute(ISecurity sec)
        {
            var receiverApiKey = ReceiverApiKey;
            MakeList(sec.Bars.Count, receiverApiKey);

            if (Context.IsOptimization)
                return this;
            if (string.IsNullOrWhiteSpace(receiverApiKey))
            {
                Context.Log($"Receiver ApiKey'{VariableVisual}' doesn't set.", MessageType.Error);
                return this;
            }

            var service = Locator.Current.GetInstance<ITSChannelService>();
            try
            {
                var dsName = sec.SecurityDescription.DSName;
                Context.StoreObject(receiverApiKey, dsName);
                service.RegisterReceiverForUpdates(receiverApiKey, ChannelApiKey, dsName,
                    () => Context.Recalc("TSChannel", sec.SecurityDescription));
            }
            catch (Exception e)
            {
                Context.Log(e.Message, MessageType.Error);
                //MakeList(sec.Bars.Count, receiverApiKey);
            }

            return this;
        }

        public IContext Context { get; set; }

        public IEnumerable<string> GetValuesForParameter(string paramName)
        {
            if (paramName.EqualsIgnoreCase(nameof(ReceiverApiKey)))
                return new[] { ReceiverApiKey };
            if (paramName.EqualsIgnoreCase(nameof(ChannelApiKey)))
                return new[] { ChannelApiKey };
            return EmptyArrays.String;
        }

        public string VariableVisual { get; set; }
    }
}
