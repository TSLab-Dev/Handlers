using System;
using System.Collections.Generic;
using System.ComponentModel;
using TSLab.Script.Handlers.Options;
using TSLab.Script.Optimization;

// ReSharper disable once CheckNamespace
namespace TSLab.Script.Handlers
{
    [HandlerCategory(HandlerCategories.ServiceElements)]
    [HelperName("Control message", Language = Constants.En)]
    [HelperName("Контрольное сообщение", Language = Constants.Ru)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.BOOL)]
    [OutputsCount(0)]
    [Description("Выводит контрольное сообщение")]
    [HelperDescription("Outputs control message", Constants.En)]
    public sealed class ControlMessageHandler : IOneSourceHandler, IBooleanReturns, IStreamHandler, IValuesHandlerWithNumber, IBooleanInputs, IContextUses, ICustomListValues
    {
        public IContext Context { get; set; }

        /// <summary>
        /// \~english True message
        /// \~russian Истинное сообщение
        /// </summary>
        [HelperName("True message", Constants.En)]
        [HelperName("Истинное сообщение", Constants.Ru)]
        [Description("Истинное сообщение")]
        [HelperDescription("True message", Constants.En)]
        [HandlerParameter(true, "")]
        public string TrueMessage { get; set; }

        /// <summary>
        /// \~english False message
        /// \~russian Ложное сообщение
        /// </summary>
        [HelperName("False message", Constants.En)]
        [HelperName("Ложное сообщение", Constants.Ru)]
        [Description("Ложное сообщение")]
        [HelperDescription("False message", Constants.En)]
        [HandlerParameter(true, "")]
        public string FalseMessage { get; set; }

        /// <summary>
        /// \~english Message
        /// \~russian Сообщение
        /// </summary>
        [HelperName("Message", Constants.En)]
        [HelperName("Сообщение", Constants.Ru)]
        [Description("Сообщение")]
        [HelperDescription("Message", Constants.En)]
        [HandlerParameter(true, "", IsCalculable = true)]
        public StringOptimProperty Message { get; set; }

        public void Execute(IList<bool> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            if (values.Count > 0)
                Message.Value = values[values.Count - 1] ? TrueMessage : FalseMessage;
            else
                Message.Value = string.Empty;
        }

        public void Execute(bool value, int number)
        {
            if (number == Context.BarsCount - (Context.IsLastBarUsed ? 1 : 2))
                Message.Value = value ? TrueMessage : FalseMessage;
        }

        public IEnumerable<string> GetValuesForParameter(string paramName)
        {
            yield return TrueMessage;
            yield return FalseMessage;
        }
    }
}
