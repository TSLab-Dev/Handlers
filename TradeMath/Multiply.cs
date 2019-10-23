using System;
using System.Collections.Generic;
using System.ComponentModel;
using TSLab.Script.Handlers.Options;

namespace TSLab.Script.Handlers
{
    [HandlerCategory(HandlerCategories.TradeMath)]
    //[HandlerName("Multiply by Coef", Language = "en-US")]
    //[HandlerName("Умножить на число", Language = "ru-RU")]
    [HelperName("Multiply by", Language = Constants.En)]
    [HelperName("Умножить на", Language = Constants.Ru)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.DOUBLE)]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.DOUBLE)]
    [Description("Умножение каждого элемента входной серии на заданный коэффициент.")]
    [HelperDescription("Multiplies each item of input by a constant factor.", Constants.En)]
    public sealed class Multiply : IDouble2DoubleHandler, IValuesHandlerWithNumber
    {
        /// <summary>
        /// \~english Every item of input is multiplied by this coefficient ( Mult*x )
        /// \~russian Каждый элемент входной серии умножается на указанный коэффициент ( Mult*x )
        /// </summary>
        [HelperName("Multiply", Constants.En)]
        [HelperName("Множитель", Constants.Ru)]
        [Description("Каждый элемент входной серии умножается на указанный коэффициент ( Mult*x )")]
        [HelperDescription("Every item of input is multiplied by this coefficient ( Mult*x )", Constants.En)]
        [HandlerParameter(true, "2", Min = "0.5", Max = "5", Step = "0.5")]
        public double Coef
        {
            get;
            set;
        }

        /// <summary>
        /// Обработчик для интерфейса IStreamHandler
        /// </summary>
        public IList<double> Execute(IList<double> bars)
        {
            IList<double> list = new List<double>(bars.Count);
            foreach (double t in bars)
                list.Add(t * Coef);
            return list;
        }

        /// <summary>
        /// Обработчик для интерфейса IValuesHandlerWithNumber
        /// </summary>
        public double Execute(double barVal, int barNum)
        {
            double res = barVal * Coef;
            return res;
        }
    }
}