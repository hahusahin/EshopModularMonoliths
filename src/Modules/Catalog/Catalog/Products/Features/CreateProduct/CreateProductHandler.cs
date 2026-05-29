namespace Catalog.Products.Features.CreateProduct;
public record CreateProductResult(Guid Id);

public record CreateProductCommand(ProductDto Product) : ICommand<CreateProductResult>;

internal class CreateProductHandler(CatalogDbContext dbContext)
    : ICommandHandler<CreateProductCommand, CreateProductResult>
{
    public async Task<CreateProductResult> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        //create Product entity from command object
        var product = CreateProduct(command.Product);
        //save to database
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        //return result
        return new CreateProductResult(product.Id);
    }

    private Product CreateProduct(ProductDto productDto)
    {
        var product = Product.Create(
            Guid.NewGuid(),
            productDto.Name,
            productDto.Category,
            productDto.Description,
            productDto.ImageFile,
            productDto.Price);

        return product;
    }
}

