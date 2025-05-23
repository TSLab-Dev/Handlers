﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TSLab.DataSource;
using TSLab.Script.Handlers.Options;
using TSLab.Script.Realtime;
using TSLab.Utils;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global
namespace TSLab.Script.Handlers
{
    [HandlerCategory(HandlerCategories.Portfolio)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.SECURITY, Name = Constants.SecuritySource)]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.BOOL)]
    public class IsPortfolioReady : ConstGenBase<bool>, IBar2BoolHandler, IBar2BoolsHandler
    {
        public IList<bool> Execute(ISecurity source)
        {
            MakeList(source.Bars.Count, source.IsPortfolioReady);
            return this;
        }

        public bool Execute(ISecurity source, int barNum)
        {
            return source.IsPortfolioReady;
        }
    }

    [HandlerCategory(HandlerCategories.Portfolio)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.SECURITY, Name = Constants.SecuritySource)]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.DOUBLE)]
    public class InitialDeposit : IBar2ValueDoubleHandler
    {
        public double Execute(ISecurity source, int barNum)
        {
            return source.InitDeposit;
        }
    }

    [HandlerCategory(HandlerCategories.Portfolio)]
    [HelperName("Free money", Language = Constants.En)]
    [HelperName("Свободные деньги", Language = Constants.Ru)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.SECURITY, Name = Constants.SecuritySource)]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.DOUBLE)]
    [Description("Показывает наличие свободных денег на счету. В режиме агента информация транслируется со счета. В режиме лаборатории рассчитывается на основании позиций по формуле: " +
        "Свободные деньги = деньги - позиции - деньги блокированные в заявках.")]
    [HelperDescription("Shows free money in your account. In agent mode information about free money is received from your account. In laboratory mode information about free money is calculated according to the following formula: " +
        "Free Money = money - (minus)positions - (minus)money blocked in orders.", Constants.En)]
    public class FreeMoney : IBar2ValueDoubleHandler
    {
        private WholeProfitState m_state = null;

        public double Execute(ISecurity source, int barNum)
        {
            var srt = source as ISecurityRt;
            if (srt != null)
            {
                return srt.CurrencyBalance;
            }
            if (m_state == null)
                m_state = new WholeProfitState(source.Positions);
            m_state.ProcessBar(barNum);
            var inPos = m_state.Active.Sum(pos => pos.EntryPrice * pos.Shares * source.LotSize);
            return source.InitDeposit + m_state.ClosedProfitCache - inPos;
        }
    }

    [HandlerCategory(HandlerCategories.Portfolio)]
    [HelperName("Portfolio estimation", Language = Constants.En)]
    [HelperName("Оценка портфеля", Language = Constants.Ru)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.SECURITY, Name = Constants.SecuritySource)]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.DOUBLE)]
    [Description("Показывает оценку портфеля. В режиме агента информация транслируется со счета. В режиме лаборатории рассчитывается на основании позиции по формуле: " +
        "Оценка портфеля = деньги + позиции.")]
    [HelperDescription("Shows your portfolio estimation. In agent mode portfolio estimation is received from your account. In laboratory mode portfolio estimation is calculated according the following formula: " +
        "Portfolio Estimation = money + positions.", Constants.En)]
    public sealed class EstimatedMoney : IBar2ValueDoubleHandler
    {
        private WholeProfitState m_state = null;

        public double Execute(ISecurity source, int barNum)
        {
            if (m_state == null)
                m_state = new WholeProfitState(source.Positions);
            m_state.ProcessBar(barNum);
            var securityRt = source as ISecurityRt;
            return securityRt?.EstimatedBalance ?? source.InitDeposit + m_state.GetProfit(barNum);
        }
    }

    [HandlerCategory(HandlerCategories.Portfolio)]
    [HelperName("Current position", Language = Constants.En)]
    [HelperName("Текущая позиция", Language = Constants.Ru)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.SECURITY, Name = Constants.SecuritySource)]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.DOUBLE)]
    [Description("Показывает совокупную позицию по бумаге. В режиме лаборатории отображается расчетная позиция скрипта. " +
        "В режиме агента отображается значение из колонки 'Текущая' окна 'Позиции' для торгуемых источников.")]
    [HelperDescription("Shows a total position involving an instrument. In laboratory mode this block shows a calculated position of a script. " +
        "In agent mode this block shows a value of the Current column(in the Positions window) for tradable sources.", Constants.En)]
    public class CurrentPosition : IBar2ValueDoubleHandler
    {
        public double Execute(ISecurity source, int barNum)
        {
            var srt = source as ISecurityRt;
            if (srt != null)
            {
                return srt.BalanceQuantity;
            }
            var activePos = source.Positions.GetActiveForBar(barNum);
            return activePos.Sum(pos => pos.Shares * (pos.IsLong ? 1 : -1));
        }
    }

    [HandlerCategory(HandlerCategories.Portfolio)]
    [HelperName("Agent current position", Language = Constants.En)]
    [HelperName("Текущая позиция агента", Language = Constants.Ru)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.SECURITY, Name = Constants.SecuritySource)]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.DOUBLE)]
    [Description("Отображает расчетную позицию агента.")]
    [HelperDescription("This block shows a calculated position of an agent.", Constants.En)]
    public class AgentCurrentPosition : IBar2ValueDoubleHandler
    {
        public double Execute(ISecurity source, int barNum)
        {
            var activePos = source.Positions.GetActiveForBar(barNum);
            return activePos.Sum(pos => pos.Shares * (pos.IsLong ? 1 : -1));
        }
    }

    [HandlerCategory(HandlerCategories.Portfolio)]
    [HelperName("Position by name", Language = Constants.En)]
    [HelperName("Позиция по имени", Language = Constants.Ru)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.SECURITY, Name = "SECURITYSource")]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.DOUBLE)]
    [Description("Получить значение позиции из таблицы Позиции.")]
    [HelperDescription("Get the position value from the Positions table.", Constants.En)]
    public class PositionByName : IStreamHandler, ICustomListValues
    {
        [HelperName("Symbol", Constants.En)]
        [HelperName("Название инструмента", Constants.Ru)]
        [Description("Название инструмента из таблицы Позиции.")]
        [HelperDescription("Symbol from the position table.", Constants.En)]
        [HandlerParameter(true)]
        public string Symbol { get; set; }

        [HelperName("Account", Constants.En)]
        [HelperName("Счет", Constants.Ru)]
        [Description("Название счета (необязательно).")]
        [HelperDescription("Account name (optional).", Constants.En)]
        [HandlerParameter(true)]
        public string Account { get; set; }

        [HelperName("Currency", Constants.En)]
        [HelperName("Валюта", Constants.Ru)]
        [Description("Название валюты (необязательно).")]
        [HelperDescription("Currency name (optional).", Constants.En)]
        [HandlerParameter(true)]
        public string Currency { get; set; }

        [HelperName("Position field", Constants.En)]
        [HelperName("Поле позиции", Constants.Ru)]
        [Description("Выводимое поле позиции.")]
        [HelperDescription("Output position field.", Constants.En)]
        [HandlerParameter(true, nameof(PositionField.RealRest), Name = "Поле позиции")]
        public PositionField PositionField { get; set; }

        public IList<double> Execute(ISecurity source)
        {
            var value = 0.0;
            var ds = source?.SecurityDescription?.TradePlace?.DataSource;
            if (!string.IsNullOrEmpty(Symbol) && ds != null && ds is IPortfolioSourceBase portfolio)
            {
                foreach (var account in portfolio.Accounts)
                {
                    if (string.IsNullOrWhiteSpace(Account) || Account.Equals(account.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        var balances = portfolio.GetBalances(account.Id);
                        var balance = FindBalance(balances, Symbol, Currency);
                        if (balance != null)
                        {
                            value = GetValue(balance, PositionField);
                            break;
                        }
                    }
                }

            }
            return Enumerable.Repeat(value, source.Bars.Count).ToList();
        }

        private static BalanceInfo FindBalance(IEnumerable<BalanceInfo> balances, string symbol, string currency)
        {
            var request = balances.AsEnumerable();

            if (!string.IsNullOrEmpty(symbol))
            {
                request = request.Where(x => 
                    symbol.Equals(x.SecurityName, StringComparison.OrdinalIgnoreCase) ||
                    symbol.Equals(x.SecurityFullName, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(currency))
            {
                request = request.Where(x => currency.Equals(x.Security?.Currency, StringComparison.OrdinalIgnoreCase));
            }

            var res = request.FirstOrDefault();
            return res;
        }

        private static double GetValue(BalanceInfo balance, PositionField positionField)
        {
            switch (positionField)
            {
                case PositionField.RealRest:
                    return balance.RealRest ?? 0;
                case PositionField.IncomeRest:
                    return balance.IncomeRest ?? 0;
                case PositionField.PlanRest:
                    return balance.PlanRest ?? 0;
                case PositionField.BalancePrice:
                    return balance.BalancePrice ?? 0;
                case PositionField.AssessedPrice:
                    return balance.AssessedPrice ?? 0;
                case PositionField.Commission:
                    return balance.Commission ?? 0;
                case PositionField.Balance:
                    return balance.Balance ?? 0;
                case PositionField.BalForwardVolume:
                    return balance.BalForwardVolume ?? 0;
                case PositionField.ProfitVolume:
                    return balance.ProfitVolume ?? 0;
                case PositionField.DailyPl:
                    return balance.DailyPl ?? 0;
                case PositionField.VarMargin:
                    return balance.VarMargin ?? 0;
            }
            return 0;
        }

        public IEnumerable<string> GetValuesForParameter(string paramName)
        {
            if (paramName.Equals("Symbol", StringComparison.InvariantCultureIgnoreCase))
                return new[] { Symbol ?? "" };

            if (paramName.Equals("Account", StringComparison.InvariantCultureIgnoreCase))
                return new[] { Account ?? "" };

            if (paramName.Equals("Currency", StringComparison.InvariantCultureIgnoreCase))
                return new[] { Currency ?? "" };

            return new[] { "" };
        }
    }

    [HandlerCategory(HandlerCategories.Portfolio)]
    [HelperName("Table Agents", Language = Constants.En)]
    [HelperName("Таблица Агенты", Language = Constants.Ru)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.SECURITY, Name = "SECURITYSource")]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.DOUBLE)]
    [Description("Предназначен для работы с таблицей Агенты из скрипта.\r\n" +
        "Возвращает числовое значение, соответствующее выбранному полю таблицы.\r\n" +
        "В зависимости от поставщика данных, для определения принадлежности инструмента к рынку, используйте счет или валюту счета.\r\n" +
        "Для большинства рынков достаточно указать Тикер.")]
    [HelperDescription("Designed to work with the Agents table from a script.\r\n" +
        "Returns a numeric value corresponding to the selected table field.\r\n" +
        "Depending on the data provider, use the account or the account currency to determine whether the instrument belongs to the market.\r\n" +
        "For most markets, it is enough to specify a Ticker.", Constants.En)]
    public class AgentsInfo : IStreamHandler, IContextUses, ICustomListValues
    {
        [HelperName("Agent", Constants.En)]
        [HelperName("Агент", Constants.Ru)]
        [Description("Название агента из таблицы Агенты.")]
        [HelperDescription("The name of the agent from the Agents table.", Constants.En)]
        [HandlerParameter(true)]
        public string AgentName { get; set; }

        [HelperName("Tiker", Constants.En)]
        [HelperName("Тикер", Constants.Ru)]
        [Description("Название инструмента.")]
        [HelperDescription("Instrument name.", Constants.En)]
        [HandlerParameter(true)]
        public string SecurityName { get; set; }

        [HelperName("Account", Constants.En)]
        [HelperName("Счет", Constants.Ru)]
        [Description("Название счета  (необязательно).")]
        [HelperDescription("Account name (optional).", Constants.En)]
        [HandlerParameter(true)]
        public string AccountName { get; set; }

        [HelperName("Currency", Constants.En)]
        [HelperName("Валюта счета", Constants.Ru)]
        [Description("Валюта счета  (необязательно).")]
        [HelperDescription("Account currency (optional).", Constants.En)]
        [HandlerParameter(true)]
        public string CurrencyName { get; set; }

        [HelperName("Output value", Constants.En)]
        [HelperName("Выводимое значение", Constants.Ru)]
        [Description("Выводимое значение из таблицы, соответствующее настройкам.")]
        [HelperDescription("The output value from the table corresponding to the settings.", Constants.En)]
        [HandlerParameter(true, nameof(AgentField.Profit), Name = "Выводимое значение")]
        public AgentField AgentField { get; set; }
        
        public IContext Context { get; set; }

        public IList<double> Execute(ISecurity sec)
        {
            var value = 0.0;
            var agent = Context.Runtime.GetAllAgentRuntimeInfo().FirstOrDefault(x => AgentName.EqualsIgnoreCase(x.AgentName));

            if (agent?.SourceItems != null)
            {
                var sources = agent.SourceItems.Where(x => SecurityName.EqualsIgnoreCase(x.SecurityName));
                if (!string.IsNullOrEmpty(AccountName))
                    sources = sources.Where(x => AccountName.EqualsIgnoreCase(x.AccountName));
                if (!string.IsNullOrEmpty(CurrencyName))
                    sources = sources.Where(x => CurrencyName.EqualsIgnoreCase(x.CurrencyName));

                var source = sources.FirstOrDefault();
                if (source != null)
                {
                    value = GetValue(source, AgentField);
                }
            }
            
            return Enumerable.Repeat(value, sec.Bars.Count).ToList();
        }

        private static double GetValue(ISourceRuntimeInfo source, AgentField agentField)
        {
            switch (agentField)
            {
                case AgentField.ProfitVol:
                    return source.ProfitVol;

                case AgentField.Profit:
                    return source.Profit;

                case AgentField.Slippage:
                    return source.Slippage;

                case AgentField.SlippagePercent:
                    return source.SlippagePercent;

                case AgentField.PositionInLots:
                    return source.PositionInLots;

                case AgentField.PositionInMoney:
                    return source.PositionInMoney;

                case AgentField.DailyProfit:
                    return source.DailyProfit;

                case AgentField.AssessedPrice:
                    return source.AssessedPrice;

                case AgentField.BalancePrice:
                    return source.BalancePrice;

                case AgentField.LastPrice:
                    return source.LastPrice ?? 0;

                case AgentField.PositionLong:
                    return source.PositionLong;

                case AgentField.PositionShort:
                    return source.PositionShort;

                case AgentField.Commission:
                    return source.Commission;
            }
            return 0;
        }

        public IEnumerable<string> GetValuesForParameter(string paramName)
        {
            if (paramName.EqualsIgnoreCase(nameof(AgentName)))
                return new[] { AgentName ?? "" };

            if (paramName.EqualsIgnoreCase(nameof(SecurityName)))
                return new[] { SecurityName ?? "" };

            if (paramName.EqualsIgnoreCase(nameof(AccountName)))
                return new[] { AccountName ?? "" };

            if (paramName.EqualsIgnoreCase(nameof(CurrencyName)))
                return new[] { CurrencyName ?? "" };

            return new[] { "" };
        }
    }

    [HandlerCategory(HandlerCategories.Portfolio)]
    [HelperName("Net value by account", Language = Constants.En)]
    [HelperName("Чистая стоимость по счету", Language = Constants.Ru)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.SECURITY, Name = "SECURITYSource")]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.DOUBLE)]
    [Description("Получить суммарное значение чистой стоимости по счету. " +
        "Если счет не указан, то берется название счета из агента (только в режиме агента).")]
    [HelperDescription("Get the total net value of the account. " +
        "If the account is not specified, the account name is taken from the agent (only in agent mode).", Constants.En)]
    public class NetValueByAccount : IStreamHandler, ICustomListValues
    {
        [HelperName("Account", Constants.En)]
        [HelperName("Счет", Constants.Ru)]
        [Description("Название счета (необязательно).")]
        [HelperDescription("Account name (optional).", Constants.En)]
        [HandlerParameter(true)]
        public string Account { get; set; }

        public IList<double> Execute(ISecurity source)
        {
            var value = 0.0;
            var ds = source?.SecurityDescription?.TradePlace?.DataSource;
            var accountName = Account;
            if (string.IsNullOrEmpty(accountName))
                accountName = (source as ISecurityRt)?.PortfolioName ?? "";

            if (!string.IsNullOrEmpty(accountName) && ds is IPortfolioSourceBase portfolio)
            {
                foreach (var account in portfolio.Accounts)
                {
                    if (accountName.Equals(account.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        var balances = portfolio.GetBalances(account.Id);
                        value = balances.Sum(x => x.Balance ?? 0);
                        break;
                    }
                }

            }
            return Enumerable.Repeat(value, source.Bars.Count).ToList();
        }

        public IEnumerable<string> GetValuesForParameter(string paramName)
        {
            if (paramName.Equals("Account", StringComparison.InvariantCultureIgnoreCase))
                return new[] { Account ?? "" };

            return new[] { "" };
        }
    }

    // Лучше не вешать категорию на базовые абстрактные классы. Это снижает гибкость дальнейшего управления ими.
    public abstract class BaseProfitHandler : IBar2ValueDoubleHandler
    {
        /// <summary>
        /// \~english Profit kind (fixed or unfixed)
        /// \~russian Тип прибыли (фиксированная или плавающая)
        /// </summary>
        [HelperName("Profit kind", Constants.En)]
        [HelperName("Тип прибыли", Constants.Ru)]
        [Description("Тип прибыли (фиксированная или плавающая)")]
        [HelperDescription("Profit kind (fixed or unfixed)", Constants.En)]
        [HandlerParameter(true, nameof(ProfitKind.Unfixed))]
        public ProfitKind ProfitKind { get; set; }

        public abstract double Execute(ISecurity source, int barNum);
    }

    [HandlerCategory(HandlerCategories.Portfolio)]
    [HelperName("Profit (whole period)", Language = Constants.En)]
    [HelperName("Доход (за все время)", Language = Constants.Ru)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.SECURITY, Name = Constants.SecuritySource)]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.DOUBLE)]
    [Description("Считает доход по бумаге по сделкам за все время.")]
    [HelperDescription("Calculates profit involving an instrument received in all trades of the whole period.", Constants.En)]
    public sealed class WholeTimeProfit : BaseProfitHandler
    {
        [HelperName("Direction trades", Constants.En)]
        [HelperName("Направление сделок", Constants.Ru)]
        [HandlerParameter(true, nameof(TradeDirection2.All))]
        public TradeDirection2 Direction { get; set; }

        // Расчет нефиксированного профита по минимальной цене свечи (для long это low, для short это high).
        // Не стал это выносить в enum ProfitKind чтобы не путать пользователей.
        // Используется при расчете просадки.
        public bool UnfixedByMin { get; set; }

        private WholeProfitState m_state = null;

        public override double Execute(ISecurity source, int barNum)
        {
            if (m_state == null)
                m_state = new WholeProfitState(source.Positions, Direction);
            m_state.ProcessBar(barNum);
            switch (ProfitKind)
            {
                case ProfitKind.Unfixed:
                    return UnfixedByMin ? m_state.GetProfitMin(barNum) : m_state.GetProfit(barNum);
                case ProfitKind.Fixed:
                    return m_state.GetAccumulatedProfit(barNum);
                case ProfitKind.MaxFixed:
                    return GetMaxFixed(source, barNum);
                default:
                    throw new InvalidEnumArgumentException(nameof(ProfitKind), (int)ProfitKind, ProfitKind.GetType());
            }
        }

        private double m_maxFixed;
        private double GetMaxFixed(ISecurity source, int barNum)
        {
            if (barNum == 0)
                m_maxFixed = 0;
            m_maxFixed = Math.Max(m_maxFixed, m_state.GetAccumulatedProfit(barNum));
            return m_maxFixed;
        }
    }

    [HandlerCategory(HandlerCategories.Portfolio)]
    [HelperName("Profit (one day period)", Language = Constants.En)]
    [HelperName("Доход (за день)", Language = Constants.Ru)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.SECURITY, Name = Constants.SecuritySource)]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.DOUBLE)]
    [Description("Считает доход по бумаге по сделкам за день.")]
    [HelperDescription("Calculates profit involving an instrument received in all trades of the day.", Constants.En)]
    public sealed class WholeDayProfit : BaseProfitHandler
    {
        private int m_oldStartBarNum = -1;

        private DateTime m_oldStartDate;

        private double m_maxProfit;

        [HelperName("Session start", Constants.En)]
        [HelperName("Начало сессии", Constants.Ru)]
        [Description("Время начала торговой сессии (формат 1ч 30м 00с)")]
        [HelperDescription("Trading session start (format 1h 30m 00s)", Constants.En)]
        [HandlerParameter(true, "0:0:0", Min = "0:0:0", Max = "23:59:59", Step = "1:0:0", EditorMin = "0:0:0", EditorMax = "23:59:59")]
        public TimeSpan SessionStart { get; set; }

        public override double Execute(ISecurity source, int barNum)
        {
            var barDate = source.Bars[barNum].Date;
            var startDay = barDate.Date.Add(SessionStart);

            if (startDay > barDate)
                startDay = startDay.AddDays(-1);

            var endDay = startDay.AddDays(1);
            if (startDay > m_oldStartDate)
            {
                m_oldStartDate = startDay;
                m_oldStartBarNum = barNum;
                m_maxProfit = 0;
            }
            Func<IPosition, int, double> getProfitFunc;
            switch (ProfitKind)
            {
                case ProfitKind.Unfixed:
                    getProfitFunc = ProfitExtensions.GetProfit;
                    break;
                case ProfitKind.Fixed:
                case ProfitKind.MaxFixed:
                    getProfitFunc = ProfitExtensions.GetAccumulatedProfit;
                    break;
                default:
                    throw new InvalidEnumArgumentException(nameof(ProfitKind), (int)ProfitKind, ProfitKind.GetType());
            }
            
            var profit = CalcProfit(source, barNum, getProfitFunc, startDay, endDay, m_oldStartBarNum);
            m_maxProfit = Math.Max(m_maxProfit, profit);
            return ProfitKind == ProfitKind.MaxFixed ? m_maxProfit : profit;
        }

        public static double CalcProfit(ISecurity source, int barNum, Func<IPosition, int, double> getProfitFunc, 
            int days, ref DateTime oldStartDate, ref int oldStartBarNum, out bool isResetPeriod)
        {
            isResetPeriod = false;
            var dt = source.Bars[barNum].Date;
            var startDay = dt.Date;
            var endDay = startDay.AddDays(1);
            var startBarNum = barNum;
            if (days > 1)
            {
                startDay = startDay.AddDays(1 - days);
                if (startDay < source.Bars[0].Date)
                {
                    startDay = source.Bars[0].Date;
                    startBarNum = 0;
                }
            }
            if (startDay > oldStartDate)
            {
                isResetPeriod = true;
                oldStartDate = startDay;
                if (days > 1)
                {
                    for (int i = barNum; i >= 0; i--)
                    {
                        if (source.Bars[i].Date >= startDay) continue;
                        startBarNum = i + 1;
                        break;
                    }
                }
                oldStartBarNum = startBarNum;
            }
            var profit = CalcProfit(source, barNum, getProfitFunc, startDay, endDay, oldStartBarNum);
            return profit;
        }

        private static double CalcProfit(ISecurity source, int barNum, Func<IPosition, int, double> getProfitFunc, 
            DateTime startDay, DateTime endDay, int oldStartBarNum)
        {
            double profit = 0;
            foreach (var pos in source.Positions)
            {
#pragma warning disable 612
                if (pos.EntryBarNum <= barNum && (pos.IsActive || (pos.ExitBar.Date >= startDay && pos.EntryBar.Date < endDay)))
#pragma warning restore 612
                {
                    profit += getProfitFunc(pos, barNum);
                    if (pos.EntryBarNum < oldStartBarNum)
                    {
                        profit -= pos.CurrentProfitByOpenPrice(oldStartBarNum);
                    }
                }
            }
            return profit;
        }
    }

    //[HandlerCategory(HandlerCategories.Portfolio)]
    // Лучше не вешать категорию на базовые абстрактные классы. Это снижает гибкость дальнейшего управления ими.
    public abstract class BasePeriodProfitHandler : BasePeriodIndicatorHandler, IBar2ValueDoubleHandler
    {
        /// <summary>
        /// \~english Profit kind (fixed or unfixed)
        /// \~russian Тип прибыли (фиксированная или плавающая)
        /// </summary>
        [HelperName("Profit kind", Constants.En)]
        [HelperName("Тип прибыли", Constants.Ru)]
        [Description("Тип прибыли (фиксированная или плавающая)")]
        [HelperDescription("Profit kind (fixed or unfixed)", Constants.En)]
        [HandlerParameter(true, nameof(ProfitKind.Unfixed))]
        public ProfitKind ProfitKind { get; set; }

        public abstract double Execute(ISecurity source, int barNum);

        protected Func<IPosition, int, double> GetProfitFunc()
        {
            switch (ProfitKind)
            {
                case ProfitKind.Unfixed:
                    return ProfitExtensions.GetProfit;
                case ProfitKind.Fixed:
                case ProfitKind.MaxFixed:
                    return ProfitExtensions.GetAccumulatedProfit;
                default:
                    throw new InvalidEnumArgumentException(nameof(ProfitKind), (int)ProfitKind, ProfitKind.GetType());
            }
        }
    }

    [HandlerCategory(HandlerCategories.Portfolio)]
    [HelperName("Profit (in N days)", Language = Constants.En)]
    [HelperName("Доход (за N дней)", Language = Constants.Ru)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.SECURITY, Name = Constants.SecuritySource)]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.DOUBLE)]
    [Description("Считает доход по бумаге за указанное количество дней.")]
    [HelperDescription("Calculates profit involving an instrument received during a specified number of days.", Constants.En)]
    public sealed class NumDaysProfit : BasePeriodProfitHandler
    {
        private int m_oldDayStart = -1;

        private DateTime m_oldStartDay;

        private double m_maxProfit;

        public override double Execute(ISecurity source, int barNum)
        {
            var profit = WholeDayProfit.CalcProfit(source, barNum, GetProfitFunc(), Period, ref m_oldStartDay, 
                ref m_oldDayStart, out var isResetPeriod);
            if (isResetPeriod)
                m_maxProfit = 0;
            m_maxProfit = Math.Max(m_maxProfit, profit);
            return ProfitKind == ProfitKind.MaxFixed ? m_maxProfit : profit;
        }
    }

    [HandlerCategory(HandlerCategories.Portfolio)]
    [HelperName("Profit (in N minutes)", Language = Constants.En)]
    [HelperName("Доход (за N минут)", Language = Constants.Ru)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.SECURITY, Name = Constants.SecuritySource)]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.DOUBLE)]
    [Description("Считает доход по бумаге за указанное количество минут.")]
    [HelperDescription("Calculates profit involving an instrument received during a specified number of minutes.", Constants.En)]
    public sealed class NumMinutesProfit : BasePeriodProfitHandler
    {
        public override double Execute(ISecurity source, int barNum)
        {
            var getProfitFunc = GetProfitFunc();
            var endDate = source.Bars[barNum].Date;
            var startDate = endDate.AddMinutes(-Period);

            return source.Positions.GetClosedOrActiveForBar(barNum)
#pragma warning disable 612
                         .Where(pos => pos.IsActive || (pos.ExitBar.Date >= startDate && pos.EntryBar.Date < endDate))
#pragma warning restore 612
                         .Sum(pos => getProfitFunc(pos, barNum));
        }
    }

    [HandlerCategory(HandlerCategories.Portfolio)]
    [HelperName("Profit (in N positions)", Language = Constants.En)]
    [HelperName("Доход (за N позиций)", Language = Constants.Ru)]
    [InputsCount(1)]
    [Input(0, TemplateTypes.SECURITY, Name = Constants.SecuritySource)]
    [OutputsCount(1)]
    [OutputType(TemplateTypes.DOUBLE)]
    [Description("Считает доход по бумаге за указанное количество позиций.")]
    [HelperDescription("Calculates profit involving an instrument received during a specified number of positions.", Constants.En)]
    public sealed class NumPositionsProfit : BasePeriodProfitHandler
    {
        public override double Execute(ISecurity source, int barNum)
        {
            var getProfitFunc = GetProfitFunc();
            double profit = 0;
            double trades = Period;

            foreach (var pos in source.Positions.OrderByDescending(pos => pos.ExitBarNum))
            {
                profit += getProfitFunc(pos, barNum);
                if (--trades <= 0)
                {
                    break;
                }
            }
            return profit;
        }
    }
}
