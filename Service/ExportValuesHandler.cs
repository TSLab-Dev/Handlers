using System;
using System.Collections.Generic;
using TSLab.DataSource;
using TSLab.Utils;

// ReSharper disable once CheckNamespace
namespace TSLab.Script.Handlers
{
    [HandlerCategory(HandlerCategories.ServiceElements)]
    [OutputsCount(0)]
    public sealed class ExportValuesHandler : ITwoSourcesHandler, ISecurityInput0, IBooleanInput1, IDoubleInput1, IIntInput1, IStreamHandler, IValuesHandlerWithNumber, IContextUses
    {
        private bool[] m_boolValues;
        private double[] m_doubleValues;
        private int[] m_intValues;

        public IContext Context { get; set; }

        [HandlerParameter(true, "")]
        public string Id { get; set; }

        public void Execute(ISecurity security, IList<bool> values)
        {
            Export(security, values);
        }

        public void Execute(ISecurity security, IList<double> values)
        {
            Export(security, values);
        }

        public void Execute(ISecurity security, IList<int> values)
        {
            Export(security, values);
        }

        public void Execute(ISecurity security, bool value, int index)
        {
            Execute(security, value, index, ref m_boolValues);
        }

        public void Execute(ISecurity security, double value, int index)
        {
            Execute(security, value, index, ref m_doubleValues);
        }

        public void Execute(ISecurity security, int value, int index)
        {
            Execute(security, value, index, ref m_intValues);
        }

        private void Execute<T>(ISecurity security, T value, int index, ref T[] values)
        {
            var barsCount = security.Bars.Count;
            (values ?? (values = new T[barsCount]))[index] = value;

            if (index == barsCount - 1)
                Export(security, values);
        }

        private void Export<T>(ISecurity security, IList<T> values)
        {
            if (Context.IsOptimization)
                return;

            var bars = security.Bars;
            var count = Math.Min(bars.Count, values.Count);
            var dateTimes = new DateTime[count];

            for (var i = 0; i < count; i++)
                dateTimes[i] = bars[i].Date;

            Context.StoreGlobalObject(Id, new NotClearableContainer(new Tuple<IReadOnlyList<DateTime>, IList<T>>(dateTimes, values)));
        }

        public IEnumerable<string> GetValuesForParameter(string paramName)
        {
            if (paramName.EqualsIgnoreCase(nameof(Id)))
                return new[] { Id ?? "" };
            return new[] { "" };
        }
    }
}
