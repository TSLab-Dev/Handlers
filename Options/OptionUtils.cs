using System;
using System.Collections.Generic;

using TSLab.Script.Options;

namespace TSLab.Script.Handlers.Options
{
    /// <summary>
    /// \~english Static class with options utilities: time to expiry, theor price, IV etc.
    /// \~russian Статический класс с опционными утилитами: расчет времени до экспирации, теоретическая цена, волатильность и т.п.
    /// </summary>
    public static class OptionUtils
    {
        /// <summary> \~english Minutes in day (1440) \~russian Минут в сутках (1440)</summary>
        public const double MinutesInDay = 1440.0;
        ///// <summary> \~english 12*60 + 92 = 812 trading minutes in FORTS day \~russian 12*60 + 92 = 812 торговых минут в сутках ФОРТС</summary>
        //public const int TradingMinutesInDayRtsOld = (8 * 60 + 42) + (4 * 60 + 50);
        /// <summary> \~english 17*60 - 90 = 990 trading minutes in FORTS day \~russian 17*60 - 90 = 990 торговых минут в сутках ФОРТС</summary>
        public const int TradingMinutesInDayRts = (17 * 60 - 5 - 15 - 10);

        /// <summary> \~english Days in year (365.242) \~russian Дней в году (365.242)</summary>
        public const double DaysInYear = MinutesInYear / MinutesInDay;
        /// <summary>
        /// \~english Minutes in year (365 days 5 hours 48 minutes and 46 seconds == 525948.77 minutes)
        /// \~russian Минут в году (365 дней 5 часов 48 минут и 46 секунд == 525948.77 минут)
        /// </summary>
        public const double MinutesInYear = 365.0 * MinutesInDay + 5.0 * 60.0 + 48.0 + 46.0 / 60.0;

        /// <summary> \~english Ticks in day (864000000000) \~russian Тиков в сутках (864000000000)</summary>
        public static readonly long TradingTicksInDay = new TimeSpan(0, (int)MinutesInDay, 0).Ticks;

        ///// <summary>
        ///// \~english 12*60 + 92 = 812 trading ticks in FORTS day
        ///// \~russian 12*60 + 92 = 812 торговых тиков в сутках ФОРТС
        ///// </summary>
        //public static readonly long TradingTicksInDayRtsOld = new TimeSpan(0, TradingMinutesInDayRtsOld, 0).Ticks;
        /// <summary>
        /// \~english 17*60 - 90 = 990 trading ticks in FORTS day
        /// \~russian 17*60 - 90 = 990 торговых тиков в сутках ФОРТС
        /// </summary>
        public static readonly long TradingTicksInDayRts = new TimeSpan(0, TradingMinutesInDayRts, 0).Ticks;

        /// <summary>
        /// Чтобы не зависеть от зоны времени считаю временем смены расписания полночь между субботой 27.2.2021 и
        /// воскресеньем 28.2.2021 (оба выходные дни).
        /// </summary>
        private static readonly DateTime FortsScheduleChange2021 = new DateTime(2021, 2, 28);

        /// <summary>
        /// Старое расписание ФОРТС, которое действовало в период 2014-2021 год.
        /// </summary>
        private static readonly RtsDayTradingSchedule RtsOld;
        /// <summary>
        /// Новое расписание ФОРТС, которое действовало с марта 2021 года.
        /// </summary>
        private static readonly RtsDayTradingSchedule RtsNew;

        static OptionUtils()
        {
            // 1. Инициализирую кеш для старого расписания торгов
            RtsOld = RtsDayTradingSchedule.RtsScheduleOld();

            string txtOld = "";
            for (int year = 2010; year <= DateTime.Now.Year + 2; year++)
            {
                txtOld += year + "; " + RtsOld.GetTicksInYear(year);
            }

            // 3. инициализация кеша для нового расписания торгов
            RtsNew = RtsDayTradingSchedule.RtsScheduleNew();

            string txtNew = "";
            for (int year = 2020; year <= DateTime.Now.Year + 20; year++)
            {
                txtNew += year + "; " + RtsNew.GetTicksInYear(year);
            }
        }

        /// <summary>
        /// Класс для хранения расписания торгового дня ФОРТС В ТИКАХ.
        /// Предполагается начало торгов, дневной и вечерний клиринг, окончание торгов.
        /// </summary>
        private class RtsDayTradingSchedule
        {
            public string Name { get; private set; }

            public long RtsMorningTicks { get; private set; }
            public long RtsDayClearingTicks { get; private set; }
            public long RtsDayClearingEndTicks { get; private set; }
            public long RtsClearingTicks { get; private set; }
            public long RtsEveningTicks { get; private set; }
            public long RtsEodTicks { get; private set; }

            public int TradingMinutesInDayRts { get; private set; }
            public long TradingTicksInDayRts { get; private set; }

            private readonly object m_ticksInYearSyncObj = new object();
            /// <summary>
            /// Кеш тиков в одном торговом годе при использовании текущего расписания ФОРТС.
            /// Ключ - торговый год, значение -- количество торговых тиков в этом году.
            /// </summary>
            private readonly Dictionary<int, long> m_ticksInYear = new Dictionary<int, long>();

            private RtsDayTradingSchedule()
            {
            }

            /// <summary>
            /// При желании можно создавать свои собственные расписания
            /// </summary>
            public RtsDayTradingSchedule(string name, long morning, long dayClearingBeg, long dayClearingEnd,
                long eveningClearingBeg, long eveningClearingEnd, long eod)
            {
                Name = name;

                RtsMorningTicks = morning;
                RtsDayClearingTicks = dayClearingBeg;
                RtsDayClearingEndTicks = dayClearingEnd;
                RtsClearingTicks = eveningClearingBeg;
                RtsEveningTicks = eveningClearingEnd;
                RtsEodTicks = eod;
            }

            /// <summary>
            /// Количество торговых тиков в указанном торговом году с учетом текущего дневного расписания торгов ФОРТС
            /// </summary>
            /// <param name="year">торговый год</param>
            /// <returns>количество торговых тиков в указанном торговом году</returns>
            public long GetTicksInYear(int year)
            {
                long ticks;
                if (m_ticksInYear.TryGetValue(year, out ticks))
                    return ticks;
                else
                {
                    lock (m_ticksInYearSyncObj)
                    {
                        var beg = new DateTime(year, 1, 1);
                        var end = new DateTime(year + 1, 1, 1);
                        ticks = GetDtRtsTradingTimeNew(end, beg, this);
                        m_ticksInYear[year] = ticks;
                        return ticks;
                    }
                }
            }

            /// <summary>
            /// Старое расписание ФОРТС, которое действовало в период 2014-2021 год.
            /// </summary>
            /// <returns>cтарое расписание ФОРТС, которое действовало в период 2014-2021 год</returns>
            public static RtsDayTradingSchedule RtsScheduleOld()
            {
                var res = new RtsDayTradingSchedule
                {
                    Name = "BeforeMarch2021",

                    RtsMorningTicks = new TimeSpan(10, 0, 0).Ticks,
                    RtsDayClearingTicks = new TimeSpan(14, 0, 0).Ticks,
                    RtsDayClearingEndTicks = new TimeSpan(14, 3, 0).Ticks,
                    RtsClearingTicks = new TimeSpan(18, 45, 0).Ticks,
                    RtsEveningTicks = new TimeSpan(19, 0, 0).Ticks,
                    RtsEodTicks = new TimeSpan(23, 50, 0).Ticks,

                    TradingMinutesInDayRts = (8 * 60 + 42) + (4 * 60 + 50)
                };
                res.TradingTicksInDayRts = new TimeSpan(0, res.TradingMinutesInDayRts, 0).Ticks;

                return res;
            }

            /// <summary>
            /// Новое расписание ФОРТС, которое действовало с марта 2021 года.
            /// </summary>
            /// <returns>новое расписание ФОРТС, которое действовало с марта 2021 года</returns>
            public static RtsDayTradingSchedule RtsScheduleNew()
            {
                var res = new RtsDayTradingSchedule
                {
                    Name = "AfterMarch2021",
                    RtsMorningTicks = new TimeSpan(7, 0, 0).Ticks,
                    RtsDayClearingTicks = new TimeSpan(14, 0, 0).Ticks,
                    RtsDayClearingEndTicks = new TimeSpan(14, 5, 0).Ticks, // дневной клиринг давно длится по 5 минут с 14:00 по 14:05
                    RtsClearingTicks = new TimeSpan(18, 45, 0).Ticks,
                    RtsEveningTicks = new TimeSpan(19, 0, 0).Ticks,
                    RtsEodTicks = new TimeSpan(23, 50, 0).Ticks,

                    TradingMinutesInDayRts = (17 * 60 - 5 - 15 - 10)
                };
                res.TradingTicksInDayRts = new TimeSpan(0, res.TradingMinutesInDayRts, 0).Ticks;

                return res;
            }

            public override string ToString()
            {
                return Name;
            }
        }

        /// <summary>
        /// Интервал времени в единицах торгового года, торговых днях и торговых тиках
        /// </summary>
        public struct TradingTimeInterval
        {
            /// <summary>Торговое время в долях года</summary>
            public readonly double DtYears;
            /// <summary>Время в торговых днях</summary>
            public readonly double DtDays;
            /// <summary>Время в торговых тиках</summary>
            public readonly long TotalTicks;

            /// <summary>Количество тиков в полном торговом году</summary>
            public readonly long TicksInYear;
            /// <summary>Количество тиков в полном торговом дне</summary>
            public readonly long TicksInDay;

            public TradingTimeInterval(double years, double days, long ticks,
                long ticksInYear, long ticksInDay)
            {
                DtYears = years;
                DtDays = days;
                TotalTicks = ticks;

                TicksInYear = ticksInYear;
                TicksInDay = ticksInDay;
            }

            /// <summary>
            /// Смена знака торгового интервала на противоположный
            /// </summary>
            /// <param name="x">исходный торговый интервал</param>
            /// <returns>торговый интервал с противоположным знаком</returns>
            public static TradingTimeInterval Negative(TradingTimeInterval x)
            {
                var res = new TradingTimeInterval(-x.DtYears, -x.DtDays, -x.TotalTicks, x.TicksInYear, x.TicksInDay);
                return res;
            }
        }

        /// <summary>
        /// \~english 
        /// \~russian Расширение для класса TimeSpan, которое позволяет вычислять время между двумя датами в долях года
        /// </summary>
        public static double TotalYears(this TimeSpan ts)
        {
            double res = ts.TotalMinutes / MinutesInYear;
            return res;
        }

        /// <summary>
        /// \~russian Вычисление времени между двумя датами в долях года
        /// </summary>
        /// <param name="end">конечная дата</param>
        /// <param name="beg">начальная дата</param>
        public static double YearsBetweenDates(DateTime end, DateTime beg)
        {
            double res = (end - beg).TotalYears();
            return res;
        }

        /// <summary>
        /// Вычисление времени между двумя датами. Суббота и воскресенье игнорируются.
        /// </summary>
        /// <param name="end">конечная дата</param>
        /// <param name="beg">начальная дата</param>
        //[Obsolete("Рекомендуется использовать быстрый метод GetDtWithoutWeekends")]
        public static TimeSpan GetDtWithoutWeekendsSlow(DateTime end, DateTime beg)
        {
            if (end == beg)
                return new TimeSpan(); // Совпадающие даты всегда имеют нулевое расстояние
            else if (end < beg)
                return -GetDtWithoutWeekendsSlow(beg, end); // При "неправильном" порядке дат делаю рекурсивный вызов

            long res = 0L;
            // 1. Гарантирую, что начало периода будет в следующий рабочий понедельник
            // (точнее, в полночь с воскресенья на пнд).
            if (beg.DayOfWeek == System.DayOfWeek.Saturday)
            {
                beg = beg.Date.AddDays(2); // выставляюсь на понедельник
                // Если теперь начало периода превышает его окончание, то возвращаю 0
                // поскольку это означает, что обе даты сидят в одной паре выходных
                if (beg >= end)
                    return new TimeSpan();
            }
            else if (beg.DayOfWeek == System.DayOfWeek.Sunday)
            {
                beg = beg.Date.AddDays(1); // выставляюсь на понедельник
                // Если теперь начало периода превышает его окончание, то возвращаю 0
                // поскольку это означает, что обе даты сидят в одной паре выходных
                if (beg >= end)
                    return new TimeSpan();
            }

            // 2. Гарантирую, что конец периода будет в следующий рабочий понедельник.
            if (end.DayOfWeek == System.DayOfWeek.Saturday)
                end = end.Date.AddDays(2); // выставляюсь на понедельник
            else if (end.DayOfWeek == System.DayOfWeek.Sunday)
                end = end.Date.AddDays(1); // выставляюсь на понедельник

            // 3. Медленный, но надёжный алгоритм -- итерации в цикле
            res = -beg.TimeOfDay.Ticks;
            beg = beg.Date;
            res += end.TimeOfDay.Ticks;
            end = end.Date;

            int dayCounter = 0;
            while (beg < end)
            {
                dayCounter++;
                beg = beg.AddDays(1);
                if (beg.DayOfWeek == System.DayOfWeek.Saturday)
                    beg = beg.AddDays(2);
                else if (beg.DayOfWeek == System.DayOfWeek.Sunday)
                    beg = beg.AddDays(1);
            }

            res += (dayCounter * TradingTicksInDay);

            return new TimeSpan(res);
        }

        /// <summary>
        /// Вычисление времени между двумя датами. Суббота и воскресенье игнорируются.
        /// </summary>
        /// <param name="end">конечная дата</param>
        /// <param name="beg">начальная дата</param>
        /// <param name="calendar">календарь. По умолчанию используется расписание торгов Московской биржи</param>
        public static TimeSpan GetDtWithoutHolidaysSlow(DateTime end, DateTime beg, ICalendar calendar = null)
        {
            if (end == beg)
                return new TimeSpan(); // Совпадающие даты всегда имеют нулевое расстояние
            else if (end < beg)
                return -GetDtWithoutWeekendsSlow(beg, end); // При "неправильном" порядке дат делаю рекурсивный вызов

            if (calendar == null)
                calendar = CalendarWithoutHolidays.Russia;

            long res = 0L;
            // 1. Гарантирую, что начало периода будет в следующий рабочий ДЕНЬ
            // (точнее, в полночь с воскресенья на пнд).
            //if (beg.DayOfWeek == System.DayOfWeek.Saturday)
            while (!calendar.IsWorkingDay(beg))
            {
                beg = beg.Date.AddDays(1); // выставляюсь на следующий календарный день
            }
            // Если теперь начало периода превышает его окончание, то возвращаю 0
            // поскольку это означает, что обе даты сидят в одном блоке праздников
            if (beg >= end)
                return new TimeSpan();

            // 2. Гарантирую, что конец периода будет в следующий рабочий ДЕНЬ
            //if (end.DayOfWeek == System.DayOfWeek.Saturday)
            while (!calendar.IsWorkingDay(end))
            {
                end = end.Date.AddDays(1); // выставляюсь на следующий календарный день
            }

            // 3. Медленный, но надёжный алгоритм -- итерации в цикле
            res = -beg.TimeOfDay.Ticks;
            beg = beg.Date;
            res += end.TimeOfDay.Ticks;
            end = end.Date;

            int dayCounter = 0;
            while (beg < end)
            {
                dayCounter++;
                beg = beg.AddDays(1);
                //if (beg.DayOfWeek == System.DayOfWeek.Saturday)
                while (!calendar.IsWorkingDay(beg))
                {
                    beg = beg.AddDays(1);
                }
            }

            res += (dayCounter * TradingTicksInDay);

            return new TimeSpan(res);
        }

        /// <summary>
        /// Вычисление фактического торгового времени между двумя датами по НОВОМУ расписанию ФОРТС,
        /// действующему с 1 марта 2021 года.
        /// </summary>
        /// <param name="end">конечная дата</param>
        /// <param name="beg">начальная дата</param>
        [Obsolete("Метода устарел и нужно переходить на использование более общего GetDtRtsTradingTime21")]
        public static TimeSpan GetDtRtsTradingTime(DateTime end, DateTime beg, ICalendar calendar = null)
        {
            long ticks = GetDtRtsTradingTimeNew(end, beg, RtsNew, calendar);
            TimeSpan res = new TimeSpan(ticks);
            return res;
        }

        /// <summary>
        /// Вычисление фактического торгового времени между двумя датами по СТАРОМУ расписанию ФОРТС,
        /// которое действовало на 14.11.2014 до 28.2.2021.
        /// </summary>
        /// <param name="end">конечная дата</param>
        /// <param name="beg">начальная дата</param>
        /// <returns>количество торговых тиков между этими двумя датами</returns>
        private static long GetDtRtsTradingTimeOld(DateTime end, DateTime beg, ICalendar calendar = null)
        {
            if (end == beg)
                return 0; // Совпадающие даты всегда имеют нулевое расстояние
            else if (end < beg)
            {
                // При "неправильном" порядке дат делаю рекурсивный вызов
                var tmp = -GetDtRtsTradingTimeOld(beg, end, calendar);
                return tmp;
            }

            if (calendar == null)
                calendar = CalendarWithoutHolidays.Russia;

            long res = 0L;
            // 1. Гарантирую, что начало периода будет в следующий рабочий ДЕНЬ
            // (точнее, в полночь с воскресенья на пнд).
            //if (beg.DayOfWeek == System.DayOfWeek.Saturday)
            while (!calendar.IsWorkingDay(beg))
            {
                beg = beg.Date.AddDays(1); // выставляюсь на следующий календарный день
            }

            // 2. Гарантирую, что конец периода будет в следующий рабочий ДЕНЬ
            //if (end.DayOfWeek == System.DayOfWeek.Saturday)
            while (!calendar.IsWorkingDay(end))
            {
                end = end.Date.AddDays(1); // выставляюсь на следующий календарный день
            }

            // 3. Гарантирую, что если начало периода лежит до 10:00, оно будет сдвинуто на 10:00
            //long rtsMorningTicks = (new TimeSpan(10, 0, 0)).Ticks;
            //long rtsDayClearingTicks = (new TimeSpan(14, 0, 0)).Ticks;
            //long rtsDayClearingEndTicks = (new TimeSpan(14, 3, 0)).Ticks;
            //long rtsClearingTicks = (new TimeSpan(18, 45, 0)).Ticks;
            //long rtsEveningTicks = (new TimeSpan(19, 0, 0)).Ticks;
            //long rtsEodTicks = (new TimeSpan(23, 50, 0)).Ticks;

            // 4. Медленный, но надёжный алгоритм -- итерации в цикле.
            #region 4.1. Сначала надо разобраться с левой границей
            {
                long begTimeOfDayTicks = beg.TimeOfDay.Ticks;
                if (begTimeOfDayTicks < RtsOld.RtsMorningTicks) // Если раннее утро -- не считаю вообще!
                {
                }
                else if (begTimeOfDayTicks <= RtsOld.RtsDayClearingTicks) // Кусочек времени от начала торгов
                {
                    res -= (begTimeOfDayTicks - RtsOld.RtsMorningTicks);
                }
                else if (begTimeOfDayTicks <= RtsOld.RtsDayClearingTicks)
                // Внутри дневного клиринга просто беру 4 часа утренней торговли
                {
                    res -= (RtsOld.RtsDayClearingTicks - RtsOld.RtsMorningTicks);
                }
                else if (begTimeOfDayTicks <= RtsOld.RtsClearingTicks)
                // Между дневным и вечерним клирингами беру 4 часа утренней торговли + хвостик от окончания дневного клиринга 
                {
                    res -= (RtsOld.RtsDayClearingTicks - RtsOld.RtsMorningTicks);
                    res -= (begTimeOfDayTicks - RtsOld.RtsDayClearingEndTicks);
                }
                else if (begTimeOfDayTicks <= RtsOld.RtsEveningTicks)
                // Внутри вечернего клиринга просто беру 4 часа утренней торговли + 4ч 42м дневной
                {
                    res -= (RtsOld.RtsDayClearingTicks - RtsOld.RtsMorningTicks);
                    res -= (RtsOld.RtsClearingTicks - RtsOld.RtsDayClearingEndTicks);
                }
                else if (begTimeOfDayTicks <= RtsOld.RtsEodTicks)
                // Внутри вечернего клиринга беру 4 часа утренней торговли + 4ч 42м дневной + хвостик от начала вечерки
                {
                    res -= (RtsOld.RtsDayClearingTicks - RtsOld.RtsMorningTicks);
                    res -= (RtsOld.RtsClearingTicks - RtsOld.RtsDayClearingEndTicks);
                    res -= (begTimeOfDayTicks - RtsOld.RtsEveningTicks);
                }
                else
                // Если торги уже закончились, то нужно взять полный день: 4 часа утренней торговли + 4ч 42м дневной + 4ч 50мин вечерки
                {
                    res -= (RtsOld.RtsDayClearingTicks - RtsOld.RtsMorningTicks);
                    res -= (RtsOld.RtsClearingTicks - RtsOld.RtsDayClearingEndTicks);
                    res -= (RtsOld.RtsEodTicks - RtsOld.RtsEveningTicks);
                }
            }
            #endregion 4.1. Сначала надо разобраться с левой границей
            beg = beg.Date;

            #region 4.2. Теперь надо разобраться с правой границей
            {
                long tmpRes = 0L;
                long endgTimeOfDayTicks = end.TimeOfDay.Ticks;
                if (endgTimeOfDayTicks < RtsOld.RtsMorningTicks) // Если раннее утро -- не считаю вообще!
                {
                }
                else if (endgTimeOfDayTicks <= RtsOld.RtsDayClearingTicks) // Кусочек времени от начала торгов
                {
                    tmpRes += (endgTimeOfDayTicks - RtsOld.RtsMorningTicks);
                }
                else if (endgTimeOfDayTicks <= RtsOld.RtsDayClearingTicks)
                // Внутри дневного клиринга просто беру 4 часа утренней торговли
                {
                    tmpRes += (RtsOld.RtsDayClearingTicks - RtsOld.RtsMorningTicks);
                }
                else if (endgTimeOfDayTicks <= RtsOld.RtsClearingTicks)
                // Между дневным и вечерним клирингами беру 4 часа утренней торговли + хвостик от окончания дневного клиринга 
                {
                    tmpRes += (RtsOld.RtsDayClearingTicks - RtsOld.RtsMorningTicks);
                    tmpRes += (endgTimeOfDayTicks - RtsOld.RtsDayClearingEndTicks);
                }
                else if (endgTimeOfDayTicks <= RtsOld.RtsEveningTicks)
                // Внутри вечернего клиринга просто беру 4 часа утренней торговли + 4ч 42м дневной
                {
                    tmpRes += (RtsOld.RtsDayClearingTicks - RtsOld.RtsMorningTicks);
                    tmpRes += (RtsOld.RtsClearingTicks - RtsOld.RtsDayClearingEndTicks);
                }
                else if (endgTimeOfDayTicks <= RtsOld.RtsEodTicks)
                // Внутри вечернего клиринга беру 4 часа утренней торговли + 4ч 42м дневной + хвостик от начала вечерки
                {
                    tmpRes += (RtsOld.RtsDayClearingTicks - RtsOld.RtsMorningTicks);
                    tmpRes += (RtsOld.RtsClearingTicks - RtsOld.RtsDayClearingEndTicks);
                    tmpRes += (endgTimeOfDayTicks - RtsOld.RtsEveningTicks);
                }
                else
                // Если торги уже закончились, то нужно взять полный день: 4 часа утренней торговли + 4ч 42м дневной + 4ч 50мин вечерки
                {
                    tmpRes += (RtsOld.RtsDayClearingTicks - RtsOld.RtsMorningTicks);
                    tmpRes += (RtsOld.RtsClearingTicks - RtsOld.RtsDayClearingEndTicks);
                    tmpRes += (RtsOld.RtsEodTicks - RtsOld.RtsEveningTicks);
                }

                res += tmpRes;
            }
            #endregion 4.2. Теперь надо разобраться с правой границей
            end = end.Date;

            int dayCounter = 0;
            while (beg < end)
            {
                dayCounter++;
                beg = beg.AddDays(1);
                //if (beg.DayOfWeek == System.DayOfWeek.Saturday)
                while (!calendar.IsWorkingDay(beg))
                {
                    beg = beg.AddDays(1);
                }
            }

            res += dayCounter * RtsOld.TradingTicksInDayRts;

            return res;
        }

        /// <summary>
        /// Вычисление фактического торгового времени между двумя датами по любому расписанию ФОРТС,
        /// которое передаётся в метод через аргумент dts.
        /// </summary>
        /// <param name="end">конечная дата</param>
        /// <param name="beg">начальная дата</param>
        /// <param name="dts">торговое расписание обычного торгового дня</param>
        /// <returns>количество торговых тиков между этими двумя датами</returns>
        private static long GetDtRtsTradingTimeNew(DateTime end, DateTime beg, RtsDayTradingSchedule dts, ICalendar calendar = null)
        {
            if (end == beg)
                return 0; // Совпадающие даты всегда имеют нулевое расстояние
            else if (end < beg)
            {
                // При "неправильном" порядке дат делаю рекурсивный вызов
                var tmp = -GetDtRtsTradingTimeNew(beg, end, dts, calendar);
                return tmp;
            }

            if (calendar == null)
                calendar = CalendarWithoutHolidays.Russia;

            long res = 0L;
            // 1. Гарантирую, что начало периода будет в следующий рабочий ДЕНЬ
            // (точнее, в полночь с воскресенья на пнд).
            //if (beg.DayOfWeek == System.DayOfWeek.Saturday)
            while (!calendar.IsWorkingDay(beg))
            {
                beg = beg.Date.AddDays(1); // выставляюсь на следующий календарный день
            }

            // 2. Гарантирую, что конец периода будет в следующий рабочий ДЕНЬ
            //if (end.DayOfWeek == System.DayOfWeek.Saturday)
            while (!calendar.IsWorkingDay(end))
            {
                end = end.Date.AddDays(1); // выставляюсь на следующий календарный день
            }

            // 3. Гарантирую, что если начало периода лежит до 10:00, оно будет сдвинуто на 10:00
            //long rtsMorningTicks = (new TimeSpan(10, 0, 0)).Ticks;
            //long rtsDayClearingTicks = (new TimeSpan(14, 0, 0)).Ticks;
            //long rtsDayClearingEndTicks = (new TimeSpan(14, 3, 0)).Ticks;
            //long rtsClearingTicks = (new TimeSpan(18, 45, 0)).Ticks;
            //long rtsEveningTicks = (new TimeSpan(19, 0, 0)).Ticks;
            //long rtsEodTicks = (new TimeSpan(23, 50, 0)).Ticks;

            // 4. Медленный, но надёжный алгоритм -- итерации в цикле.
            #region 4.1. Сначала надо разобраться с левой границей
            {
                long begTimeOfDayTicks = beg.TimeOfDay.Ticks;
                if (begTimeOfDayTicks < dts.RtsMorningTicks) // Если раннее утро -- не считаю вообще!
                {
                }
                else if (begTimeOfDayTicks <= dts.RtsDayClearingTicks) // Кусочек времени от начала торгов
                {
                    res -= (begTimeOfDayTicks - dts.RtsMorningTicks);
                }
                else if (begTimeOfDayTicks <= dts.RtsDayClearingTicks)
                // Внутри дневного клиринга просто беру 4 часа утренней торговли
                {
                    res -= (dts.RtsDayClearingTicks - dts.RtsMorningTicks);
                }
                else if (begTimeOfDayTicks <= dts.RtsClearingTicks)
                // Между дневным и вечерним клирингами беру 4 часа утренней торговли + хвостик от окончания дневного клиринга 
                {
                    res -= (dts.RtsDayClearingTicks - dts.RtsMorningTicks);
                    res -= (begTimeOfDayTicks - dts.RtsDayClearingEndTicks);
                }
                else if (begTimeOfDayTicks <= dts.RtsEveningTicks)
                // Внутри вечернего клиринга просто беру 4 часа утренней торговли + 4ч 42м дневной
                {
                    res -= (dts.RtsDayClearingTicks - dts.RtsMorningTicks);
                    res -= (dts.RtsClearingTicks - dts.RtsDayClearingEndTicks);
                }
                else if (begTimeOfDayTicks <= dts.RtsEodTicks)
                // Внутри вечернего клиринга беру 4 часа утренней торговли + 4ч 42м дневной + хвостик от начала вечерки
                {
                    res -= (dts.RtsDayClearingTicks - dts.RtsMorningTicks);
                    res -= (dts.RtsClearingTicks - dts.RtsDayClearingEndTicks);
                    res -= (begTimeOfDayTicks - dts.RtsEveningTicks);
                }
                else
                // Если торги уже закончились, то нужно взять полный день: 4 часа утренней торговли + 4ч 42м дневной + 4ч 50мин вечерки
                {
                    res -= (dts.RtsDayClearingTicks - dts.RtsMorningTicks);
                    res -= (dts.RtsClearingTicks - dts.RtsDayClearingEndTicks);
                    res -= (dts.RtsEodTicks - dts.RtsEveningTicks);
                }
            }
            #endregion 4.1. Сначала надо разобраться с левой границей
            beg = beg.Date;

            #region 4.2. Теперь надо разобраться с правой границей
            {
                long tmpRes = 0L;
                long endgTimeOfDayTicks = end.TimeOfDay.Ticks;
                if (endgTimeOfDayTicks < dts.RtsMorningTicks) // Если раннее утро -- не считаю вообще!
                {
                }
                else if (endgTimeOfDayTicks <= dts.RtsDayClearingTicks) // Кусочек времени от начала торгов
                {
                    tmpRes += (endgTimeOfDayTicks - dts.RtsMorningTicks);
                }
                else if (endgTimeOfDayTicks <= dts.RtsDayClearingTicks)
                // Внутри дневного клиринга просто беру 4 часа утренней торговли
                {
                    tmpRes += (dts.RtsDayClearingTicks - dts.RtsMorningTicks);
                }
                else if (endgTimeOfDayTicks <= dts.RtsClearingTicks)
                // Между дневным и вечерним клирингами беру 4 часа утренней торговли + хвостик от окончания дневного клиринга 
                {
                    tmpRes += (dts.RtsDayClearingTicks - dts.RtsMorningTicks);
                    tmpRes += (endgTimeOfDayTicks - dts.RtsDayClearingEndTicks);
                }
                else if (endgTimeOfDayTicks <= dts.RtsEveningTicks)
                // Внутри вечернего клиринга просто беру 4 часа утренней торговли + 4ч 42м дневной
                {
                    tmpRes += (dts.RtsDayClearingTicks - dts.RtsMorningTicks);
                    tmpRes += (dts.RtsClearingTicks - dts.RtsDayClearingEndTicks);
                }
                else if (endgTimeOfDayTicks <= dts.RtsEodTicks)
                // Внутри вечернего клиринга беру 4 часа утренней торговли + 4ч 42м дневной + хвостик от начала вечерки
                {
                    tmpRes += (dts.RtsDayClearingTicks - dts.RtsMorningTicks);
                    tmpRes += (dts.RtsClearingTicks - dts.RtsDayClearingEndTicks);
                    tmpRes += (endgTimeOfDayTicks - dts.RtsEveningTicks);
                }
                else
                // Если торги уже закончились, то нужно взять полный день: 4 часа утренней торговли + 4ч 42м дневной + 4ч 50мин вечерки
                {
                    tmpRes += (dts.RtsDayClearingTicks - dts.RtsMorningTicks);
                    tmpRes += (dts.RtsClearingTicks - dts.RtsDayClearingEndTicks);
                    tmpRes += (dts.RtsEodTicks - dts.RtsEveningTicks);
                }

                res += tmpRes;
            }
            #endregion 4.2. Теперь надо разобраться с правой границей
            end = end.Date;

            int dayCounter = 0;
            while (beg < end)
            {
                dayCounter++;
                beg = beg.AddDays(1);
                //if (beg.DayOfWeek == System.DayOfWeek.Saturday)
                while (!calendar.IsWorkingDay(beg))
                {
                    beg = beg.AddDays(1);
                }
            }

            res += dayCounter * dts.TradingTicksInDayRts;

            return res;
        }

        /// <summary>
        /// Вычисление фактического торгового времени между двумя датами по расписанию ФОРТС
        /// с учетом изменений в режиме торгов в марте 2021 года.
        /// </summary>
        /// <param name="end">конечная дата</param>
        /// <param name="beg">начальная дата</param>
        public static TradingTimeInterval GetDtRtsTradingTime21(DateTime end, DateTime beg, ICalendar calendar = null)
        {
            if (end < beg)
            {
                var tmp = GetDtRtsTradingTime21(beg, end, calendar);
                var resNeg = TradingTimeInterval.Negative(tmp);
                return resNeg;
            }

            if (end < FortsScheduleChange2021)
            {
                long oldTicks = GetDtRtsTradingTimeOld(end, beg, calendar);
                long ticksInYear = RtsOld.GetTicksInYear(end.Year);
                long ticksInDay = RtsOld.TradingTicksInDayRts;
                double dtYears = (double)oldTicks / ticksInYear;
                double dtDays = (double)oldTicks / ticksInDay;
                TradingTimeInterval resOld = new TradingTimeInterval(dtYears, dtDays, oldTicks, ticksInYear, ticksInDay);
                return resOld;
            }
            else if (FortsScheduleChange2021 <= beg)
            {
                long newTicks = GetDtRtsTradingTimeNew(end, beg, RtsNew, calendar);
                long ticksInYear = RtsNew.GetTicksInYear(end.Year);
                long ticksInDay = RtsNew.TradingTicksInDayRts;
                double dtYears = (double)newTicks / ticksInYear;
                double dtDays = (double)newTicks / ticksInDay;
                TradingTimeInterval resNew = new TradingTimeInterval(dtYears, dtDays, newTicks, ticksInYear, ticksInDay);
                return resNew;
            }
            else
            {
                // 1. Вычисляем интервалы до 1 марта 2021
                long oldTicks = GetDtRtsTradingTimeOld(FortsScheduleChange2021, beg, calendar);
                long ticksInYearOld = RtsOld.GetTicksInYear(FortsScheduleChange2021.Year);
                long ticksInDayOld = RtsOld.TradingTicksInDayRts;
                double dtYearsOld = (double)oldTicks / ticksInYearOld;
                double dtDaysOld = (double)oldTicks / ticksInDayOld;
                //TradingTimeInterval resOld = new TradingTimeInterval(dtYearsOld, dtDaysOld, oldTicks, ticksInYearOld, ticksInDayOld);

                // 3. Вычисляем интервал после 1 марта 2021
                long newTicks = GetDtRtsTradingTimeNew(end, FortsScheduleChange2021, RtsNew, calendar);
                long ticksInYearNew = RtsNew.GetTicksInYear(end.Year);
                long ticksInDayNew = RtsNew.TradingTicksInDayRts;
                double dtYearsNew = (double)newTicks / ticksInYearNew;
                double dtDaysNew = (double)newTicks / ticksInDayNew;
                //TradingTimeInterval resNew = new TradingTimeInterval(dtYearsNew, dtDaysNew, newTicks, ticksInYearNew, ticksInDayNew);

                // 5. Комбинируем в один общий ответ
                // Сложнее всего определиться как считать количество тиков в полном торговом годе.
                // Скорее всего надо просто использовать новые аргументы.
                // Потому что именно они будут иметь максимальную практическую важность.
                TradingTimeInterval resMix = new TradingTimeInterval(
                    dtYearsOld + dtYearsNew, dtDaysOld + dtDaysNew, oldTicks + newTicks,
                    ticksInYearNew, ticksInDayNew);
                return resMix;
            }
        }

        /// <summary>
        /// Получить полное количество торговых дней в году с учетом их весов.
        /// (Результат кешируется в статической коллекции для ускорения последующих обращений)
        /// </summary>
        /// <param name="year">год, который нас интересует</param>
        /// <returns>полное количество торговых дней в году с учетом их весов</returns>
        public static double GetLiquidProRtsTradingDaysInYear(int year)
        {
            double daysInYear = LiquidProTimeModelRepository.GetDaysInYear(year);
            return daysInYear;
        }

        /// <summary>
        /// Вычисление фактического взвешенного торгового времени (по алгоритму Liquid.Pro)
        /// между двумя датами по расписанию ФОРТС на 19.09.2017.
        /// </summary>
        /// <param name="end">конечная дата</param>
        /// <param name="beg">начальная дата</param>
        /// <param name="daysinEndYear">количество дней в году, который указан в поздней из двух дат</param>
        public static double GetDtLiquidProRtsTradingTime(DateTime end, DateTime beg, out double daysInEndYear)
        {
            daysInEndYear = LiquidProTimeModelRepository.GetDaysInYear(end.Year);

            if (end == beg)
                return 0; // Совпадающие даты всегда имеют нулевое расстояние
            else if (end < beg)
                return -GetDtLiquidProRtsTradingTime(beg, end, out daysInEndYear); // При "неправильном" порядке дат делаю рекурсивный вызов

            double dT = LiquidProTimeModelRepository.GetYearPartBetween(beg, end);
            return dT;
        }
    }
}
