using System;
using System.Collections.Generic;
using System.Linq;

namespace TSLab.Script.Handlers
{
    internal class WholeProfitState
    {
        public IList<IPosition> Active { get; private set; } = new List<IPosition>();

        public double ClosedProfitCache { get; private set; }

        public TradeDirection2 Direction { get; }

        private readonly IPositionsList m_list;
        
        private int m_lastBar;

        private int m_positionsCount;

        public WholeProfitState(IPositionsList list, TradeDirection2 direction = TradeDirection2.All)
        {
            m_list = list;
            Direction = direction;
        }

        public void ProcessBar(int barNum)
        {
            var currentPositionsCount = GetPositionsCount();
            if (m_positionsCount != currentPositionsCount)
            {
                m_lastBar = 0;
            }
            else
            {
                foreach (var position in Active)
                {
                    if (position.IsActiveForBar(barNum))
                        continue;
                    m_lastBar = 0;
                    break;
                }
            }

            if (m_lastBar != 0 && m_lastBar <= barNum)
                return;

            ClosedProfitCache = 0;
            Active.Clear();
            var closedCount = 0;
            foreach (var pos in m_list)
            {
                if (Direction == TradeDirection2.Buys && !pos.IsLong)
                    continue;
                if (Direction == TradeDirection2.Sells && !pos.IsShort)
                    continue;
                if (pos.EntryBarNum > barNum)
                {
                    currentPositionsCount--; //GLSP-3176
                    continue;
                }
                if (pos.IsActive || pos.ExitBarNum > barNum)
                {
                    Active.Add(pos);
                    continue;
                }

                closedCount++;
                ClosedProfitCache += pos.Profit();
            }

            m_positionsCount = currentPositionsCount;
            m_lastBar = Active.Count + (closedCount == m_positionsCount ? barNum : 0);
        }

        private int m_lastPositionsCount;
        private int m_positionsCountByDirection;
        private int GetPositionsCount()
        {
            switch (Direction)
            {
                case TradeDirection2.All:
                    return m_list.PositionCount;

                case TradeDirection2.Buys:
                    if (m_lastPositionsCount != m_list.PositionCount)
                    {
                        m_positionsCountByDirection = m_list.Count(x => x.IsLong);
                        m_lastPositionsCount = m_list.PositionCount;
                    }
                    return m_positionsCountByDirection;

                case TradeDirection2.Sells:
                    if (m_lastPositionsCount != m_list.PositionCount)
                    {
                        m_positionsCountByDirection = m_list.Count(x => x.IsShort);
                        m_lastPositionsCount = m_list.PositionCount;
                    }
                    return m_positionsCountByDirection;

                default:
                    throw new NotImplementedException();
            }
        }
    }

    internal static class ProfitExtensions
    {
        public static double GetProfit(this WholeProfitState state, int barNum)
        {
            var result = state.Active.Sum(p => p.CurrentProfit(barNum)) + state.ClosedProfitCache;
            return result;
        }

        public static double GetProfitMin(this WholeProfitState state, int barNum)
        {
            var result = state.Active.Sum(p => p.CurrentProfitMin(barNum)) + state.ClosedProfitCache;
            return result;
        }

        public static double GetProfit(this IEnumerable<IPosition> positions, int barNum)
        {
            var result = positions.Sum(item => item.GetProfit(barNum));
            return result;
        }

        public static double GetProfit(this IPosition position, int barNum)
        {
            var result = position.EntryBarNum > barNum ? 0 : position.IsActiveForBar(barNum) ? position.CurrentProfit(barNum) : position.Profit();
            return result;
        }

        public static double GetAccumulatedProfit(this WholeProfitState state, int barNum)
        {
            var result = state.Active.Sum(p => p.GetAccumulatedProfit(barNum)) + state.ClosedProfitCache;
            return result;
        }

        public static double GetAccumulatedProfit(this IPosition position, int barNum)
        {
            var result = position.EntryBarNum > barNum ? 0 : position.IsActiveForBar(barNum) ? position.GetAccumulatedProfit(barNum) : position.Profit();
            return result;
        }
    }
}
