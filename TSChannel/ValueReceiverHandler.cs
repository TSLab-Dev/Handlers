using System;
using System.Collections.Generic;
using TSLab.Script.Handlers.Options;
using TSLab.Utils;

namespace TSLab.Script.Handlers.TSChannel
{
    [HandlerCategory(HandlerCategories.TSChannel)]
    [InputsCount(1, 2)]
    [Input(0, TemplateTypes.STRING, Name = Constants.SecuritySource)]
    [Input(1, TemplateTypes.STRING, Name = "Prefix")]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.DOUBLE)]
    [NotCacheable]
    public class ValueReceiverHandler : ConstGenBase<double>, IStreamHandler, INeedVariableVisual, IContextUses
    {
        [HandlerParameter(NotOptimized = false)]
        public double Value { get; set; }


        [HandlerParameter(true, "0")]
        public double DefaultValue { get; set; }

        public IList<double> Execute(IList<string> receiver)
        {
            return Execute(receiver, null);
        }

        public IList<double> Execute(IList<string> receiver, IList<string> prefix)
        {
            if (receiver.Count == 0)
                return Array.Empty<double>();


            var service = Locator.Current.GetInstance<ITSChannelService>();
            double v = DefaultValue;
            try
            {
                var pfx = prefix == null ? "" : prefix[0];
                var name = $"{pfx}{VariableVisual}";
                v = service.GetValue(receiver[0], name, DefaultValue);
            }
            catch (Exception e)
            {
                Context.Log(e.Message, MessageType.Error);
            }

            MakeList(receiver.Count, v);
            Value = v;
            return this;
        }

        public IContext Context { get; set; }

        public string VariableVisual { get; set; }
    }
}
