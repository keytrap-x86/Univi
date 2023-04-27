namespace Univi.Core.Interfaces;
public interface ILocalWindowsSessionService
{
    /// <summary>
    ///     Gets the SID of the currently connected to console session user
    /// </summary>
    /// <param name="sessionId"></param>
    /// <returns></returns>
    string? GetConsoleUserSid(int sessionId = 1);

    /// <summary>
    ///     Gets the SID of a user
    /// </summary>
    /// <param name="username"></param>
    /// <returns></returns>
    string? GetUserSid(string username);

    /// <summary>
    ///     Gets the username of the currently logged in user
    /// </summary>
    /// <returns></returns>
    string? GetLoggedInUsername();
}
