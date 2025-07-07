namespace BlazorServerApp_Server.Data.Model
{
    public class ProductModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> ImageUrls { get; set; }
        public Dictionary<string, string> Specifications { get; set; }
        public List<ReviewModel> Reviews { get; set; }
        public List<RelatedProductModel> RelatedProducts { get; set; }
    }

    public class ReviewModel
    {
        public string Author { get; set; }
        public string Content { get; set; }
    }

    public class RelatedProductModel
    {
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public decimal Price { get; set; }
    }

}
