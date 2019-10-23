using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TSLab.Script.Handlers.Options;
using TSLab.Utils;

namespace TSLab.Script.Handlers
{
    public enum EventKind
    {
        [LocalizeDescription(nameof(EventKind) + "." + nameof(None))]
        None,
        [LocalizeDescription(nameof(EventKind) + "." + nameof(OrderRejected))]
        OrderRejected,
        [LocalizeDescription(nameof(EventKind) + "." + nameof(OrderFilled))]
        OrderFilled,
        [LocalizeDescription(nameof(EventKind) + "." + nameof(PositionOpening))]
        PositionOpening,
        [LocalizeDescription(nameof(EventKind) + "." + nameof(PositionClosing))]
        PositionClosing,
        [LocalizeDescription(nameof(EventKind) + "." + nameof(OrderQtyChanged))]
        OrderQtyChanged,
        [LocalizeDescription(nameof(EventKind) + "." + nameof(TradingIsStarted))]
        TradingIsStarted,
        [LocalizeDescription(nameof(EventKind) + "." + nameof(TradingIsStopped))]
        TradingIsStopped,
        [LocalizeDescription(nameof(EventKind) + "." + nameof(OrderCanceled))]
        OrderCanceled,
        [LocalizeDescription(nameof(EventKind) + "." + nameof(PretradeLimitation))]
        PretradeLimitation,
    }

    [HandlerCategory(HandlerCategories.ServiceElements)]
    [HelperName("Event", Language = Constants.En)]
    [HelperName("Событие", Language = Constants.Ru)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.SECURITY, Name = Constants.SecuritySource)]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.BOOL)]
    [Description("Событие")]
    [HelperDescription("Event", Constants.En)]
    public sealed class EventKindHandler : IOneSourceHandler, IStreamHandler, ISecurityInputs, IContextUses
    {
        private static readonly IReadOnlyDictionary<EventKind, string> s_eventKindToNameMap = ((EventKind[])Enum.GetValues(typeof(EventKind))).ToDictionary(item => item, item => item.ToString());

        public IContext Context { get; set; }

        /// <summary>
        /// \~english Event kind
        /// \~russian Вид события
        /// </summary>
        [HelperName("Event kind", Constants.En)]
        [HelperName("Вид события", Constants.Ru)]
        [Description("Вид события")]
        [HelperDescription("Event kind", Constants.En)]
        [HandlerParameter(true, nameof(EventKind.None))]
        public EventKind EventKind { get; set; }

        public IList<bool> Execute(ISecurity source)
        {
            var strEventKind = s_eventKindToNameMap[EventKind];
            var sourceSecurityDescriptionId = source.SecurityDescription.Id;
            var result = Context.Runtime.LastRecalcReasons.Any(item => item.Name == strEventKind && item.DataSourceSecurity.Id == sourceSecurityDescriptionId);
            return new ConstList<bool>(0, result);
        }
    }
}
