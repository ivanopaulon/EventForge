using EventForge.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BarcodeController : BaseApiController
{
    private readonly IBarcodeService _barcodeService;
    private readonly ILogger<BarcodeController> _logger;

    public BarcodeController(IBarcodeService barcodeService, ILogger<BarcodeController> logger)
    {
        _barcodeService = barcodeService;
        _logger = logger;
    }

    /// <summary>
    /// Generates a barcode or QR code based on the provided parameters
    /// </summary>
    /// <param name="request">The barcode generation request</param>
    /// <returns>The generated barcode as base64 image</returns>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(BarcodeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BarcodeResponseDto>> GenerateBarcode([FromBody] BarcodeRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _barcodeService.GenerateBarcodeAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid barcode generation request");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating barcode");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while generating the barcode");
        }
    }

    /// <summary>
    /// Generates a QR code with default settings
    /// </summary>
    /// <param name="data">The data to encode in the QR code</param>
    /// <returns>The generated QR code as base64 image</returns>
    [HttpPost("qr")]
    [ProducesResponseType(typeof(BarcodeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BarcodeResponseDto>> GenerateQRCode([FromBody][Required] string data)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return BadRequest("Data cannot be empty");
            }

            var result = await _barcodeService.GenerateQRCodeAsync(data);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid QR code generation request");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR code");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while generating the QR code");
        }
    }

    /// <summary>
    /// Validates if the provided data is suitable for the specified barcode type
    /// </summary>
    /// <param name="data">The data to validate</param>
    /// <param name="barcodeType">The barcode type to validate against</param>
    /// <returns>True if the data is valid for the barcode type, otherwise false</returns>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<bool> ValidateData([FromQuery][Required] string data, [FromQuery] BarcodeType barcodeType)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return BadRequest("Data cannot be empty");
            }

            var isValid = _barcodeService.ValidateDataForBarcodeType(data, barcodeType);
            return Ok(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating barcode data");
            return BadRequest("An error occurred while validating the data");
        }
    }

    /// <summary>
    /// Gets the supported barcode types
    /// </summary>
    /// <returns>List of supported barcode types</returns>
    [HttpGet("types")]
    [ProducesResponseType(typeof(IEnumerable<BarcodeType>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<BarcodeType>> GetSupportedBarcodeTypes()
    {
        var types = Enum.GetValues<BarcodeType>();
        return Ok(types);
    }

    /// <summary>
    /// Gets the supported image formats
    /// </summary>
    /// <returns>List of supported image formats</returns>
    [HttpGet("formats")]
    [ProducesResponseType(typeof(IEnumerable<ImageFormat>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<ImageFormat>> GetSupportedImageFormats()
    {
        var formats = Enum.GetValues<ImageFormat>();
        return Ok(formats);
    }
}