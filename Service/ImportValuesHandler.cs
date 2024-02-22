using System;
using System.Collections.Generic;
using TSLab.DataSource;
using TSLab.Utils;

// ReSharper disable once CheckNamespace
namespace TSLab.Script.Handlers
{
    [HandlerCategory(HandlerCategories.ServiceElements)]
    [InputsCount(1)]
    public abstract class ImportValuesHandler<T> : IOneSourceHandler, ISecurityInputs, IStreamHandler, IContextUses
    {
        public IContext Context { get; set; }

        [HandlerParameter(true, "")]
        public string Id { get; set; }

        public IList<T> Execute(ISecurity security)
        {
            var bars = security.Bars;
            var barsCount = bars.Count;
            var values = new T[barsCount];

            if (barsCount == 0 || !(Context.LoadGlobalObject<NotClearableContainer>(Id, null)?.Content is Tuple<IReadOnlyList<DateTime>, IList<T>> context))
                return values;

            var importedDateTimes = context.Item1;
            var importedValues = context.Item2;
            var importedCount = Math.Min(importedDateTimes.Count, importedValues.Count);

            if (importedCount == 0)
                return values;

            int index = 0, importedIndex = 0;
            var importedDateTime = importedDateTimes[0];

            while (index < barsCount && bars[index].Date < importedDateTime)
                values[index++] = default(T);

            var currentValues = new List<T>();
            while (index < barsCount && importedIndex < importedCount)
            {
                importedDateTime = importedDateTimes[importedIndex];
                var barDate = bars[index].Date;

                if (importedDateTime <= barDate)
                    currentValues.Add(importedValues[importedIndex++]);
                else
                {
                    var value1 = GetValue(currentValues);
                    while (index < barsCount && bars[index].Date < importedDateTime)
                        values[index++] = value1;

                    currentValues.Clear();
                }
            }
            var value2 = GetValue(currentValues);
            while (index < barsCount)
                values[index++] = value2;

            return values;
        }

        protected abstract T GetValue(IReadOnlyList<T> values);

        public IEnumerable<string> GetValuesForParameter(string paramName)
        {
            if (paramName.EqualsIgnoreCase(nameof(Id)))
                return new[] { Id ?? "" };
            return new[] { "" };
        }
    }
}
