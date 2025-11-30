using System.Security.Claims;
using Kerpackie.Discord.Auth.Interfaces;
using Kerpackie.Discord.Auth.Models;

namespace AspireDemo.Basic.ApiService;

public class DemoDiscordAuthHandler : IDiscordAuthHandler
{
    public Task<ClaimsPrincipal> OnDiscordLoginAsync(DiscordLoginContext context)
    {
        var identity = new ClaimsIdentity("Identity.External");
        
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, context.DiscordId));
        identity.AddClaim(new Claim(ClaimTypes.Name, context.Username));
        
        if (!string.IsNullOrEmpty(context.AvatarUrl))
        {
            identity.AddClaim(new Claim("urn:discord:avatar:url", context.AvatarUrl));
        }

        return Task.FromResult(new ClaimsPrincipal(identity));
    }
}
