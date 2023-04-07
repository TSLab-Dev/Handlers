using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TSLab.DataSource;
using TSLab.Script.Handlers.Options;

// ReSharper disable once CheckNamespace
namespace TSLab.Script.Handlers
{
    [HandlerCategory(HandlerCategories.TradeMath)]
    [HelperName("Bars Constructor", Language = Constants.En)]
    [HelperName("Конструктор баров", Language = Constants.Ru)]
    [InputsCount(5)]
    [Input(0, TemplateTypes.DOUBLE, Name = "Open")]
    [Input(1, TemplateTypes.DOUBLE, Name = "Close")]
    [Input(2, TemplateTypes.DOUBLE, Name = "High")]
    [Input(3, TemplateTypes.DOUBLE, Name = "Low")]
    [Input(4, TemplateTypes.DOUBLE, Name = "Volume")]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.SECURITY)]
    [Description("Кубик преобразует 5 числовых серий на входе в синтетический инструмент с барами. Порядок входов: открытие, закрытие, максимум, минимум, объем.")]
    [HelperDescription("Handler converts 5 input numeric series to a synthetic security with bars. Inputs are: open, close, high, low, volume.", Constants.En)]
    public sealed class BarsConstructorHandler : BarsConstructorBase, IFiveSourcesHandler, ISecurityReturns, IStreamHandler, IDoubleInputs, IContextUses, INeedVariableName
    {
        public IContext Context { get; set; }
        public string VariableName { get; set; }

        public ISecurity Execute(IList<double> openList, IList<double> closeList, IList<double> highList, IList<double> lowList, IList<double> volumeList)
        {
            if (openList == null)
                throw new ArgumentNullException(nameof(openList));

            if (closeList == null)
                throw new ArgumentNullException(nameof(closeList));

            if (highList == null)
                throw new ArgumentNullException(nameof(highList));

            if (lowList == null)
                throw new ArgumentNullException(nameof(lowList));

            if (volumeList == null)
                throw new ArgumentNullException(nameof(volumeList));

            var security = Context.Runtime.Securities.First();
            var securityBars = security.Bars;
            var count = Math.Min(openList.Count, Math.Min(closeList.Count, Math.Min(highList.Count, Math.Min(lowList.Count, Math.Min(volumeList.Count, securityBars.Count)))));
            var bars = new IDataBar[count];

            for (var i = 0; i < count; i++)
                bars[i] = new DataBar(securityBars[i].Date, openList[i], highList[i], lowList[i], closeList[i], volumeList[i]);

            return new Security(bars, VariableName, security);
        }
    }
}
