﻿using System;
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
    [OutputType(TemplateTypes.BOOL)]
    [NotCacheable]
    public class BoolReceiverHandler : ConstGenBase<bool>, IStreamHandler, INeedVariableVisual, IContextUses
    {
        [HandlerParameter(NotOptimized = false)]
        public bool Value { get; set; }


        [HandlerParameter(true, "false")]
        public bool DefaultValue { get; set; }

        public IList<bool> Execute(IList<string> receiver)
        {
            return Execute(receiver, null);
        }

        public IList<bool> Execute(IList<string> receiver, IList<string> prefix)
        {
            if (receiver.Count == 0)
                return Array.Empty<bool>();

            var v = Convert.ToDouble(DefaultValue);
            try
            {
                var service = Locator.Current.GetInstance<ITSChannelService>();
                var pfx = prefix == null ? "" : prefix[0];
                var name = $"{pfx}{VariableVisual}";
                v = service.GetValue(receiver[0], name, v);
            }
            catch (Exception e)
            {
                Context.Log(e.Message, MessageType.Error);
            }

            var bv = Convert.ToBoolean(v);
            MakeList(receiver.Count, bv);
            Value = bv;
            return this;
        }

        public IContext Context { get; set; }

        public string VariableVisual { get; set; }
    }
}
