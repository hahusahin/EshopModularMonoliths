namespace Catalog.Products.Features.UpdateProduct;
public record UpdateProductResult(bool IsSuccess);

public record UpdateProductCommand(ProductDto Product) : ICommand<UpdateProductResult>;

internal class UpdateProductHandler(CatalogDbContext dbContext)
    : ICommandHandler<UpdateProductCommand, UpdateProductResult>
{
    public async Task<UpdateProductResult> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        //Update Product entity from command object
        var product = await dbContext.Products
          .FindAsync([command.Product.Id], cancellationToken: cancellationToken);

        if (product is null)
        {
            throw new Exception($"Product not found: {command.Product.Id}");
        }

        UpdateProductWithNewValues(product, command.Product);

        //save to database
        dbContext.Products.Update(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        //return result
        return new UpdateProductResult(true);
    }

    private void UpdateProductWithNewValues(Product product, ProductDto productDto)
    {
        product.Update(
            productDto.Name,
            productDto.Category,
            productDto.Description,
            productDto.ImageFile,
            productDto.Price);
    }
}