using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EventForge.Server.Services.Auth;

/// <summary>
/// Service for JWT token generation and validation.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a JWT token for the specified user.
    /// </summary>
    /// <param name="user">User information</param>
    /// <param name="roles">User roles</param>
    /// <param name="permissions">User permissions</param>
    /// <returns>JWT token</returns>
    string GenerateToken(User user, IEnumerable<string> roles, IEnumerable<string> permissions);

    /// <summary>
    /// Validates a JWT token.
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>Claims principal if valid, null otherwise</returns>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>
    /// Gets the token expiration time in seconds.
    /// </summary>
    int TokenExpirationSeconds { get; }
}

/// <summary>
/// JWT configuration options.
/// </summary>
public class JwtOptions
{
    /// <summary>
    /// JWT issuer.
    /// </summary>
    public string Issuer { get; set; } = "EventForge";

    /// <summary>
    /// JWT audience.
    /// </summary>
    public string Audience { get; set; } = "EventForge";

    /// <summary>
    /// Secret key for signing tokens.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time in minutes.
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Clock skew in minutes.
    /// </summary>
    public int ClockSkewMinutes { get; set; } = 5;
}

/// <summary>
/// Implementation of JWT token service.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<JwtTokenService> _logger;
    private readonly TokenValidationParameters _tokenValidationParameters;

    public JwtTokenService(IConfiguration configuration, ILogger<JwtTokenService> logger)
    {
        _logger = logger;
        _jwtOptions = configuration.GetSection("Authentication:Jwt").Get<JwtOptions>() ?? new JwtOptions();

        if (string.IsNullOrEmpty(_jwtOptions.SecretKey))
        {
            throw new InvalidOperationException("JWT SecretKey must be configured in Authentication:Jwt:SecretKey");
        }

        if (_jwtOptions.SecretKey.Length < 32)
        {
            throw new InvalidOperationException("JWT SecretKey must be at least 32 characters long");
        }

        _tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtOptions.Issuer,
            ValidAudience = _jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey)),
            ClockSkew = TimeSpan.FromMinutes(_jwtOptions.ClockSkewMinutes)
        };
    }

    public int TokenExpirationSeconds => _jwtOptions.ExpirationMinutes * 60;

    public string GenerateToken(User user, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
            new("full_name", user.FullName),
            new("user_id", user.Id.ToString()),
            new("username", user.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Add roles
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Add permissions
        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes),
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        var token = tokenHandler.WriteToken(securityToken);

        _logger.LogDebug("JWT token generated for user {Username}", user.Username);

        return token;
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, _tokenValidationParameters, out _);
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate JWT token");
            return null;
        }
    }
}