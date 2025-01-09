namespace DbDemo.Extensions;

public static class DateTimeExtension
{
    public static string ToDbDateTimeString(this DateTime date) => date.ToString("yyyy-MM-dd HH:mm:ss");
    public static string ToDbDateString(this DateTime date) => date.ToString("yyyy-MM-dd");
    public static string ToUndashedDbDateString(this DateTime date) => date.ToString("yyyyMMdd");
    public static string ToTimeString(this DateTime date, bool is24h = true) => is24h ? date.ToString("HH:mm:ss") : date.ToString("h:mm:ss tt");

}
