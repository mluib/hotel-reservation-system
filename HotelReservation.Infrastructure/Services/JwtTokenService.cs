using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HotelReservation.Application.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;

namespace HotelReservation.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateToken(string userId, string userName, System.Collections.Generic.IList<string> roles)
    {
        var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured");
        var issuer = _config["Jwt:Issuer"] ?? "hotel";
        var audience = _config["Jwt:Audience"] ?? "hotel_audience";
        var expiresMinutes = int.TryParse(_config["Jwt:ExpireMinutes"], out var m) ? m : 60;

        var claims = new System.Collections.Generic.List<Claim>
        {
            // Use userId as the subject identifier (sub) per best practices
            new Claim(JwtRegisteredClaimNames.Sub, userId ?? string.Empty),
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userName ?? string.Empty)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var keyBytes = Encoding.UTF8.GetBytes(key);
        var securityKey = new SymmetricSecurityKey(keyBytes);
        var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: System.DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
