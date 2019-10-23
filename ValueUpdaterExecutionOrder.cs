using TSLab.Utils;

namespace TSLab.Script.Handlers
{
    public enum ValueUpdaterExecutionOrder
    {
        [LocalizeDescription("ValueUpdaterExecutionOrder.Common")]
        Common,
        [LocalizeDescription("ValueUpdaterExecutionOrder.AtTheEnd")]
        AtTheEnd,
        [LocalizeDescription("ValueUpdaterExecutionOrder.AfterExit")]
        AfterExit,
    }
}
