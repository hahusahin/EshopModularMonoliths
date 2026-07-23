namespace Basket.Basket.Features.UpdateItemPriceInBasket;

public record UpdateItemPriceInBasketResult(bool IsSuccess);

public record UpdateItemPriceInBasketCommand(Guid ProductId, decimal Price)
    : ICommand<UpdateItemPriceInBasketResult>;

public class UpdateItemPriceInBasketCommandValidator : AbstractValidator<UpdateItemPriceInBasketCommand>
{
    public UpdateItemPriceInBasketCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("ProductId is required");
        RuleFor(x => x.Price).GreaterThan(0).WithMessage("Price must be greater than 0");
    }
}

internal class UpdateItemPriceInBasketHandler 
    (BasketDbContext dbContext)
    : ICommandHandler<UpdateItemPriceInBasketCommand, UpdateItemPriceInBasketResult>
{
    public async Task<UpdateItemPriceInBasketResult> Handle(UpdateItemPriceInBasketCommand command, CancellationToken cancellationToken)
    {
        //Find Shopping Cart Items with a given ProductId
        var itemsToUpdate = await dbContext.ShoppingCartItems
            .Where(item => item.ProductId == command.ProductId)
            .ToListAsync(cancellationToken);

        if (itemsToUpdate.Count == 0) {
            return new UpdateItemPriceInBasketResult(false);
        }

        //Iterate items and Update Price of every item with incoming command.Price
        foreach (var item in itemsToUpdate)
        {
            item.UpdatePrice(command.Price);
        }

        //save to database
        await dbContext.SaveChangesAsync(cancellationToken);

        //return result
        return new UpdateItemPriceInBasketResult(true);
    }
}
