using System.Security.Claims;
using Kerpackie.Discord.Auth.Interfaces;
using Kerpackie.Discord.Auth.Models;

namespace AspireDemo.ApiService;

public class DemoDiscordAuthHandler : IDiscordAuthHandler
{
    public Task<ClaimsPrincipal> OnDiscordLoginAsync(DiscordLoginContext context)
    {
        var id = new ClaimsIdentity("Identity.External");
        id.AddClaim(new Claim(ClaimTypes.NameIdentifier, context.DiscordId));
        id.AddClaim(new Claim(ClaimTypes.Name, context.Username));
        
        if (!string.IsNullOrEmpty(context.AvatarUrl))
        {
            id.AddClaim(new Claim("urn:discord:avatar:url", context.AvatarUrl));
        }

        return Task.FromResult(new ClaimsPrincipal(id));
    }
}
