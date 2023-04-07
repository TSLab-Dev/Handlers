using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using TSLab.DataSource;
using TSLab.Script.Handlers.Options;
using TSLab.Utils;

namespace TSLab.Script.Handlers
{
    [HandlerCategory(HandlerCategories.ServiceElements)]
    [HelperName("Save to Global Cache", Language = Constants.En)]
    [HelperName("Сохранить в Глобальный Кеш", Language = Constants.Ru)]
    [HandlerAlwaysKeep]
    [InputsCount(2)]
    [Input(0, TemplateTypes.SECURITY, Name = Constants.SecuritySource)]
    [Input(1, TemplateTypes.DOUBLE, Name = "Indicator")]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.DOUBLE)]
    [Description("Сохранить значение любого индикатора в Глобальный Кеш. " +
        "Записывается последнее значение. " +
        "Записанные данные можно посмотреть в файле Temp\\GlobalCache_'name'.txt.")]
    [HelperDescription("Save any indicator to Global Cache. " +
        "The last value is recorded. " +
        "The recorded data can be viewed in the file Temp\\GlobalCache_'name'.txt.", Constants.En)]
    public class SaveToGlobalCache2 : IValuesHandlerWithNumber, IContextUses, ICustomListValues
    {
        public const string KeyPrefix = "GC2";

        [HelperName("Name", Constants.En)]
        [HelperName("Название", Constants.Ru)]
        [Description("Уникальное название в Глобальном Кеше")]
        [HelperDescription("Unique name in the Global Cache", Language = Constants.En)]
        [HandlerParameter(true, "")]
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
                    TrimData(data, MaxValues, sec.Bars.First().Date, dt);
                    data[dt] = value;
                    SaveData(Context, data, Name, SaveToStorage);
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

        public IEnumerable<string> GetValuesForParameter(string paramName)
        {
            if (paramName.EqualsIgnoreCase(nameof(Name)))
                return new[] { Name ?? "" };
            return new[] { "" };
        }

        public static void SaveData(IContext ctx, SortedList<DateTime, double> data, string name, bool isStorage)
        {
            var key = string.Join("_", KeyPrefix, name);
            ctx.StoreGlobalObject(key, new NotClearableContainer(data), isStorage);
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

                if (res != null)
                    TraceToFile(res, name);
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
                var values = data.Values.ToList();

                for (int i = 0; i < res.Length; i++)
                {
                    var date = bars[i].Date;
                    var index = dates.FindLastIndex(x => x <= date);
                    if (index >= 0)
                        res[i] = values[index];
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
                var fileName = Path.Combine(AppPath.TempFolder, FileUtil.GetValidFilePath($"GlobalCache_{name}.txt"));
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
            foreach (var item in data.Keys.ToArray())
            {
                if (firstDate != default && item < firstDate)
                    data.Remove(item);

                if (lastDate != default && item > lastDate)
                    data.Remove(item);
            }
            while (maxLength > 0 && data.Count > maxLength)
                data.RemoveAt(0);
        }
    }
}
