namespace VersionUp.VersionHandlers
{
	/// <summary>
	/// Defines the contract for parsing and updating version elements in specific file formats.
	/// </summary>
	public interface IVersionFileHandler
	{
	    /// <summary>
	    /// Gets whether this handler can process the file at the specified path.
	    /// </summary>
	    /// <param name="filePath">The absolute path to the target file.</param>
	    /// <returns>True if the handler supports this file type; otherwise, false.</returns>
	    bool CanHandle(string filePath);

	    /// <summary>
	    /// Parses the version string from the file content.
	    /// </summary>
	    /// <param name="fileContent">The raw content of the file.</param>
	    /// <returns>The parsed version string, or null if no version is defined.</returns>
	    string? GetVersion(string fileContent);

	    /// <summary>
	    /// Updates the version string within the file content and returns the updated content.
	    /// </summary>
	    /// <param name="fileContent">The raw content of the file.</param>
	    /// <param name="newVersion">The new version string to write.</param>
	    /// <returns>The updated file content.</returns>
	    string UpdateVersion(string fileContent, string newVersion);
	}
}
