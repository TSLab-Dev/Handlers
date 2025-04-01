using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

using TSLab.Script.Handlers.Options;
// ReSharper disable CommentTypo
// ReSharper disable ArrangeAccessorOwnerBody
// ReSharper disable StringLiteralTypo

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable once CheckNamespace
namespace TSLab.Script.Handlers
{
    //[HandlerName("Constant")]
    [HandlerCategory(HandlerCategories.TradeMath, "Const", true)]
    [HelperName("Constant", Language = Constants.En)]
    [HelperName("Константа", Language = Constants.Ru)]
    #region Атрибуты с описанием и ссылками
    [Description("Постоянное значение.")]
    [HelperDescription("A constant value.", Constants.En)]
    [HelperLink(@"http://www.tslab.ru/files/script/StochK.xml", "Пример по индикатору Stochastic K", Constants.Ru)]
    [HelperLink(@"http://www.tslab.ru/files/script/StochK.xml", "Example of Stochastic K", Constants.En)]
    [HelperLink(@"http://www.tslab.ru/files/script/2MA_customStop.xml", "Пример стратегии 2МА с нестандартным стопом", Constants.Ru)]
    [HelperLink(@"http://www.tslab.ru/files/script/2MA_customStop.xml", "Example of 2МА (with nonstandard stop)", Constants.En)]
    #endregion Атрибуты с описанием и ссылками
    public sealed class ConstGen : ConstGenBase<double>, IBar2DoubleHandler, IDouble1CalculatorHandler, IDouble2DoubleHandler
    {
        /// <summary>
        /// \~english A value to return as output of a handler
        /// \~russian Значение на выходе блока
        /// </summary>
        [HandlerParameter]
        public double Value { get; set; }

        public IList<double> Execute(IContext context)
        {
            MakeList(context.BarsCount, Value);
            return this;
        }

        public IList<double> Execute(ISecurity source)
        {
            MakeList(source.Bars.Count, Value);
            return this;
        }

        public double Execute(double source1)
        {
            return Value;
        }

        public IList<double> Execute(IList<double> source)
        {
            MakeList(source.Count, Value);
            return this;
        }
    }

    [HandlerCategory(HandlerCategories.TradeMath, "Const", true)]
    public class StringConst : ConstGenBase<string>, IOneSourceHandler, IStringReturns, IStreamHandler, ISecurityInputs, ICustomListValues
    {
        /// <summary>
        /// \~english A value to return as output of a handler
        /// \~russian Значение на выходе блока
        /// </summary>
        [HandlerParameter(Default = "")]
        public string Value { get; set; } = "";

        public IList<string> Execute(IContext context)
        {
            MakeList(context.BarsCount, Value);
            return this;
        }

        public IList<string> Execute(ISecurity source)
        {
            MakeList(source.Bars.Count, Value);
            return this;
        }

        public IEnumerable<string> GetValuesForParameter(string paramName)
        {
            return new[] { Value }; 
        }
    }

    [HandlerCategory(HandlerCategories.TradeMath, "Const", true)]
    public class BoolConst : ConstGenBase<bool>, IBar2BoolsHandler
    {
        /// <summary>
        /// \~english A value to return as output of a handler
        /// \~russian Значение на выходе блока
        /// </summary>
        [HandlerParameter]
        public bool Value { get; set; }

        public IList<bool> Execute(IContext context)
        {
            MakeList(context.BarsCount, Value);
            return this;
        }

        public IList<bool> Execute(ISecurity source)
        {
            MakeList(source.Bars.Count, Value);
            return this;
        }
    }

    // useless test class

    //[HandlerName("Bool Breaker")]
    [HandlerCategory(HandlerCategories.TradeMath, "BoolConst", true)]
    public sealed class BoolBreaker : BoolConst
    {
        public override IEnumerator<bool> GetEnumerator()
        {
            return new BreakerEnumerator(m_count, m_value);
        }

        public override bool this[int index]
        {
            get { return index == m_count - 1 && m_value; }
            set { throw new InvalidOperationException(); }
        }

        private sealed class BreakerEnumerator : Enumerator
        {
            public BreakerEnumerator(int size, bool value)
                : base(size, value)
            {
            }

            public override bool Current
            {
                get { return m_cur >= m_size - 1 && m_value; }
            }
        }
    }
}