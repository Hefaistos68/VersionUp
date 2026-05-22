namespace VersionUp
{
	using System;

	/// <summary>
	/// Class responsible for incrementing semver or traditional .NET version strings.
	/// </summary>
	public class VersionIncrementer
	{
	    /// <summary>The logger used to record version increment operations.</summary>
	    private readonly IVersionLogger _logger;

	    /// <summary>
	    /// Initializes a new instance of the <see cref="VersionIncrementer"/> class.
	    /// </summary>
	    /// <param name="logger">The version logger instance.</param>
	    public VersionIncrementer(IVersionLogger logger)
	    {
	        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	    }

	    /// <summary>
	    /// Increments a specific segment of a version string.
	    /// </summary>
	    /// <param name="currentVersion">The current version string.</param>
	    /// <param name="segment">The version segment to increment.</param>
	    /// <returns>The incremented version string.</returns>
	    public string Increment(string currentVersion, VersionSegment segment)
	    {
	        if (string.IsNullOrWhiteSpace(currentVersion))
	        {
	            _logger.Log("Current version is empty, returning default 1.0.0");

	            return "1.0.0";
	        }

	        if (!Version.TryParse(currentVersion, out Version parsedVersion))
	        {
	            _logger.Log($"Failed to parse version '{currentVersion}', returning default 1.0.0");

	            return "1.0.0";
	        }

	        int major = parsedVersion.Major;
	        int minor = parsedVersion.Minor;
	        int build = parsedVersion.Build < 0 ? 0 : parsedVersion.Build;
	        int revision = parsedVersion.Revision < 0 ? 0 : parsedVersion.Revision;

	        switch (segment)
	        {
	            case VersionSegment.Major:
	                major++;
	                minor = 0;
	                build = 0;
	                revision = 0;
	                break;

	            case VersionSegment.Minor:
	                minor++;
	                build = 0;
	                revision = 0;
	                break;

	            case VersionSegment.Build:
	                build++;
	                revision = 0;
	                break;

	            case VersionSegment.Revision:
	                revision++;
	                break;

	            default:
	                throw new ArgumentOutOfRangeException(nameof(segment), segment, null);
	        }

	        string result = revision > 0
	            ? $"{major}.{minor}.{build}.{revision}"
	            : $"{major}.{minor}.{build}";

	        _logger.Log($"Incremented version from '{currentVersion}' to '{result}' (Segment: {segment})");

	        return result;
	    }
	}
}
