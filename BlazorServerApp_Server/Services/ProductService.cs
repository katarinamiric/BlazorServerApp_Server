using BlazorServerApp_Server.Data;
using BlazorServerApp_Server.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace BlazorServerApp_Server.Services
{
    public class ProductService
    {
        private readonly ApplicationDbContext _context;

        public ProductService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Product?> GetProductByIdAsync(int productId)
        {
            await Task.Delay(2000);

            var product = await _context.Products
                .Include(p => p.RelatedProductPairs)
                    .ThenInclude(prp => prp.RelatedProduct)
                .FirstOrDefaultAsync(p => p.Id == productId);

            return product;
        }

        public async Task<List<Product>> GetRelatedProductsAsync(int productId, int count = 4)
        {
            await Task.Delay(500);

            var product = await _context.Products
                .Include(p => p.RelatedProductPairs)
                    .ThenInclude(prp => prp.RelatedProduct)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return new List<Product>();
            }

            var relatedProducts = product.RelatedProductPairs
                .Select(prp => prp.RelatedProduct)
                .Where(rp => rp.Id != productId)
                .DistinctBy(rp => rp.Id)
                .Take(count)
                .ToList();

            return relatedProducts;
        }
        public async Task<List<Product>> GetAllProductsAsync()
        {
            await Task.Delay(500);
            return await _context.Products.ToListAsync();
        }

        public async Task AddProductAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
        }

        public async Task AddRelatedProductPairAsync(int productId, int relatedProductId)
        {
            var existingPair = await _context.ProductRelatedProducts
                .AnyAsync(prp => (prp.ProductId == productId && prp.RelatedProductId == relatedProductId) ||
                                 (prp.ProductId == relatedProductId && prp.RelatedProductId == productId));

            //cant relate product to itself
            if (!existingPair && productId != relatedProductId)
            {
                _context.ProductRelatedProducts.Add(new ProductRelatedProduct
                {
                    ProductId = productId,
                    RelatedProductId = relatedProductId
                });
                await _context.SaveChangesAsync();
            }
        }
    }
}