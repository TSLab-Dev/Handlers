namespace TSLab.Script.Handlers
{
    [HandlerInvisible]
    [OutputType(TemplateTypes.DOUBLE)]
    public sealed class CycleValueUpdaterHandler : IHandler
    {
        [HandlerParameter(false, "0", NotOptimized = true)]
        public double StartFrom { get; set; }

        [HandlerParameter(false, nameof(ValueUpdaterExecutionOrder.AtTheEnd), NotOptimized = true)]
        public ValueUpdaterExecutionOrder ExecutionOrder { get; set; }
    }
}
