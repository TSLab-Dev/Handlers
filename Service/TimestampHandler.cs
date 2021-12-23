using System;
using System.Collections.Generic;
using TSLab.Utils;

// ReSharper disable once CheckNamespace
namespace TSLab.Script.Handlers
{
    [HandlerCategory(HandlerCategories.ServiceElements)]
    public sealed class TimestampHandler : IBar2DoubleHandler, IContextUses
    {
        public IContext Context { get; set; }

        public IList<double> Execute(ISecurity source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var bars = source.Bars;
            var barsCount = bars.Count;

            if (barsCount == 0)
                return EmptyArrays.Double;

            var results = Context.GetArray<double>(barsCount);
            for (var i = 0; i < barsCount; i++)
                results[i] = new DateTimeOffset(bars[i].Date).ToUnixTimeMilliseconds();

            return results;
        }
    }
}