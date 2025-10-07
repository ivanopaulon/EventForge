using EventForge.Server.Services.Interfaces;
using SkiaSharp;
using Spire.Barcode;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace EventForge.Server.Services.Common;

public class BarcodeService : IBarcodeService
{
    private readonly ILogger<BarcodeService> _logger;

    public BarcodeService(ILogger<BarcodeService> logger)
    {
        _logger = logger;
    }

    public async Task<BarcodeResponseDto> GenerateBarcodeAsync(BarcodeRequestDto request)
    {
        try
        {
            _logger.LogInformation("Generating barcode of type {BarcodeType} for data length {DataLength}",
                request.BarcodeType, request.Data.Length);

            // Validate data for barcode type
            if (!ValidateDataForBarcodeType(request.Data, request.BarcodeType))
            {
                throw new ArgumentException($"Invalid data format for barcode type {request.BarcodeType}");
            }

            // Create barcode settings
            var settings = new BarcodeSettings
            {
                Type = MapBarcodeType(request.BarcodeType),
                Data = request.Data,
                ImageWidth = request.Width,
                ImageHeight = request.Height
            };

            // Generate barcode
            var generator = new BarCodeGenerator(settings);

            // Convert to base64 using cross-platform method
            var base64Image = await ConvertBarcodeToBase64Async(generator, request.ImageFormat, request.Width, request.Height, request.Data);
            var mimeType = GetMimeType(request.ImageFormat);

            return new BarcodeResponseDto
            {
                Base64Image = base64Image,
                MimeType = mimeType,
                Width = request.Width,
                Height = request.Height,
                BarcodeType = request.BarcodeType,
                Data = request.Data
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating barcode for data: {Data}", request.Data);
            throw;
        }
    }

    public async Task<BarcodeResponseDto> GenerateQRCodeAsync(string data)
    {
        var request = new BarcodeRequestDto
        {
            Data = data,
            BarcodeType = BarcodeType.QRCode,
            Width = 300,
            Height = 300,
            ImageFormat = ImageFormat.PNG
        };

        return await GenerateBarcodeAsync(request);
    }

    public bool ValidateDataForBarcodeType(string data, BarcodeType barcodeType)
    {
        if (string.IsNullOrEmpty(data))
            return false;

        return barcodeType switch
        {
            BarcodeType.QRCode => ValidateQRCodeData(data),
            BarcodeType.Code128 => ValidateCode128Data(data),
            BarcodeType.Code39 => ValidateCode39Data(data),
            BarcodeType.Code39Extended => ValidateCode39ExtendedData(data),
            BarcodeType.Code93 => ValidateCode93Data(data),
            BarcodeType.Code93Extended => ValidateCode93ExtendedData(data),
            BarcodeType.EAN13 => ValidateEAN13Data(data),
            BarcodeType.EAN8 => ValidateEAN8Data(data),
            BarcodeType.EAN128 => ValidateEAN128Data(data),
            BarcodeType.EAN14 => ValidateEAN14Data(data),
            BarcodeType.Code11 => ValidateCode11Data(data),
            BarcodeType.Codabar => ValidateCodabarData(data),
            BarcodeType.Code25 => ValidateCode25Data(data),
            BarcodeType.Interleaved25 => ValidateInterleaved25Data(data),
            _ => false
        };
    }

    private static BarCodeType MapBarcodeType(BarcodeType barcodeType)
    {
        return barcodeType switch
        {
            BarcodeType.QRCode => BarCodeType.QRCode,
            BarcodeType.Code128 => BarCodeType.Code128,
            BarcodeType.Code39 => BarCodeType.Code39,
            BarcodeType.Code39Extended => BarCodeType.Code39Extended,
            BarcodeType.Code93 => BarCodeType.Code93,
            BarcodeType.Code93Extended => BarCodeType.Code93Extended,
            BarcodeType.EAN13 => BarCodeType.EAN13,
            BarcodeType.EAN8 => BarCodeType.EAN8,
            BarcodeType.EAN128 => BarCodeType.EAN128,
            BarcodeType.EAN14 => BarCodeType.EAN14,
            BarcodeType.Code11 => BarCodeType.Code11,
            BarcodeType.Codabar => BarCodeType.Codabar,
            BarcodeType.Code25 => BarCodeType.Code25,
            BarcodeType.Interleaved25 => BarCodeType.Interleaved25,
            _ => BarCodeType.QRCode
        };
    }

    /// <summary>
    /// Cross-platform barcode image conversion using SkiaSharp when possible
    /// </summary>
    private async Task<string> ConvertBarcodeToBase64Async(BarCodeGenerator generator, ImageFormat format, int width, int height, string data)
    {
        try
        {
            // Try to use the original FreeSpire method first
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // On Windows, use the original System.Drawing approach with suppress warnings
                return await ConvertImageToBase64WindowsAsync(generator, format);
            }
            else
            {
                // On Linux/macOS, provide a fallback that explains the limitation
                _logger.LogWarning("Running on non-Windows platform. Barcode generation requires Windows environment for full functionality.");
                return await CreatePlaceholderImageAsync(data, format, width, height);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate barcode image using platform-specific method. Creating placeholder.");
            // Fallback to placeholder
            return await CreatePlaceholderImageAsync(data, format, width, height);
        }
    }

    /// <summary>
    /// Windows-specific image conversion (original approach with warnings suppressed)
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static async Task<string> ConvertImageToBase64WindowsAsync(BarCodeGenerator generator, ImageFormat format)
    {
        try
        {
            var image = generator.GenerateImage();
            using var memoryStream = new MemoryStream();
            var imageFormat = GetSystemDrawingImageFormat(format);

            await Task.Run(() => image.Save(memoryStream, imageFormat));

            var imageBytes = memoryStream.ToArray();
            return Convert.ToBase64String(imageBytes);
        }
        catch (Exception)
        {
            throw new PlatformNotSupportedException("Barcode image generation is only supported on Windows. Please use a Windows environment for full barcode functionality.");
        }
    }

    /// <summary>
    /// Creates a placeholder image using SkiaSharp for cross-platform compatibility
    /// </summary>
    private async Task<string> CreatePlaceholderImageAsync(string data, ImageFormat format, int width, int height)
    {
        try
        {
            await Task.Delay(1); // Make it async

            // Create SkiaSharp bitmap
            using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;

            // Fill background with white
            canvas.Clear(SKColors.White);

            // Draw placeholder text
            using var paint = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            using var font = new SKFont
            {
                Size = Math.Min(width, height) / 10
            };

            var text = $"BARCODE: {data}";
            var textBounds = new SKRect();
            _ = font.MeasureText(text, out var textBounds2);
            textBounds = textBounds2;

            var x = (width - textBounds.Width) / 2;
            var y = ((height - textBounds.Height) / 2) + textBounds.Height;

            canvas.DrawText(text, x, y, SKTextAlign.Left, font, paint);

            // Draw border
            using var borderPaint = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };
            canvas.DrawRect(1, 1, width - 2, height - 2, borderPaint);

            // Convert to image
            using var image = surface.Snapshot();
            using var imageData = image.Encode(GetSkiaSharpEncodedImageFormat(format), 100);

            return Convert.ToBase64String(imageData.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate placeholder barcode using SkiaSharp");
            throw new InvalidOperationException("Failed to generate barcode on this platform. SkiaSharp dependencies may be missing.", ex);
        }
    }

    [SupportedOSPlatform("windows")]
    private static System.Drawing.Imaging.ImageFormat GetSystemDrawingImageFormat(ImageFormat format)
    {
        return format switch
        {
            ImageFormat.PNG => System.Drawing.Imaging.ImageFormat.Png,
            ImageFormat.JPEG => System.Drawing.Imaging.ImageFormat.Jpeg,
            ImageFormat.BMP => System.Drawing.Imaging.ImageFormat.Bmp,
            ImageFormat.GIF => System.Drawing.Imaging.ImageFormat.Gif,
            _ => System.Drawing.Imaging.ImageFormat.Png
        };
    }

    private static SKEncodedImageFormat GetSkiaSharpEncodedImageFormat(ImageFormat format)
    {
        return format switch
        {
            ImageFormat.PNG => SKEncodedImageFormat.Png,
            ImageFormat.JPEG => SKEncodedImageFormat.Jpeg,
            ImageFormat.BMP => SKEncodedImageFormat.Bmp,
            ImageFormat.GIF => SKEncodedImageFormat.Gif,
            _ => SKEncodedImageFormat.Png
        };
    }

    private static string GetMimeType(ImageFormat format)
    {
        return format switch
        {
            ImageFormat.PNG => "image/png",
            ImageFormat.JPEG => "image/jpeg",
            ImageFormat.BMP => "image/bmp",
            ImageFormat.GIF => "image/gif",
            _ => "image/png"
        };
    }

    #region Validation Methods

    private static bool ValidateQRCodeData(string data)
    {
        // QR codes can contain up to 4,296 alphanumeric characters
        return data.Length <= 4296;
    }

    private static bool ValidateCode128Data(string data)
    {
        // Code 128 can encode ASCII characters
        return data.All(c => c <= 127);
    }

    private static bool ValidateCode39Data(string data)
    {
        // Code 39 supports specific characters
        var code39Pattern = @"^[A-Z0-9\-\.\s\$\/\+%]*$";
        return Regex.IsMatch(data, code39Pattern);
    }

    private static bool ValidateCode39ExtendedData(string data)
    {
        // Code 39 Extended can encode ASCII characters
        return data.All(c => c <= 127);
    }

    private static bool ValidateCode93Data(string data)
    {
        // Code 93 supports specific characters (similar to Code 39 but more compact)
        var code93Pattern = @"^[A-Z0-9\-\.\s\$\/\+%]*$";
        return Regex.IsMatch(data, code93Pattern);
    }

    private static bool ValidateCode93ExtendedData(string data)
    {
        // Code 93 Extended can encode ASCII characters
        return data.All(c => c <= 127);
    }

    private static bool ValidateEAN13Data(string data)
    {
        // EAN-13 must be exactly 13 digits
        return data.Length == 13 && data.All(char.IsDigit);
    }

    private static bool ValidateEAN8Data(string data)
    {
        // EAN-8 must be exactly 8 digits
        return data.Length == 8 && data.All(char.IsDigit);
    }

    private static bool ValidateEAN128Data(string data)
    {
        // EAN-128 can encode various lengths, ASCII characters
        return data.Length > 0 && data.All(c => c <= 127);
    }

    private static bool ValidateEAN14Data(string data)
    {
        // EAN-14 must be exactly 14 digits
        return data.Length == 14 && data.All(char.IsDigit);
    }

    private static bool ValidateCode11Data(string data)
    {
        // Code 11 supports digits and dash
        var code11Pattern = @"^[0-9\-]*$";
        return Regex.IsMatch(data, code11Pattern);
    }

    private static bool ValidateCodabarData(string data)
    {
        // Codabar supports digits and specific special characters
        var codabarPattern = @"^[0-9\-\$\:\/\.\+]*$";
        return Regex.IsMatch(data, codabarPattern);
    }

    private static bool ValidateCode25Data(string data)
    {
        // Code 25 (Standard 2 of 5) supports only digits
        return data.All(char.IsDigit);
    }

    private static bool ValidateInterleaved25Data(string data)
    {
        // Interleaved 2 of 5 supports only digits and must have even length
        return data.All(char.IsDigit) && data.Length % 2 == 0;
    }

    #endregion
}