using System;
using System.Collections.Generic;
using System.ComponentModel;
using TSLab.Script.Handlers.Options;

namespace TSLab.Script.Handlers
{
    [HandlerCategory(HandlerCategories.TradeMath)]
    [HelperName("Ln", Language = Constants.En)]
    [HelperName("Ln", Language = Constants.Ru)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.DOUBLE)]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.DOUBLE)]
    [Description("Натуральный логарифм (Ln) для серии значений.")]
    [HelperDescription("A natural logarithm (Ln) for a values series.", Constants.En)]
    public sealed class Ln : IDouble2DoubleHandler, IValuesHandlerWithNumber
    {
        private double m_mult = 1, m_add = 0;

        /// <summary>
        /// \~english A result of logarithm may be multiplied by this coefficient ( Mult*LN(x) + Add )
        /// \~russian Результат логарифмирования можно сразу умножить на этот коэффициент ( Mult*LN(x) + Add )
        /// </summary>
        [HelperName("Multiply", Constants.En)]
        [HelperName("Множитель", Constants.Ru)]
        [Description("Результат логарифмирования можно сразу умножить на этот коэффициент ( Mult*LN(x) + Add )")]
        [HelperDescription("A result of logarithm may be multiplied by this coefficient ( Mult*LN(x) + Add )", Constants.En)]
        [HandlerParameter(true, Default = "1")]
        public double Mult
        {
            get { return m_mult; }
            set { m_mult = value; }
        }

        /// <summary>
        /// \~english A result of logarithm (after multiplication) may be shifted by this value ( Mult*LN(x) + Add )
        /// \~russian Результат логарифмирования (после домножения) можно увеличить на этот сдвиг ( Mult*LN(x) + Add )
        /// </summary>
        [HelperName("Add", Constants.En)]
        [HelperName("Прибавить", Constants.Ru)]
        [Description("Результат логарифмирования (после домножения) можно увеличить на этот сдвиг ( Mult*LN(x) + Add )")]
        [HelperDescription("A result of logarithm (after multiplication) may be shifted by this value ( Mult*LN(x) + Add )", Constants.En)]
        [HandlerParameter(true, Default = "0")]
        public double Add
        {
            get { return m_add; }
            set { m_add = value; }
        }

        /// <summary>
        /// Обработчик для интерфейса IStreamHandler
        /// </summary>
        public IList<double> Execute(IList<double> bars)
        {
            IList<double> list = new List<double>(bars.Count);
            for (int j = 0; j < bars.Count; j++)
            {
                double res = m_mult * Math.Log(bars[j]) + m_add;
                list.Add(res);
            }
            return list;
        }

        /// <summary>
        /// Обработчик для интерфейса IValuesHandlerWithNumber
        /// </summary>
        public double Execute(double barVal, int barNum)
        {
            double res = m_mult * Math.Log(barVal) + m_add;
            return res;
        }
    }
}