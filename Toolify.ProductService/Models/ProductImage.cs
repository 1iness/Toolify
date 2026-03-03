namespace Toolify.ProductService.Models
{
    public class ProductImage
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public byte[] ImageData { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = "image/jpeg"; 
        public bool IsMain { get; set; }
    }
}