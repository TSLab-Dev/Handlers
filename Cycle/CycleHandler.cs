using System;

namespace TSLab.Script.Handlers
{
    [HandlerInvisible]
    [OutputType(TemplateTypes.INT)]
    public sealed class CycleHandler : IHandler
    {
        [HandlerParameter(true, "1", Min = "1", Max = "2147483647", Step = "1", EditorMin = "1", EditorMax = "2147483647")]
        public int MaxCount { get; set; }

        public int GetCount(double value)
        {
            if (double.IsNaN(value))
                return 0;

            if (value <= int.MinValue)
                return int.MinValue;

            if (value >= int.MaxValue)
                return int.MaxValue;

            return GetCount((int)Math.Round(value, MidpointRounding.AwayFromZero));
        }

        public int GetCount(int value)
        {
            return Math.Min(MaxCount, value);
        }
    }
}
