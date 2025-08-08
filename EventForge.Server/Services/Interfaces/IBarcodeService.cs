using EventForge.DTOs.Common;

namespace EventForge.Server.Services.Interfaces;

public interface IBarcodeService
{
    /// <summary>
    /// Generates a barcode or QR code based on the provided request parameters
    /// </summary>
    /// <param name="request">The barcode generation request</param>
    /// <returns>A barcode response containing the generated image as base64</returns>
    Task<BarcodeResponseDto> GenerateBarcodeAsync(BarcodeRequestDto request);

    /// <summary>
    /// Generates a QR code with default settings
    /// </summary>
    /// <param name="data">The data to encode in the QR code</param>
    /// <returns>A barcode response containing the generated QR code as base64</returns>
    Task<BarcodeResponseDto> GenerateQRCodeAsync(string data);

    /// <summary>
    /// Validates if the provided data is suitable for the specified barcode type
    /// </summary>
    /// <param name="data">The data to validate</param>
    /// <param name="barcodeType">The barcode type to validate against</param>
    /// <returns>True if the data is valid for the barcode type, otherwise false</returns>
    bool ValidateDataForBarcodeType(string data, BarcodeType barcodeType);
}