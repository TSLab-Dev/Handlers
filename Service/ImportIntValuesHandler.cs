using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace TSLab.Script.Handlers
{
    public sealed class ImportIntValuesHandler : ImportValuesHandler<int>, IIntReturns
    {
        protected override int GetValue(IReadOnlyList<int> values)
        {
            if (values.Count == 0)
                return default(int);

            if (values.Count == 1)
                return values[0];

            return values.Sum() / values.Count;
        }
    }
}
