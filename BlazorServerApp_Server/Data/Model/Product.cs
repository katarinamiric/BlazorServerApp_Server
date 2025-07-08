using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlazorServerApp_Server.Data.Model
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty; // Long description for content

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public string ImageUrl { get; set; } = string.Empty; // Main product image

        public List<string> AdditionalImageUrls { get; set; } = new List<string>(); // Multiple images

        public string Brand { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public double Rating { get; set; } // For star ratings

        public int ReviewsCount { get; set; }

        // Properties for making it "heavy"
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