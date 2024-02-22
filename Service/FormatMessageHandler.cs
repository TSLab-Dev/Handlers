using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using TSLab.Script.Handlers.Options;
using TSLab.Utils;

// ReSharper disable once CheckNamespace
namespace TSLab.Script.Handlers
{
    [HandlerCategory(HandlerCategories.ServiceElements)]
    [HelperName("Format message", Language = Constants.En)]
    [HelperName("Форматированное сообщение", Language = Constants.Ru)]
    [HandlerAlwaysKeep]
    [InputsCount(2, 30)]
    [Input(0, TemplateTypes.SECURITY, Name = "SECURITYSource")]
    [Input(1, TemplateTypes.BOOL)]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.STRING)]
    [Description("При появлении на входе блока значения 'Истина' выводит в лог программы форматированное сообщение.")]
    [HelperDescription("When input becomes TRUE a handler sends user message to a TSLab log.", Constants.En)]
    public sealed class FormatMessageHandler : IValuesHandlerWithNumber, IContextUses, ICustomListValues
    {
        private const string UserMessageTag = "$UserMessageTag";
        private static readonly CultureInfo m_culture = CultureInfo.CurrentCulture;

        public IContext Context { get; set; }

        [HelperName("Message", Constants.En)]
        [HelperName("Сообщение", Constants.Ru)]
        [Description("Сообщение")]
        [HelperDescription("Message", Constants.En)]
        [HandlerParameter(true, @"{DateTime}: {Input3}", NotOptimized = true, Editor = "FormatMessageEditor")]
        public string Message { get; set; }

        [HelperName("Write to log", Constants.En)]
        [HelperName("Выводить в лог", Constants.Ru)]
        [Description("Выводить сообщение в лог")]
        [HelperDescription("Write message to log", Constants.En)]
        [HandlerParameter(true, "true")]
        public bool WriteToLog { get; set; }

        [HelperName("Tag", Constants.En)]
        [HelperName("Метка", Constants.Ru)]
        [Description("Дополнительная пользовательская метка")]
        [HelperDescription("Additional user tag", Constants.En)]
        [HandlerParameter(true, "Tag")]
        public string Tag { get; set; }

        [HelperName("Importance", Constants.En)]
        [HelperName("Важность", Constants.Ru)]
        [Description("Важность сообщения (Info, Warning, Error)")]
        [HelperDescription("Message importance (Info, Warning, Error)", Constants.En)]
        [HandlerParameter(true, "Info")]
        public MessageType Type { get; set; }

        public string Execute(ISecurity sec, bool value, params object[] values)
        {
            if (values.Length < 1)
                return string.Empty;

            int index = Convert.ToInt32(values.Last());
            if (value && index == Context.BarsCount - (Context.IsLastBarUsed ? 1 : 2))
            {
                values = values.Take(values.Length - 1).ToArray();
                var text = ConvertValue(Message, sec, values);
                if (WriteToLog)
                    Log(text);
                return text;
            }

            return string.Empty;
        }

        private string ConvertValue(string text, ISecurity sec, IList<object> values)
        {
            var res = text;
            var lastPos = sec.Positions.GetLastPositionActive(Context.BarsCount);
            res = res.Replace("{DateTime}", ValueToString(DateTime.Now));
            res = res.Replace("{InitDeposit}", ValueToString(sec.InitDeposit));
            res = res.Replace("{Symbol}", ValueToString(sec.Symbol));
            res = res.Replace("{Interval}", ValueToString(sec.IntervalInstance));
            res = res.Replace("{EntryPrice}", ValueToString(lastPos?.EntryPrice));

            for (int i = 0; i < 35; i++)
            {
                // Нумерация та, которая показывается на блоке когда наводишь мышку на пимпочку.
                // То есть 1 и 2 входы заняты, поэтому нумерация начинается с 3.
                var userNumber = i + 3;
                var value = ValueToString(values.ElementAtOrDefault(i));
                res = res.Replace($"{{Input{userNumber}}}", value);
            }
            return res;
        }

        private string ValueToString(object value)
        {
            if (value == null)
                return string.Empty;
            if (value is double d)
                return ((decimal)d).ToString(m_culture);
            return value.ToString();
        }

        private void Log(string message)
        {
            var args = new Dictionary<string, object> { { UserMessageTag, Tag ?? string.Empty } };
            Context.Log(message, Type, true, args);
        }

        public IEnumerable<string> GetValuesForParameter(string paramName)
        {
            if (paramName.EqualsIgnoreCase(nameof(Tag)))
                return new[] { Tag ?? "" };
            return new[] { "" };
        }
    }
}
