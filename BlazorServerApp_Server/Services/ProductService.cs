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
            // Simulate a network/database delay to make the rendering "heavy"
            await Task.Delay(2000); // 2 seconds delay

            var product = await _context.Products
                .Include(p => p.RelatedProductPairs)
                    .ThenInclude(prp => prp.RelatedProduct) // Eager load related products
                .FirstOrDefaultAsync(p => p.Id == productId);

            return product;
        }

        public async Task<List<Product>> GetRelatedProductsAsync(int productId, int count = 4)
        {
            // Simulate a delay for related product fetching if it were a separate call
            await Task.Delay(500);

            // Fetch the product with its related products already loaded
            var product = await _context.Products
                .Include(p => p.RelatedProductPairs)
                    .ThenInclude(prp => prp.RelatedProduct)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return new List<Product>();
            }

            // Extract distinct related products, excluding the current product
            var relatedProducts = product.RelatedProductPairs
                .Select(prp => prp.RelatedProduct)
                .Where(rp => rp.Id != productId) // Ensure the product itself is not listed as related
                .DistinctBy(rp => rp.Id) // Ensure unique related products
                .Take(count)
                .ToList();

            // If not enough related products from the defined relationships,
            // you might want to fetch some other random products or from the same category.
            // For now, we'll just return what's directly related.

            return relatedProducts;
        }

        // Method to get a list of products (e.g., for related product suggestions if no direct relations exist)
        public async Task<List<Product>> GetAllProductsAsync()
        {
            await Task.Delay(500); // Simulate delay
            return await _context.Products.ToListAsync();
        }

        // You'll need methods to add/update products for seeding
        public async Task AddProductAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
        }

        public async Task AddRelatedProductPairAsync(int productId, int relatedProductId)
        {
            // Check if the relation already exists to prevent duplicates
            var existingPair = await _context.ProductRelatedProducts
                .AnyAsync(prp => (prp.ProductId == productId && prp.RelatedProductId == relatedProductId) ||
                                 (prp.ProductId == relatedProductId && prp.RelatedProductId == productId));

            if (!existingPair && productId != relatedProductId) // A product cannot be related to itself
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