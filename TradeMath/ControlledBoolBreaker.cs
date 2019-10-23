using System;
using System.Collections.Generic;
using System.Linq;
using TSLab.Script.Handlers.Options;
using TSLab.Script.Optimization;
using TSLab.Utils;

namespace TSLab.Script.Handlers.TradeMath
{
    [HandlerCategory(HandlerCategories.TradeMath)]
    [HelperName("Controlled Boolean Breaker", Language = Constants.En)]
    [HelperName("Управляемый логический разделитель", Language = Constants.Ru)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.BOOL)]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.BOOL)]
    public sealed class ControlledBoolBreaker : IOneSourceHandler, IBooleanReturns, IStreamHandler, IValuesHandler, IBooleanInputs, IContextUses
    {
        public IContext Context { get; set; }

        /// <summary>
        /// \~english A value to return as output of a handler
        /// \~russian Значение на выходе блока
        /// </summary>
        [HelperName("Value", Constants.En)]
        [HelperName("Значение", Constants.Ru)]
        [HandlerParameter]
        public BoolOptimProperty Value { get; set; }

        public IList<bool> Execute(IList<bool> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (source.Count == 0)
                return EmptyArrays.Bool;

            if (source.Last())
                Value.Value = false;

            return new ConstList<bool>(source.Count, Value.Value);
        }

        public bool Execute(bool value, int index)
        {
            if (index != Context.BarsCount - (Context.IsLastBarUsed ? 1 : 2))
                return false;

            if (value)
                Value.Value = false;

            return Value.Value;
        }
    }
}
