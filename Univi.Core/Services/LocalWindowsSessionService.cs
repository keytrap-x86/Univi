using Microsoft.Extensions.Logging;
using System;
using System.Security.Principal;
using Univi.Core.Infrastructure;
using Univi.Core.Interfaces;

namespace Univi.Core.Services;
/// <summary>
///     A service that manages all windows-session related tasks.
/// </summary>
public class LocalWindowsSessionService : ILocalWindowsSessionService
{
    private readonly ILogger<LocalWindowsSessionService> _logger;

    public LocalWindowsSessionService(ILogger<LocalWindowsSessionService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    ///     Gets the currently logged in user (to console, so physically)
    /// </summary>
    /// <param name="sessionId"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public string? GetConsoleUserSid(int sessionId = 1)
    {
        try
        {
            if (!PInvokes.TryGetUsernameBySessionId(sessionId, false, out var username))
                return null;

            return GetUserSid(username);
        }
        catch (Exception ex)
        {
            throw new Exception("Une erreur est survenue lors de la " +
                "récupération de la session utilisateur. L'application ne peut pas fonctionner correctement." + ex.Message, ex);
        }
    }

    /// <summary>
    ///     Gets the currently logged in username
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public string? GetLoggedInUsername()
    {
        try
        {
            if (PInvokes.TryGetUsernameBySessionId(1, true, out var username))
            {
                return username;
            }

            return null;
        }
        catch (Exception ex)
        {
            throw new Exception("Une erreur est survenue lors de la " +
                                   "récupération de la session utilisateur. L'application ne peut pas fonctionner correctement." + ex.Message, ex);
        }
    }

    /// <summary>
    ///     Gets user SID from username
    /// </summary>
    /// <param name="username"></param>
    /// <returns></returns>
    public string? GetUserSid(string username)
    {
        if (username.StartsWith(@".\"))
        {
            username = username.Replace(@".\", Environment.MachineName + @"\");
        }

        try
        {
            NTAccount account = new(username);
            return account.Translate(typeof(SecurityIdentifier))?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error while retreiving SID from username {user} : {err}", username, ex.Message);
        }

        return null;
    }
}
