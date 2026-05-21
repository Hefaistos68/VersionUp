namespace VersionUp;

using System;
using System.IO;
using System.Text.RegularExpressions;

/// <summary>
/// Handles parsing and updating versions in native C++ resource script (.rc) files.
/// </summary>
public class RcVersionHandler : IVersionFileHandler
{
    /// <summary>Matches the <c>FILEVERSION major,minor,build,revision</c> keyword line.</summary>
    private static readonly Regex FileVersionKeywordRegex = new Regex(
        @"FILEVERSION\s+\d+\s*,\s*\d+\s*,\s*\d+\s*,\s*\d+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>Matches the <c>PRODUCTVERSION major,minor,build,revision</c> keyword line.</summary>
    private static readonly Regex ProductVersionKeywordRegex = new Regex(
        @"PRODUCTVERSION\s+\d+\s*,\s*\d+\s*,\s*\d+\s*,\s*\d+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>Matches a <c>VALUE "FileVersion", "x.y.z.w"</c> string block value.</summary>
    private static readonly Regex FileVersionValueRegex = new Regex(
        @"(VALUE\s+""FileVersion""\s*,\s*"")([^""]+)("")",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>Matches a <c>VALUE "ProductVersion", "x.y.z.w"</c> string block value.</summary>
    private static readonly Regex ProductVersionValueRegex = new Regex(
        @"(VALUE\s+""ProductVersion""\s*,\s*"")([^""]+)("")",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <inheritdoc />
    public bool CanHandle(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return false;
        }

        string extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension == ".rc";
    }

    /// <inheritdoc />
    public string? GetVersion(string fileContent)
    {
        if (string.IsNullOrWhiteSpace(fileContent))
        {
            return null;
        }

        Match fileValMatch = FileVersionValueRegex.Match(fileContent);

        if (fileValMatch.Success)
        {
            string value = fileValMatch.Groups[2].Value;

            return value;
        }

        Match fileKeyMatch = FileVersionKeywordRegex.Match(fileContent);

        if (fileKeyMatch.Success)
        {
            string raw = fileKeyMatch.Value;
            string rawNumbers = Regex.Replace(raw, @"[^\d,]", "");
            string versionStr = rawNumbers.Replace(',', '.');

            return versionStr;
        }

        return null;
    }

    /// <inheritdoc />
    public string UpdateVersion(string fileContent, string newVersion)
    {
        if (string.IsNullOrWhiteSpace(fileContent))
        {
            return fileContent;
        }

        string commaVersion = "1,0,0,0";

        if (Version.TryParse(newVersion, out Version v))
        {
            int major = v.Major;
            int minor = v.Minor;
            int build = v.Build < 0 ? 0 : v.Build;
            int revision = v.Revision < 0 ? 0 : v.Revision;

            commaVersion = $"{major},{minor},{build},{revision}";
        }

        string result = fileContent;

        result = FileVersionKeywordRegex.Replace(result, $"FILEVERSION {commaVersion}");
        result = ProductVersionKeywordRegex.Replace(result, $"PRODUCTVERSION {commaVersion}");
        result = FileVersionValueRegex.Replace(result, $"${{1}}{newVersion}${{3}}");
        result = ProductVersionValueRegex.Replace(result, $"${{1}}{newVersion}${{3}}");

        return result;
    }
}
