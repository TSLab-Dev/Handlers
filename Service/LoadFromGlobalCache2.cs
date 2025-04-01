using System.Collections.Generic;
using System.ComponentModel;
using TSLab.Script.Handlers.Options;
using TSLab.Script.Options;
using TSLab.Utils;

namespace TSLab.Script.Handlers
{
    [HandlerCategory(HandlerCategories.ServiceElements)]
    [HelperName("Load from Global Cache", Language = Constants.En)]
    [HelperName("Загрузить из Глобального Кеша", Language = Constants.Ru)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.SECURITY | TemplateTypes.OPTION_SERIES | TemplateTypes.OPTION, Name = Constants.AnyOption)]
    [OutputType(TemplateTypes.DOUBLE)]
    [Description("Загрузить значение индикатора из Глобального Кеша")]
    [HelperDescription("Load indicator from Global Cache", Constants.En)]
    public class LoadFromGlobalCache2 : IStreamHandler, IContextUses, ICustomListValues
    {
        [HelperName("Name", Constants.En)]
        [HelperName("Название", Constants.Ru)]
        [Description("Уникальное название в Глобальном Кеше")]
        [HelperDescription("Unique name in the Global Cache", Language = Constants.En)]
        [HandlerParameter(true, "MyCache")]
        public string Name { get; set; }

        [HelperName("Load from disk", Constants.En)]
        [HelperName("Загружать с диска", Constants.Ru)]
        [Description("Загружать значения из файла на диске для повторного использования между перезапусками программы")]
        [HelperDescription("Load from HDD to use indicator values across different program sessions", Language = Constants.En)]
        [HandlerParameter(true, "true")]
        public bool LoadFromStorage { get; set; }

        public IContext Context { get; set; }

        public IList<double> Execute(ISecurity sec)
        {
            return Load(sec);
        }

        public IList<double> Execute(IOption opt)
        {
            if ((opt == null) || (opt.UnderlyingAsset == null))
                return Constants.EmptyListDouble;

            var res = Execute(opt.UnderlyingAsset);
            return res;
        }

        public IList<double> Execute(IOptionSeries optSer)
        {
            if ((optSer == null) || (optSer.UnderlyingAsset == null))
                return Constants.EmptyListDouble;

            var res = Execute(optSer.UnderlyingAsset);
            return res;
        }

        private IList<double> Load(ISecurity sec)
        {
            var res = SaveToGlobalCache2.LoadDataByBars(Context, sec, Name, LoadFromStorage);
            return res;
        }

        public IEnumerable<string> GetValuesForParameter(string paramName)
        {
            if (paramName.EqualsIgnoreCase(nameof(Name)))
                return new[] { Name ?? "" };
            return new[] { "" };
        }
    }
}
