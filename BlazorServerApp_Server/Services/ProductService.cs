using BlazorServerApp_Server.Data;
using BlazorServerApp_Server.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace BlazorServerApp_Server.Services
{
    public class ProductService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

        public ProductService(IDbContextFactory<ApplicationDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }
        public async Task<Product?> GetProductByIdAsync(int productId)
        {
            await Task.Delay(2000);
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var product = await context.Products
                .Include(p => p.RelatedProductPairs)
                .ThenInclude(prp => prp.RelatedProduct)
                .FirstOrDefaultAsync(p => p.Id == productId);

            return product;
        }
        public async Task<List<Product>> GetProductsAsync(string? categoryPath = null)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var query = context.Products
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(categoryPath))
            {
                var parts = categoryPath.Split('/');
                if (parts.Length == 2)
                {
                    // Handle sub-categories like "women/shoes"
                    var parentName = parts[0];
                    var subName = parts[1];
                    query = query.Where(p => p.Category.ParentCategory!.Name  == parentName &&
                                             p.Category.Name==subName);
                }
                else
                {
                    // Handle top-level categories like "women" or "shoes"
                    query = query.Where(p => p.Category.Name==categoryPath ||
                                             p.Category.ParentCategory!.Name == categoryPath);
                }
            }

            return await query.ToListAsync();
        }
        public async Task<List<Product>> GetRelatedProductsAsync(int productId, int count = 4)
        {
            await Task.Delay(500);
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var product = await context.Products
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
            using var context = await _dbContextFactory.CreateDbContextAsync();

            await Task.Delay(500);
            return await context.Products.ToListAsync();
        }

        public async Task<List<Product>> GetDiscountedProductsAsync(string category)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var query = context.Products
                .Include(p => p.Category)
                .Where(p => p.DiscountedPrice.HasValue) // Check if the product has a discounted price
                .Where(p => p.Category.Name==category ||
                            p.Category.ParentCategory!.Name==category);

            return await query.ToListAsync();
        }
    }
}