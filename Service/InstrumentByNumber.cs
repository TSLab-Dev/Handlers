using System;
using System.ComponentModel;
using TSLab.Script.Handlers.Options;
using TSLab.Utils;

namespace TSLab.Script.Handlers
{
    [HandlerCategory(HandlerCategories.ServiceElements)]
    [HelperName("Instrument by number", Language = Constants.En)]
    [HelperName("Инструмент по нореру", Language = Constants.Ru)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.MULTI_SECURITY, Name = Constants.SecuritySource)]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.SECURITY)]
    [Description("Найти инструмент по номеру в мульти-источнике.")]
    [HelperDescription("Find the instrument by number in the multi-source.", Constants.En)]
    [NotCacheable]
    public class InstrumentByNumber : IStreamHandler, ISecurityReturns
    {
        [HelperName("Number", Constants.En)]
        [HelperName("Номер", Constants.Ru)]
        [HandlerParameter(true, "0")]
        public int Number { get; set; }

        public ISecurity Execute(ISecurity[] securities)
        {
            if (securities.Length > Number)
                return securities[Number];
            throw new Exception(RM.GetString("SecurityNotFound"));
        }
    }
}
