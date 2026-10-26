using CYHCare.Application.DTOs;

namespace CYHCare.Web.Services;

/// <summary>
/// In-memory and browser state management for logged-in user in Blazor Server interactivity.
/// </summary>
public class UserSessionService
{
    public bool IsAuthenticated { get; private set; }
    public int UserId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public int? HelperId { get; private set; }
    public bool IsVerified { get; private set; }
    public string Token { get; private set; } = string.Empty;

    public event Action? OnAuthStateChanged;

    public void SetSession(AuthResponseDto auth)
    {
        IsAuthenticated = true;
        UserId = auth.UserId;
        FullName = auth.FullName;
        Email = auth.Email;
        Role = auth.Role;
        HelperId = auth.HelperId;
        IsVerified = auth.IsVerified;
        Token = auth.Token;

        NotifyStateChanged();
    }

    public void ClearSession()
    {
        IsAuthenticated = false;
        UserId = 0;
        FullName = string.Empty;
        Email = string.Empty;
        Role = string.Empty;
        HelperId = null;
        IsVerified = false;
        Token = string.Empty;

        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnAuthStateChanged?.Invoke();
}
