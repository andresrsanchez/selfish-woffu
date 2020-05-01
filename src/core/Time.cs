using System;
using System.Globalization;

namespace core
{
    internal class Time
    {
        private Time() { }
        public int StartTime { get; private set; }
        public int EndTime { get; private set; }
        public int? TrueStartTime { get; private set; }
        public int? TrueEndTime { get; private set; }

        public static Time Create(string startTime, string endTime, string trueStartTime, string trueEndTime)
        {
            if (string.IsNullOrWhiteSpace(startTime)) throw new ArgumentException(nameof(startTime));
            if (string.IsNullOrWhiteSpace(endTime)) throw new ArgumentException(nameof(endTime));

            static int? GetHour(string hour) =>
                hour == null ? (int?)null : DateTime.ParseExact(hour, "HH:mm:ss", CultureInfo.InvariantCulture).Hour;

            return new Time
            {
                StartTime = GetHour(startTime).Value,
                EndTime = GetHour(endTime).Value,
                TrueStartTime = GetHour(trueStartTime),
                TrueEndTime = GetHour(trueEndTime)
            };
        }

        public DateTime GetDateToSign()
        {
            if (TrueStartTime.HasValue && TrueEndTime.HasValue) throw new InvalidOperationException("At the moment we don't support sign more than 2 times");
            var hour = TrueStartTime.HasValue ? EndTime : StartTime;

            var now = DateTime.Now;
            return new DateTime(now.Year, now.Month, now.Day, hour, now.Minute, now.Second);
        }
    }
}
