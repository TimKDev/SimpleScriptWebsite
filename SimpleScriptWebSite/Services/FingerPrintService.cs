using System.Text;
using SimpleScriptWebSite.Interfaces;

namespace SimpleScriptWebSite.Services;

public class FingerPrintService : IFingerPrintService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FingerPrintService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetUserIdentifier()
    {
        // Get IP address
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
        {
            return null;
        }

        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return null;
        }

        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(ipAddress);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}