namespace TSLab.Script.Handlers
{
    [HandlerInvisible]
    public abstract class CycleResultHandler : IHandler
    {
        [HandlerParameter(true, "0", Min = "0", Max = "2147483647", Step = "1", EditorMin = "0", EditorMax = "2147483647")]
        public int Index { get; set; }

        [HandlerParameter(true, "true")]
        public bool UseLastIndex { get; set; }
    }

    [OutputType(TemplateTypes.BOOL)]
    public sealed class CycleBoolResultHandler : CycleResultHandler
    {
    }

    [OutputType(TemplateTypes.DOUBLE)]
    public sealed class CycleDoubleResultHandler : CycleResultHandler
    {
    }
}
