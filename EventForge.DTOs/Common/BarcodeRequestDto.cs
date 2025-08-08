using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Common
{
    public class BarcodeRequestDto
    {
        [Required]
        [StringLength(1000, MinimumLength = 1)]
        public string Data { get; set; } = string.Empty;

        public BarcodeType BarcodeType { get; set; } = BarcodeType.QRCode;

        public int Width { get; set; } = 300;

        public int Height { get; set; } = 300;

        public ImageFormat ImageFormat { get; set; } = ImageFormat.PNG;
    }

    public enum BarcodeType
    {
        QRCode,
        Code128,
        Code39,
        EAN13,
        EAN8,
        Code93,
        Code11,
        Codabar,
        Code25,
        Interleaved25,
        Code39Extended,
        Code93Extended,
        EAN128,
        EAN14
    }

    public enum ImageFormat
    {
        PNG,
        JPEG,
        BMP,
        GIF
    }
}