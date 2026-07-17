namespace KalshiSharp.Models.Responses
{
    public sealed record AmendOrderResponse
    {
        /// <summary>
        /// The order before amendment
        /// </summary>
        public required OrderResponse OldOrder { get; set; }

        /// <summary>
        /// The order after amendment
        /// </summary>
        public required OrderResponse Order { get; set; }
    }
}
