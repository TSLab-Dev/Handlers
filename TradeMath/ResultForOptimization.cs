using System;
using System.Collections.Generic;
using System.Linq;

namespace TSLab.Script.Handlers
{
    [HandlerCategory(HandlerCategories.TradeMath)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.DOUBLE)]
    [OutputsCount(0)]
    public class ResultForOptimization : IOneSourceHandler, IValuesHandler, IStreamHandler, IDoubleInputs, IDoubleReturns, IContextUses
    {
        public IContext Context { set; get; }

        public IList<double> Execute(IList<double> values)
        {
            var count = values.Count;

            if (count < 1)
                return new double[0];

            Context.ScriptResult = values.Last();
            return values;
        }

        public double Execute(double value, int barNum)
        {
            Context.ScriptResult = value;
            return value;
        }
    }
}
