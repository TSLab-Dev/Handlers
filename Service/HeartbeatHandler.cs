using System;
using System.ComponentModel;
using TSLab.Script.Handlers.Options;

namespace TSLab.Script.Handlers
{
    [HandlerCategory(HandlerCategories.ServiceElements)]
    [HelperName("Метроном 2", Language = Constants.Ru)]
    [HelperName("Heartbeat 2", Language = Constants.En)]
    [HandlerAlwaysKeep]
    [OutputsCount(0)]
    [Description("Метроном 2")]
    [HelperDescription("Heartbeat 2", Constants.En)]
    public sealed class HeartbeatHandler : IHandler, IZeroSourceHandler, IContextUses, INeedVariableId, INeedVariableName
    {
        public IContext Context { get; set; }
        public string VariableId { get; set; }
        public string VariableName { get; set; }

        [HelperName("Интервал", Constants.Ru)]
        [HelperName("Interval", Constants.En)]
        [Description("Интервал")]
        [HelperDescription("Interval", Language = Constants.En)]
        [HandlerParameter(true, "0:1:0", Min = "0:0:0", Max = "1.0:0:0", Step = "0:0:1", EditorMin = "0:0:0", EditorMax = "1.0:0:0")]
        public TimeSpan Interval { get; set; }

        public void Execute()
        {
            if (!Context.IsOptimization)
                HeartbeatManager.Instance.Register(this);
        }
    }
}
