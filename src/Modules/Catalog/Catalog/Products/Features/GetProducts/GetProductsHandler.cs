namespace Catalog.Products.Features.GetProducts;

public record GetProductsResult(IEnumerable<ProductDto> Products);

public record GetProductsQuery() : IQuery<GetProductsResult>;

internal class GetProductsHandler(CatalogDbContext dbContext) : IQueryHandler<GetProductsQuery, GetProductsResult>
{
    public async Task<GetProductsResult> Handle(GetProductsQuery query, CancellationToken cancellationToken)
    {
        // get products from db
        var products = await dbContext.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        // map product entity to product DTO using Mabster
        var productDtos = products.Adapt<List<ProductDto>>();

        // return the result
        return new GetProductsResult(productDtos);
    }
}
