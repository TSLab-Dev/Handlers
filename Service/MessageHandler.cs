using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TSLab.Script.Handlers.Options;

// ReSharper disable once CheckNamespace
namespace TSLab.Script.Handlers
{
    [HandlerCategory(HandlerCategories.ServiceElements)]
    [HelperName("Message", Language = Constants.En)]
    [HelperName("Сообщение", Language = Constants.Ru)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.BOOL)]
    [OutputsCount(0)]
    [Description("При появлении на входе блока значения 'Истина' выводит в лог программы пользовательское сообщение.")]
    [HelperDescription("When input becomes TRUE a handler sends user message to a TSLab log.", Constants.En)]
    public sealed class MessageHandler : IOneSourceHandler, IBooleanReturns, IStreamHandler, IValuesHandlerWithNumber, IBooleanInputs, IContextUses
    {
        private const string UserMessageTag = "$UserMessageTag";
        private int m_barIndex = -1;
        private int m_cycleIndex;

        public IContext Context { get; set; }

        /// <summary>
        /// \~english Message
        /// \~russian Сообщение
        /// </summary>
        [HelperName("Message", Constants.En)]
        [HelperName("Сообщение", Constants.Ru)]
        [Description("Сообщение")]
        [HelperDescription("Message", Constants.En)]
        [HandlerParameter(true, "", NotOptimized = true)]
        public string Message { get; set; }

        /// <summary>
        /// \~english Additional user tag
        /// \~russian Дополнительная пользовательская метка
        /// </summary>
        [HelperName("Tag", Constants.En)]
        [HelperName("Метка", Constants.Ru)]
        [Description("Дополнительная пользовательская метка")]
        [HelperDescription("Additional user tag", Constants.En)]
        [HandlerParameter(true, "Tag", NotOptimized = true)]
        public string Tag { get; set; }

        /// <summary>
        /// \~english Message importance (Info, Warning, Error)
        /// \~russian Важность сообщения (Info, Warning, Error)
        /// </summary>
        [HelperName("Importance", Constants.En)]
        [HelperName("Важность", Constants.Ru)]
        [Description("Важность сообщения (Info, Warning, Error)")]
        [HelperDescription("Message importance (Info, Warning, Error)", Constants.En)]
        [HandlerParameter(true, "Info", NotOptimized = true)]
        public MessageType Type { get; set; }

        public bool IsInCycle { get; set; }

        public void Execute(IList<bool> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            if (values.LastOrDefault())
                Log(Message);
        }

        public void Execute(bool value, int barIndex)
        {
            if (IsInCycle)
            {
                if (m_barIndex != barIndex)
                {
                    m_barIndex = barIndex;
                    m_cycleIndex = 0;
                }
                else
                    m_cycleIndex++;
            }
            if (value && barIndex == Context.BarsCount - (Context.IsLastBarUsed ? 1 : 2))
                Log(IsInCycle ? $"{Message} ({m_cycleIndex})" : Message);
        }

        public void Execute(IReadOnlyList<IReadOnlyList<bool>> listOfLists)
        {
            if (listOfLists == null)
                throw new ArgumentNullException(nameof(listOfLists));

            if (listOfLists.Count == 0)
                return;

            var lastList = listOfLists.Last();
            var indexes = new List<int>(lastList.Count);

            for (var i = 0; i < lastList.Count; i++)
                if (lastList[i])
                    indexes.Add(i);

            if (indexes.Count > 0)
                Log($"{Message} ({string.Join(", ", indexes)})");
        }

        private void Log(string message)
        {
            var args = new Dictionary<string, object> { { UserMessageTag, Tag ?? string.Empty } };
            Context.Log(message, Type, true, args);
        }
    }
}
