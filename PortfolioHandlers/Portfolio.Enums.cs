using TSLab.Utils;

namespace TSLab.Script.Handlers
{
    public enum PositionField
    {
        [LocalizeDescription("PositionField.RealRest")] // Текущая
        RealRest,
        [LocalizeDescription("PositionField.IncomeRest")] // Входящая
        IncomeRest,
        [LocalizeDescription("PositionField.PlanRest")] // Плановая
        PlanRest,
        [LocalizeDescription("PositionField.BalancePrice")] // Учетная цена
        BalancePrice,
        [LocalizeDescription("PositionField.AssessedPrice")] // Оценочная цена
        AssessedPrice,
        [LocalizeDescription("PositionField.Commission")] // Комиссия
        Commission,
        [LocalizeDescription("PositionField.Balance")] // Чистая стоимость
        Balance,
        [LocalizeDescription("PositionField.BalForwardVolume")] // Учетная стоимость
        BalForwardVolume,
        [LocalizeDescription("PositionField.ProfitVolume")] // НП/У
        ProfitVolume,
        [LocalizeDescription("PositionField.DailyPl")] // П/У (дн)
        DailyPl,
        [LocalizeDescription("PositionField.VarMargin")] // Вар.маржа
        VarMargin,
    }

    public enum AgentField
    {
        [LocalizeDescription("AgentField.Profit")] // П/У
        Profit,

        [LocalizeDescription("AgentField.DailyProfit")] // П/У (дн)
        DailyProfit,

        [LocalizeDescription("AgentField.ProfitVol")] // НП/У
        ProfitVol,

        [LocalizeDescription("AgentField.PositionInLots")] // Позиция (лоты)
        PositionInLots,

        [LocalizeDescription("AgentField.PositionInMoney")] // Позиция (деньги)
        PositionInMoney,

        [LocalizeDescription("AgentField.PositionLong")] // Длинная поз.(лоты)
        PositionLong,

        [LocalizeDescription("AgentField.PositionShort")] // Короткая поз.(лоты)
        PositionShort,

        [LocalizeDescription("AgentField.AssessedPrice")] // Оценочная цена
        AssessedPrice,

        [LocalizeDescription("AgentField.BalancePrice")] // Учетная цена
        BalancePrice,

        [LocalizeDescription("AgentField.LastPrice")] // Текущая цена
        LastPrice,

        [LocalizeDescription("AgentField.Commission")] // Комиссия
        Commission,

        [LocalizeDescription("AgentField.Slippage")] // Проскальзывание
        Slippage,

        [LocalizeDescription("AgentField.SlippagePercent")] // Проскальзывание %
        SlippagePercent,
    }

    public enum ProfitKind
    {
        [LocalizeDescription("ProfitKind.Unfixed")]
        Unfixed,
        [LocalizeDescription("ProfitKind.Fixed")]
        Fixed,
        [LocalizeDescription("ProfitKind.MaxFixed")]
        MaxFixed,
    }
}
