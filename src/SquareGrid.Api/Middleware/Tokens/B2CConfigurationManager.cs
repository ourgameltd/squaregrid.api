using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SquareGrid.Api.Middleware.Tokens
{
    public class B2CConfigurationManager
    {
        private readonly string _authority;
        private readonly string _clientId;
        private readonly string _issuer;
        private readonly IConfigurationManager<OpenIdConnectConfiguration> _configManager;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(24);

        public B2CConfigurationManager(IMemoryCache cache)
        {
            _authority = Environment.GetEnvironmentVariable("B2CAuthority") ?? throw new Exception("B2CAuthority cannot be null.");
            _clientId = Environment.GetEnvironmentVariable("B2CClientId") ?? throw new Exception("B2CClientId cannot be null.");
            _issuer = Environment.GetEnvironmentVariable("B2CIssuer") ?? throw new Exception("B2CIssuer cannot be null.");

            _configManager = new ConfigurationManager<OpenIdConnectConfiguration>($"{_authority}.well-known/openid-configuration", new OpenIdConnectConfigurationRetriever());
            _cache = cache;
        }

        public async Task<OpenIdConnectConfiguration> GetConfigurationAsync()
        {
            if (!_cache.TryGetValue(_authority, out OpenIdConnectConfiguration? config))
            {
                config = await _configManager.GetConfigurationAsync(new CancellationToken());
                _cache.Set(_authority, config, _cacheDuration);
            }

            return config!;
        }

        public async Task<ClaimsPrincipal> ValidateToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentNullException(token);
            }

            OpenIdConnectConfiguration config = await GetConfigurationAsync();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _clientId,
                ValidateLifetime = true,
                IssuerSigningKeys = config.SigningKeys
            };

            var handler = new JwtSecurityTokenHandler();
            return handler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
        }
    }
}
