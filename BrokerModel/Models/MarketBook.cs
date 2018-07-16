using System;

namespace Shared.Models
{
    public struct MarketBook
    {
        public DateTime LocalTime { get; set; }
        public DateTime UtcTime { get { return LocalToUtc(LocalTime); } }
        public DateTime ServerTime { get; set; }
        public double BidPrice { get; set; }
        public double AskPrice { get; set; }
        public double BidSize { get; set; }
        public double AskSize { get; set; }
        public double LastSize { get; set; }
        public double LastPrice { get; set; }
        public DateTime EstTime { get { return getShift(UtcTime);} }
        static public DateTime getShift(DateTime time) { return TimeZoneInfo.ConvertTimeFromUtc(time, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")); }
        static public DateTime LocalToUtc(DateTime theDateToChange)
        {
            DateTime localDateTime = DateTime.SpecifyKind(theDateToChange, DateTimeKind.Local);
            DateTime utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime, TimeZoneInfo.Local);
            return utcDateTime;
        }
    };
}
