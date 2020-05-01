using System;
using System.Globalization;
using System.Text.Json;

namespace selfish
{
    internal class Time
    {
        public int StartTime { get; private set; }
        public int EndTime { get; private set; }
        public int? TrueStartTime { get; private set; }
        public int? TrueEndTime { get; private set; }

        public static Time Create(JsonElement element)
        {
            static int? GetHour(JsonElement hour) =>
                hour.ValueKind == JsonValueKind.Null ? (int?)null : DateTime.ParseExact(hour.GetString(), "HH:mm:ss", CultureInfo.InvariantCulture).Hour;

            return new Time
            {
                StartTime = GetHour(element.GetProperty("StartTime")).Value,
                EndTime = GetHour(element.GetProperty("EndTime")).Value,
                TrueStartTime = GetHour(element.GetProperty("TrueStartTime")),
                TrueEndTime = GetHour(element.GetProperty("TrueEndTime"))
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
