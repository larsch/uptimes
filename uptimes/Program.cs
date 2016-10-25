using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uptimes
{
    class Program
    {
        const string COLSEP = "   ";
        static ConsoleColor weekendBackgroundColor = Console.BackgroundColor;
        static ConsoleColor weekdayBackgroundColor = Console.BackgroundColor;
        static ConsoleColor weekendForegroundColor = ConsoleColor.Red;
        static ConsoleColor weekdayForegroundColor = ConsoleColor.Blue;
        static ConsoleColor standardForegroundColor = Console.ForegroundColor;
        static ConsoleColor standardBackgroundColor = Console.BackgroundColor;

        static void Main(string[] args)
        {
            var systemLog = GetSystemEventLog();
            PrintStartStop(systemLog);
        }

        private static EventLog GetSystemEventLog()
        {
            EventLog[] logs = EventLog.GetEventLogs();
            foreach (var log in logs)
            {
                if (log.Log == "System")
                {
                    return log;
                }
            }
            return null;
        }

        static private DateTime lastDate;

        private static void PrintDate(DateTime start, DateTime end, string message)
        {
            string shortDateString = start.DayOfWeek.ToString().Substring(0, 3) + " " + start.ToShortDateString();
            string endShortDateString = end.DayOfWeek.ToString().Substring(0, 3) + " " + end.ToShortDateString();


            SetColorFromDate(start);
            PrintWeekNumberPrefix(start);
            if (start.Date == lastDate.Date)
                Console.Out.Write(new string(' ', shortDateString.Length) + COLSEP);
            else
                Console.Out.Write(shortDateString + COLSEP);
            ResetColors();
            if (start.Date == end.Date)
            {
                Console.Out.Write(start.ToShortTimeString() + " - " + end.ToShortTimeString());
            }
            else
            {
                Console.Out.WriteLine(start.ToShortTimeString());
                SetColorFromDate(end);
                PrintWeekNumberPrefix(end);
                Console.Out.Write(endShortDateString + COLSEP);
                ResetColors();
                Console.Out.Write("         - " + end.ToShortTimeString());
            }
            Console.Out.WriteLine(COLSEP + "(" + message + ")");
            lastDate = end;
        }

        private static EventType GetEventType(EventLogEntry entry)
        {
            if (entry.Source == Source.KernelPower)
            {
                if (entry.InstanceId == InstanceId.EnteringSleep)
                    return EventType.EnteringSleep;
                else if (entry.InstanceId == InstanceId.ResumingFromSleep)
                    return EventType.ResumingFromSleep;
            }
            else if (entry.Source == Source.PowerTroubleshooter)
            {
                if (entry.InstanceId == InstanceId.ReturnFromLowPower)
                {
                    return EventType.ResumingFromSleep;
                }
            }
            else if (entry.Source == Source.KernelGeneral)
            {
                if (entry.InstanceId == InstanceId.StartUp)
                {
                    return EventType.StartingUp;
                }
                else if (entry.InstanceId == InstanceId.ShuttingDown)
                {
                    return EventType.ShuttingDown;
                }
                else if (entry.InstanceId == InstanceId.SystemTimeChanged)
                {
                    // system time change event immediately after waking from sleep
                    return EventType.SystemTimeChanged;
                }
            }
            return EventType.Other;
        }

        private static void PrintStartStop(EventLog systemLog)
        {
            EventLogEntryCollection entries = systemLog.Entries;
            EventLogEntry[] entriesArray = new EventLogEntry[entries.Count];
            entries.CopyTo(entriesArray, 0);
            DateTime startTime = entriesArray.First().TimeGenerated;
            bool started = true;
            bool sleeping = false;

            foreach (EventLogEntry entry in entriesArray)
            {
                switch (GetEventType(entry))
                {
                    case EventType.StartingUp:
                        if (started)
                        {
                            Program.PrintDate(startTime, entry.TimeGenerated, "unexpected restart");
                        }
                        startTime = entry.TimeGenerated;
                        started = true;
                        break;
                    case EventType.ShuttingDown:
                        Program.PrintDate(startTime, entry.TimeGenerated, "shutting down");
                        started = false;
                        break;
                    case EventType.EnteringSleep:
                        sleeping = true;
                        Program.PrintDate(startTime, entry.TimeGenerated, "going to sleep");
                        started = false;
                        break;
                    case EventType.ResumingFromSleep:
                        sleeping = false;
                        startTime = entry.TimeGenerated;
                        started = true;
                        break;
                    case EventType.SystemTimeChanged:
                        if (sleeping)
                        {
                            sleeping = false;
                            startTime = entry.TimeGenerated;
                        }
                        break;
                }
            }
            Program.PrintDate(startTime, DateTime.Now, "still running");
        }

        private static void SetColorFromDate(DateTime start)
        {
            return;
            bool isWeekend = (start.DayOfWeek == DayOfWeek.Saturday || start.DayOfWeek == DayOfWeek.Sunday);
            Console.ForegroundColor = isWeekend ? weekendForegroundColor : weekdayForegroundColor;
            Console.BackgroundColor = isWeekend ? weekendBackgroundColor : weekdayBackgroundColor;
        }

        private static void ResetColors()
        {
            Console.BackgroundColor = standardBackgroundColor;
            Console.ForegroundColor = standardForegroundColor;
        }

        private static void PrintWeekNumberPrefix(DateTime start)
        {
            DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
            DateTime date1 = new DateTime(2011, 1, 1);
            Calendar cal = dfi.Calendar;
            Int32 startWeekOfYear = cal.GetWeekOfYear(start, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);
            Int32 lastWeekOfYear = cal.GetWeekOfYear(lastDate, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);
            string s = String.Format("Week {0,2:d}" + COLSEP, startWeekOfYear);
            if (startWeekOfYear == lastWeekOfYear)
            {
                Console.Out.Write(new string(' ', s.Length));
            }
            else
            {
                Console.Out.Write(s);
            }
        }

    }
}
