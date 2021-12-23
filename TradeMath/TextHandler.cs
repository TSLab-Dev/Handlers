using System;
using System.Collections.Generic;
using System.ComponentModel;
using TSLab.Script.Handlers.Options;

namespace TSLab.Script.Handlers
{
    [HandlerCategory(HandlerCategories.TradeMath)]
    [HelperName("Text", Language = Constants.En)]
    [HelperName("Текст", Language = Constants.Ru)]
    [InputsCount(0)]
    [Description("Блок без входов. Содержит редактируемый строковый параметр, который будет возвращаться из блока в качестве результата его работы.")]
    [HelperDescription("This block has no entries. It has an editable text parameter which returns as a result of its work.", Constants.En)]
    public sealed class TextHandler : ConstGenBase<string>, IStreamHandler, IStringReturns, ICustomListValues, IContextUses
    {
        public IContext Context { get; set; }

        /// <summary>
        /// \~english Text (string)
        /// \~russian Текст (строка)
        /// </summary>
        [HelperName("Text", Constants.En)]
        [HelperName("Текст", Constants.Ru)]
        [Description("Текст (строка)")]
        [HelperDescription("Text (string)", Constants.En)]
        [HandlerParameter(Default = "*")]
        public string Text { get; set; }

        public IList<string> Execute()
        {
            MakeList(Context.BarsCount, Text);
            return this;
        }

        public IEnumerable<string> GetValuesForParameter(string paramName)
        {
            return new[] { Text };
        }
    }
}
