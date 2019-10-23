using System.Collections.Generic;
using System.Linq;

namespace TSLab.Script.Handlers
{
    public sealed class ImportBoolValuesHandler : ImportValuesHandler<bool>, IBooleanReturns
    {
        protected override bool GetValue(IReadOnlyList<bool> values)
        {
            if (values.Count == 0)
                return default(bool);

            if (values.Count == 1)
                return values[0];

            var trueCount = values.Count(item => item);
            return trueCount >= values.Count - trueCount;
        }
    }
}
