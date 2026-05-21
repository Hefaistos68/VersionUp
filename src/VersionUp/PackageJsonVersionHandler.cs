namespace VersionUp;

using System;
using System.IO;
using System.Text.RegularExpressions;

/// <summary>
/// Handles parsing and updating the version property in package.json files.
/// </summary>
public class PackageJsonVersionHandler : IVersionFileHandler
{
    /// <summary>Matches the <c>"version"</c> property value in a JSON object.</summary>
    private static readonly Regex VersionRegex = new Regex(
        @"""version""\s*:\s*""([^""]+)""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <inheritdoc />
    public bool CanHandle(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return false;
        }

        string fileName = Path.GetFileName(filePath).ToLowerInvariant();

        return fileName == "package.json";
    }

    /// <inheritdoc />
    public string? GetVersion(string fileContent)
    {
        if (string.IsNullOrWhiteSpace(fileContent))
        {
            return null;
        }

        Match match = VersionRegex.Match(fileContent);

        if (match.Success)
        {
            string value = match.Groups[1].Value;

            return value;
        }

        return null;
    }

    /// <inheritdoc />
    public string UpdateVersion(string fileContent, string newVersion)
    {
        if (string.IsNullOrWhiteSpace(fileContent))
        {
            return $"{{\n  \"version\": \"{newVersion}\"\n}}";
        }

        string result = VersionRegex.Replace(fileContent, match =>
        {
            string rawMatch = match.Value;
            string rawValue = match.Groups[1].Value;

            int index = rawMatch.IndexOf(rawValue, StringComparison.Ordinal);

            if (index >= 0)
            {
                string replaced = rawMatch.Substring(0, index) + newVersion + rawMatch.Substring(index + rawValue.Length);

                return replaced;
            }

            return rawMatch;
        });

        return result;
    }
}
