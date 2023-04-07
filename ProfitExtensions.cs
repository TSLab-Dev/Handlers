using System.Collections.Generic;
using System.Linq;

namespace TSLab.Script.Handlers
{
    internal class WholeProfitState
    {
        private readonly IPositionsList m_list;

        private int m_lastBar;

        public IList<IPosition> Active { get; private set; } = new List<IPosition>();

        private int m_positionCount;

        public double ClosedProfitCache { get; private set; }

        public WholeProfitState(IPositionsList list)
        {
            m_list = list;
        }

        public void ProcessBar(int barNum)
        {
            if (m_positionCount != m_list.PositionCount)
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
                if (pos.EntryBarNum > barNum)
                    continue;
#pragma warning disable CS0612
                if (pos.IsActive || pos.ExitBarNum > barNum)
#pragma warning restore CS0612
                {
                    Active.Add(pos);
                    continue;
                }

                closedCount++;
                ClosedProfitCache += pos.Profit();
            }

            m_positionCount = m_list.PositionCount;
            m_lastBar = Active.Count + closedCount == m_positionCount ? barNum : 0;
        }
    }

    internal static class ProfitExtensions
    {
        public static double GetProfit(this WholeProfitState state, int barNum)
        {
            var result = state.Active.Sum(p => p.CurrentProfit(barNum)) + state.ClosedProfitCache;
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
