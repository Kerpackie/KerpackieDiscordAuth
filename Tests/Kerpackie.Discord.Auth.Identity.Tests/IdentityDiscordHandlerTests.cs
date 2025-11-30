using System.Security.Claims;
using Kerpackie.Discord.Auth.Identity;
using Kerpackie.Discord.Auth.Models;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Kerpackie.Discord.Auth.Identity.Tests;

public class IdentityDiscordHandlerTests
{
    private readonly Mock<UserManager<IdentityUser>> _userManagerMock;
    private readonly Mock<IUserClaimsPrincipalFactory<IdentityUser>> _principalFactoryMock;
    private readonly IdentityDiscordHandler<IdentityUser> _handler;

    public IdentityDiscordHandlerTests()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        _userManagerMock = new Mock<UserManager<IdentityUser>>(store.Object, null, null, null, null, null, null, null, null);
        _principalFactoryMock = new Mock<IUserClaimsPrincipalFactory<IdentityUser>>();
        _handler = new IdentityDiscordHandler<IdentityUser>(_userManagerMock.Object, _principalFactoryMock.Object);
    }

    [Fact]
    public async Task OnDiscordLoginAsync_UserExists_LinksLoginAndUpdatesTokens()
    {
        // Arrange
        var context = new DiscordLoginContext
        {
            Email = "test@example.com",
            Username = "TestUser",
            DiscordId = "123456789",
            AccessToken = "access_token_123",
            AvatarUrl = "https://discord.com/avatar.png",
            OriginalClaims = new List<Claim> { new Claim("urn:discord:global_name", "TestUser") }
        };

        var user = new IdentityUser { Email = "test@example.com", UserName = "test@example.com" };
        _userManagerMock.Setup(x => x.FindByEmailAsync(context.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.GetLoginsAsync(user)).ReturnsAsync(new List<UserLoginInfo>());
        _userManagerMock.Setup(x => x.AddLoginAsync(user, It.IsAny<UserLoginInfo>())).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.SetAuthenticationTokenAsync(user, "Discord", "access_token", context.AccessToken)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>());
        _userManagerMock.Setup(x => x.AddClaimAsync(user, It.IsAny<Claim>())).ReturnsAsync(IdentityResult.Success);
        
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        _principalFactoryMock.Setup(x => x.CreateAsync(user)).ReturnsAsync(principal);

        // Act
        var result = await _handler.OnDiscordLoginAsync(context);

        // Assert
        Assert.Equal(principal, result);
        _userManagerMock.Verify(x => x.FindByEmailAsync(context.Email), Times.Once);
        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<IdentityUser>()), Times.Never);
        _userManagerMock.Verify(x => x.AddLoginAsync(user, It.Is<UserLoginInfo>(l => l.LoginProvider == "Discord" && l.ProviderKey == context.DiscordId)), Times.Once);
        _userManagerMock.Verify(x => x.SetAuthenticationTokenAsync(user, "Discord", "access_token", context.AccessToken), Times.Once);
        _userManagerMock.Verify(x => x.AddClaimAsync(user, It.Is<Claim>(c => c.Type == "urn:discord:avatar:url" && c.Value == context.AvatarUrl)), Times.Once);
    }

    [Fact]
    public async Task OnDiscordLoginAsync_UserDoesNotExist_CreatesUser()
    {
        // Arrange
        var context = new DiscordLoginContext
        {
            Email = "new@example.com",
            Username = "NewUser",
            DiscordId = "987654321",
            AccessToken = "new_access_token",
            AvatarUrl = null,
            OriginalClaims = new List<Claim>()
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(context.Email)).ReturnsAsync((IdentityUser?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>())).ReturnsAsync(IdentityResult.Success);
        
        // Capture the created user to setup subsequent calls
        IdentityUser? createdUser = null;
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>()))
            .Callback<IdentityUser>(u => createdUser = u)
            .ReturnsAsync(IdentityResult.Success);

        // We need to setup calls for the created user, but since it's created inside the method, 
        // we can't easily setup strict mocks for that specific instance unless we use It.IsAny<IdentityUser>() 
        // or rely on the fact that the mock returns success for any user.
        // However, GetLoginsAsync needs to return empty list for the new user.
        _userManagerMock.Setup(x => x.GetLoginsAsync(It.IsAny<IdentityUser>())).ReturnsAsync(new List<UserLoginInfo>());
        _userManagerMock.Setup(x => x.AddLoginAsync(It.IsAny<IdentityUser>(), It.IsAny<UserLoginInfo>())).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.SetAuthenticationTokenAsync(It.IsAny<IdentityUser>(), "Discord", "access_token", context.AccessToken)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.GetClaimsAsync(It.IsAny<IdentityUser>())).ReturnsAsync(new List<Claim>());

        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        _principalFactoryMock.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>())).ReturnsAsync(principal);

        // Act
        var result = await _handler.OnDiscordLoginAsync(context);

        // Assert
        Assert.Equal(principal, result);
        _userManagerMock.Verify(x => x.CreateAsync(It.Is<IdentityUser>(u => u.Email == context.Email && u.EmailConfirmed)), Times.Once);
        _userManagerMock.Verify(x => x.AddLoginAsync(It.IsAny<IdentityUser>(), It.Is<UserLoginInfo>(l => l.LoginProvider == "Discord")), Times.Once);
    }
}
