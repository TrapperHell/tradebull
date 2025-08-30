using FluentValidation;
using TradeBull.Models;
using TradeBull.Models.Requests;

namespace TradeBull.Validation
{
    public class TradeRequestValidator : AbstractValidator<TradeRequest>
    {
        public TradeRequestValidator()
        {
            RuleFor(x => x.Quantity).GreaterThan(0);

            RuleFor(x => x.RequestedSharePrice).Null()
                .When(x => x.Condition == TradeCondition.Current)
                .WithMessage("A share price cannot be specified when trading at the current market value.");

            RuleFor(x => x.Condition).Equal(TradeCondition.Current)
                .WithMessage("Fall / Rise trades are not supported at this time.");
        }
    }
}
