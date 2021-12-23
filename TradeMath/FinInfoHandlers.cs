using System;
using System.Collections.Generic;
using System.ComponentModel;

using TSLab.DataSource;
using TSLab.Script.Handlers.Options;
using TSLab.Utils;

namespace TSLab.Script.Handlers
{
    /// <summary>
    /// Базовый класс для группы кубиков, которые используют FinInfo
    /// </summary>
    [HandlerCategory(HandlerCategories.TradeMath)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.SECURITY, Name = Constants.SecuritySource)]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.DOUBLE)]
    public abstract class FinInfoHandler : IBar2DoubleHandler//, IContextUses
    {
        //public IContext Context { get; set; }

        public IList<double> Execute(ISecurity source)
        {
            return new ConstList<double>(source.Bars.Count, GetValue(source.FinInfo) ?? 0);
        }

        protected abstract double? GetValue(FinInfo finInfo);
    }

    // Гарантийные обязательства покупателя
    public sealed class BuyDeposit : FinInfoHandler
    {
        protected override double? GetValue(FinInfo finInfo)
        {
            return finInfo.BuyDeposit;
        }
    }

    // Гарантийные обязательства продавца
    public sealed class SellDeposit : FinInfoHandler
    {
        protected override double? GetValue(FinInfo finInfo)
        {
            return finInfo.SellDeposit;
        }
    }

    // Категория и описание входов/выходов идет через базовый класс.
    [HelperName("Price step", Language = Constants.En)]
    [HelperName("Шаг цены", Language = Constants.Ru)]
    [Description("Шаг цены инструмента. Эта же величина показывается в таблице 'Котировки'.")]
    [HelperDescription("Price step of a security. This value is shown also in 'Quotes' table.", Constants.En)]
    public sealed class Tick : FinInfoHandler
    {
        protected override double? GetValue(FinInfo finInfo)
        {
            if (finInfo == null || finInfo.Security == null)
                return 1;

            var lastPrice = finInfo.LastPrice ?? 0.0;
            var tick = finInfo.Security.GetTick(lastPrice);
            if (!DoubleUtil.IsPositive(tick))
                tick = Math.Pow(10, -finInfo.Security.Decimals);

            return tick;
        }
    }

    // Категория и описание входов/выходов идет через базовый класс.
    [HelperName("Lot step", Language = Constants.En)]
    [HelperName("Шаг лота", Language = Constants.Ru)]
    [Description("Шаг лота инструмента. Эта же величина показывается в таблице 'Котировки'.")]
    [HelperDescription("Lot step of a security. This value is shown also in 'Quotes' table.", Constants.En)]
    public sealed class LotTick : FinInfoHandler
    {
        protected override double? GetValue(FinInfo finInfo)
        {
            return finInfo?.Security?.LotTick;
        }
    }

    [HelperName("Lot size", Language = Constants.En)]
    [HelperName("Размер лота", Language = Constants.Ru)]
    [Description("Размер лота инструмента. Блок возвращает количество акций в одном лоте. Эта же величина показывается в таблице 'Котировки'.")]
    [HelperDescription("Lot size of a security. The block returns the number of shares in one lot. This value is shown also in 'Quotes' table.", Constants.En)]
    public sealed class LotSize : FinInfoHandler
    {
        protected override double? GetValue(FinInfo finInfo)
        {
            return finInfo?.Security?.LotSize;
        }
    }
}
