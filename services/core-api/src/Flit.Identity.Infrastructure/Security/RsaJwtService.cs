using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Flit.Identity.Shared.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace Flit.Identity.Infrastructure.Security;

public sealed class RsaJwtService
{
    private readonly IConfiguration _config;
    private readonly RsaSecurityKey _signingKey;
    private readonly RsaSecurityKey _validationKey;

    public RsaJwtService(IConfiguration config, IWebHostEnvironment env)
    {
        _config = config;

        if (config.GetValue("Jwt:DevGenerate", false))
        {
            var rsa = RSA.Create(2048);
            _signingKey = new RsaSecurityKey(rsa);

            var publicOnly = RSA.Create();
            publicOnly.ImportParameters(rsa.ExportParameters(false));
            _validationKey = new RsaSecurityKey(publicOnly);
            return;
        }

        _signingKey = LoadPrivateKey(ResolveKeyPath(config["Jwt:PrivateKeyPath"]!, env));
        _validationKey = LoadPublicKey(ResolveKeyPath(config["Jwt:PublicKeyPath"]!, env));
    }

    public string CreateAccessToken(
        Guid userId,
        string email,
        Guid? tenantId,
        IReadOnlyList<string> roles,
        IReadOnlyList<PermissionGrant> permissions,
        bool isSuperAdmin,
        int tokenVersion)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new("token_version", tokenVersion.ToString()),
            new("is_super_admin", isSuperAdmin ? "true" : "false"),
        };

        if (tenantId is not null)
        {
            claims.Add(new Claim("tenant_id", tenantId.Value.ToString()));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim("roles", role));
        }

        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permissions", $"{permission.Key}|{permission.Scope}"));
        }

        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.RsaSha256);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal Validate(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        return handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _config["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = _config["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _validationKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        }, out _);
    }

    private static string ResolveKeyPath(string configuredPath, IWebHostEnvironment env)
    {
        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        return Path.GetFullPath(Path.Combine(env.ContentRootPath, configuredPath));
    }

    private static RsaSecurityKey LoadPrivateKey(string path)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(path));
        return new RsaSecurityKey(rsa);
    }

    private static RsaSecurityKey LoadPublicKey(string path)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(path));
        return new RsaSecurityKey(rsa);
    }
}
