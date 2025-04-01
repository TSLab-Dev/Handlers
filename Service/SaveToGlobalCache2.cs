using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using TSLab.DataSource;
using TSLab.Script.Handlers.Options;
using TSLab.Script.Options;
using TSLab.Utils;
using TSLab.Utils.PathService;

namespace TSLab.Script.Handlers
{
    [HandlerCategory(HandlerCategories.ServiceElements)]
    [HelperName("Save to Global Cache", Language = Constants.En)]
    [HelperName("Сохранить в Глобальный Кеш", Language = Constants.Ru)]
    [HandlerAlwaysKeep]
    [InputsCount(2)]
    [Input(0, TemplateTypes.SECURITY | TemplateTypes.OPTION_SERIES | TemplateTypes.OPTION, Name = Constants.AnyOption)]
    [Input(1, TemplateTypes.DOUBLE, Name = "Indicator")]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.DOUBLE)]
    [Description("Сохранить значение любого индикатора в Глобальный Кеш. " +
        "Записывается последнее значение.")]
    [HelperDescription("Save any indicator to Global Cache. " +
        "The last value is recorded.", Constants.En)]
    public class SaveToGlobalCache2 : IValuesHandlerWithNumber, IContextUses, ICustomListValues
    {
        public const string KeyPrefix = "GC2";

        [HelperName("Name", Constants.En)]
        [HelperName("Название", Constants.Ru)]
        [Description("Уникальное название в Глобальном Кеше")]
        [HelperDescription("Unique name in the Global Cache", Language = Constants.En)]
        [HandlerParameter(true, "MyCache")]
        public string Name { get; set; }

        [HelperName("Save to disk", Constants.En)]
        [HelperName("Сохранять на диск", Constants.Ru)]
        [Description("Сохранять значения в файл на диске для повторного использования между перезапусками программы")]
        [HelperDescription("Save to HDD to use indicator values across different program sessions", Language = Constants.En)]
        [HandlerParameter(true, "true")]
        public bool SaveToStorage { get; set; }

        [HelperName("Save to next bar", Constants.En)]
        [HelperName("Сохранять на следующий бар", Constants.Ru)]
        [Description("Сохранять новое значение на следующий бар. Если выключено, то будет сохранять на текущий бар")]
        [HelperDescription("Save the new value to the next bar. If disabled, it will save to the current bar", Language = Constants.En)]
        [HandlerParameter(true, "true")]
        public bool SaveToNextBar { get; set; }

        [HelperName("Maximum numbers", Constants.En)]
        [HelperName("Максимальное количество", Constants.Ru)]
        [Description("Максимальное количество сохраняемых значений. Если 0, то будет ограничиваться количеством баров")]
        [HelperDescription("The maximum number of values. If 0, then it will be limited by the number of bars", Language = Constants.En)]
        [HandlerParameter(true, "0")]
        public int MaxValues { get; set; }

        [HelperName("Do not trim", Constants.En)]
        [HelperName("Не ограничивать", Constants.Ru)]
        [Description("При сохранении не ограничивать данные. При включении этой настройки не будет учитываться настройка 'Максимальное количество'")]
        [HelperDescription("When saving, do not limit the data. When enabling this setting, the 'Maximum numbers' setting will not be taken into account", Language = Constants.En)]
        [HandlerParameter(true, "false")]
        public bool NotTrim { get; set; }

        [HelperName("Логировать в файл")]
        [Description(@"Логировать данные в файл Temp\GlobalCache_{name}.txt")]
        [HandlerParameter(true, "false")]
        public bool IsTraceToFile { get; set; }

        public IContext Context { get; set; }

        private double[] m_tempValues;

        public double Execute(ISecurity sec, double value, int index)
        {
            var lastNumber = Context.IsLastBarUsed ? sec.Bars.Count - 1 : sec.Bars.Count - 2;

            // Сохраняем последнее значение
            if (index >= lastNumber)
            {
                var data = LoadData(Context, Name, SaveToStorage) ?? new SortedList<DateTime, double>();
                lock (data)
                {
                    var dt = sec.Bars[index].Date;
                    if (SaveToNextBar)
                        dt = sec.IntervalInstance.AddShift(dt);
                    if (!NotTrim)
                        TrimData(data, MaxValues, sec.Bars.First().Date, dt);
                    data[dt] = value;
                    SaveData(Context, data, Name, SaveToStorage, IsTraceToFile);
                }
            }

            // Читаем все значение
            if (m_tempValues == null || index == 0 || index >= lastNumber)
            {
                m_tempValues = LoadDataByBars(Context, sec, Name, SaveToStorage);
            }
            var res = m_tempValues.Length > index ? m_tempValues[index] : default;
            return res;
        }

        public double Execute(IOption opt, double value, int index)
        {
            if ((opt == null) || double.IsNaN(value) || double.IsInfinity(value))
                return default;
            
            var res = Execute(opt.UnderlyingAsset, value, index);
            return res;
        }

        public double Execute(IOptionSeries optSer, double value, int index)
        {
            if ((optSer == null) || (optSer.UnderlyingAsset == null) || double.IsNaN(value) || double.IsInfinity(value))
                return default;

            var res = Execute(optSer.UnderlyingAsset, value, index);
            return res;
        }

        public IEnumerable<string> GetValuesForParameter(string paramName)
        {
            if (paramName.EqualsIgnoreCase(nameof(Name)))
                return new[] { Name ?? "" };
            return new[] { "" };
        }

        public static void SaveData(IContext ctx, SortedList<DateTime, double> data, string name, bool isStorage, bool isTraceToFile)
        {
            var key = string.Join("_", KeyPrefix, name);
            ctx.StoreGlobalObject(key, new NotClearableContainer(data), isStorage);
            if (isTraceToFile)
                TraceToFile(data, name);
        }

        public static SortedList<DateTime, double> LoadData(IContext ctx, string name, bool isStorage)
        {
            var key = string.Join("_", KeyPrefix, name);
            var obj = ctx.LoadGlobalObject(key, isStorage);
            SortedList<DateTime, double> res = null;

            if (obj is NotClearableContainer cont)
            {
                if (cont.Content is SortedList<DateTime, double> d1)
                    res = d1;

                // старый вариант сохранения в 2.2.9.0
                if (res == null && cont.Content is Dictionary<DateTime, double> d2)
                {
                    res = new SortedList<DateTime, double>();
                    foreach (var key2 in d2)
                        res[key2.Key] = key2.Value;
                }
            }
            return res;
        }

        public static double[] LoadDataByBars(IContext ctx, ISecurity sec, string name, bool isStorage)
        {
            var bars = sec.Bars;
            var res = new double[bars.Count];
            var data = LoadData(ctx, name, isStorage);
            if (data == null)
                return res;

            lock (data)
            {
                var dates = data.Keys.ToList();

                for (int i = 0; i < res.Length; i++)
                {
                    var date = bars[i].Date;
                    var index = dates.FindLastIndex(x => x <= date);
                    if (index >= 0)
                        res[i] = data.Values[index];
                }
            }
            return res;
        }

        private static void TraceToFile(SortedList<DateTime, double> data, string name)
        {
            try
            {
                if (data == null)
                    return;
                var fileName = PathLocator.Current.GetPath(AppFolder.Temp, FileUtil.GetValidFilePath($"GlobalCache_{name}.txt"));
                var sb = new StringBuilder();
                foreach (var item in data)
                    sb.AppendLine($"{item.Key:dd.MM.yyyy HH:mm:ss.fff}: {item.Value}");
                FileUtil.CreateFolderForFile(fileName);
                File.WriteAllText(fileName, sb.ToString());
            }
            catch { }
        }

        private static void TrimData(SortedList<DateTime, double> data, int maxLength, 
            DateTime firstDate = default, DateTime lastDate = default)
        {
            var toRemove = new List<DateTime>();
            foreach (var item in data.Keys)
            {
                if (firstDate != default && item < firstDate)
                    toRemove.Add(item);

                if (lastDate != default && item > lastDate)
                    toRemove.Add(item);
            }
            toRemove.ForEach(d => data.Remove(d));
            while (maxLength > 0 && data.Count > maxLength)
                data.RemoveAt(0);
        }
    }
}
