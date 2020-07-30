using System.Collections.Generic;
using System.Linq;

namespace TSLab.Script.Handlers
{
    [HandlerCategory(HandlerCategories.TradeMath)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.DOUBLE)]
    [OutputsCount(0)]
    public class ResultForOptimization : IOneSourceHandler, IValuesHandler, IStreamHandler, IDoubleInputs, IDoubleReturns, 
                                         IContextUses, INeedVariableVisual
    {
        public IContext Context { set; get; }
        public string VariableVisual { get; set; }

        public IList<double> Execute(IList<double> values)
        {
            var count = values.Count;

            if (count < 1)
                return new double[0];

            Context.ScriptResults[VariableVisual] = values.Last();
            return values;
        }

        public double Execute(double value, int barNum)
        {
            Context.ScriptResults[VariableVisual] = value;
            return value;
        }
    }
}
