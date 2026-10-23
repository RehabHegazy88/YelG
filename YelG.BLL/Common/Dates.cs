namespace YelG.BLL.Common
{
    public static class Dates
    {
        /// <summary>
        /// The Basic base entities store AddedDate / LastModifiedDate as UTC DateTime (the interceptor
        /// writes them); EF reads them back without a kind. This makes them exact moments for the screens.
        /// </summary>
        public static DateTimeOffset AsUtc(this DateTime value) =>
            new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}
