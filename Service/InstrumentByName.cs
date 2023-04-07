using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TSLab.Script.Handlers.Options;
using TSLab.Utils;

namespace TSLab.Script.Handlers
{
    [HandlerCategory(HandlerCategories.ServiceElements)]
    [HelperName("Instrument by name", Language = Constants.En)]
    [HelperName("Инструмент по имени", Language = Constants.Ru)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.MULTI_SECURITY, Name = Constants.SecuritySource)]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.SECURITY)]
    [Description("Найти инструмент по имени в мульти-источнике. " +
        "Если инструмент не найден по имени, то возвращает первый по списку.")]
    [HelperDescription("Find the instrument by name in the multi-source. " +
        "If the instrument is not found by name, it returns the first one in the list.", Constants.En)]
    [NotCacheable]
    public class InstrumentByName : IStreamHandler, ISecurityReturns, ICustomListValues
    {
        [HelperName("Name", Constants.En)]
        [HelperName("Название", Constants.Ru)]
        [HandlerParameter(true, "")]
        public string Name { get; set; }

        private string[] m_names;

        public ISecurity Execute(ISecurity[] securities)
        {
            m_names = securities.Select(x => x.Symbol).ToArray();
            return securities.FirstOrDefault(x => x.Symbol.ContainsIgnoreCase(Name)) ?? securities.First();
        }
        
        public IEnumerable<string> GetValuesForParameter(string paramName)
        {
            if (paramName.EqualsIgnoreCase(nameof(Name)))
                return m_names ?? new[] { "" };
            return new[] { "" };
        }
    }
}
