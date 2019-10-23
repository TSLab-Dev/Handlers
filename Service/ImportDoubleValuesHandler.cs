using System.Collections.Generic;
using System.Linq;

namespace TSLab.Script.Handlers
{
    public sealed class ImportDoubleValuesHandler : ImportValuesHandler<double>, IDoubleReturns
    {
        protected override double GetValue(IReadOnlyList<double> values)
        {
            if (values.Count == 0)
                return default(double);

            if (values.Count == 1)
                return values[0];

            return values.Sum() / values.Count;
        }
    }
}
