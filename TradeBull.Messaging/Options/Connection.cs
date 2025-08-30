using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace TradeBull.Messaging.Options
{
    public class Connection
    {
        public required string Uri { get; set; }

        [Required]
        public required string ExchangeName { get; set; }

        [Required]
        public required string QueueName { get; set; }

        [MaxLength(255)]
        public required string RoutingKey { get; set; }
    }

    [OptionsValidator]
    public partial class ConnectionValidation : IValidateOptions<Connection>
    {
        // Source generator will automatically provide the implementation of IValidateOptions
    }
}
