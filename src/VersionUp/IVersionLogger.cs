namespace VersionUp;

/// <summary>
/// Simple interface to log version increment activities.
/// </summary>
public interface IVersionLogger
{
    /// <summary>
    /// Logs a version increment message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    void Log(string message);
}
