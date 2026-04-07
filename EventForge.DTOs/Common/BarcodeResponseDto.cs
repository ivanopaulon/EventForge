namespace EventForge.DTOs.Common
{
    public class BarcodeResponseDto
    {
        public string Base64Image { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public int Width { get; set; }
        public int Height { get; set; }
        public BarcodeType BarcodeType { get; set; }
        public string Data { get; set; } = string.Empty;
    }
}