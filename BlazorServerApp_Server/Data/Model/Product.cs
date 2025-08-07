using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlazorServerApp_Server.Data.Model
{

    public class Category
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;
        public int? ParentCategoryId { get; set; }
        public virtual Category? ParentCategory { get; set; }
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
        public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();
    }
    public class Product
    {
            [Key]
            public int Id { get; set; }
            [Required]
            [MaxLength(100)]
            public string Name { get; set; } = string.Empty;
            [Required]
            public string Description { get; set; } = string.Empty;
            [Required]
            public decimal Price { get; set; }
            [Required]
            public string ImageUrl { get; set; } = string.Empty;
            [Required]
            [MaxLength(50)]
            public string Brand { get; set; } = string.Empty;
            public int CategoryId { get; set; }
            public decimal? DiscountedPrice { get; set; }



            public double Rating { get; set; } // For star ratings

            public int ReviewsCount { get; set; }
public virtual Category Category { get; set; } = default!;
            public List<string> AdditionalImageUrls { get; set; } = new List<string>(); // Multiple images
            public string LongFeatureList { get; set; } = string.Empty; // A very long string of features
            public string TechnicalSpecifications { get; set; } = string.Empty; // More detailed text

            // Navigation property for related products
            public virtual ICollection<ProductRelatedProduct> RelatedProductPairs { get; set; } = new List<ProductRelatedProduct>();
    } 

    // Junction table for many-to-many relationship for related products
    public class ProductRelatedProduct
    {
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int RelatedProductId { get; set; }
        public Product RelatedProduct { get; set; } = null!;
    }
}