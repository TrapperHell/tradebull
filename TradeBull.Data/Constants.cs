namespace TradeBull.Data
{
    // HACK: This should not be needed in a live environment
    public static class Constants
    {
        public static readonly Guid DefaultUserId = Guid.Parse("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA");

        public const string DefaultMarketName = "TSE";

        public const decimal TradeFlatFee = 1.49m;
    }
}
