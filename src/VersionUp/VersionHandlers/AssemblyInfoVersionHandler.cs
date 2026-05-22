using System;
using System.IO;
using System.Text.RegularExpressions;

namespace VersionUp.VersionHandlers
{
	/// <summary>
	/// Handles parsing and updating version attributes in C#, VB, and F# AssemblyInfo files.
	/// </summary>
	public class AssemblyInfoVersionHandler : IVersionFileHandler
	{
	    /// <summary>
	    /// Matches <c>AssemblyVersion</c>, <c>AssemblyFileVersion</c>, and
	    /// <c>AssemblyInformationalVersion</c> attribute declarations in C#, VB, and F# syntax.
	    /// </summary>
	    private static readonly Regex VersionRegex = new Regex(
	        @"(?:\[<assembly:\s*|\[assembly:\s*|<Assembly:\s*)(AssemblyVersion|AssemblyFileVersion|AssemblyInformationalVersion)\(\""([^\""]+)\""\)(?:\]>|\]|>)",
	        RegexOptions.Compiled | RegexOptions.IgnoreCase);

	    /// <inheritdoc />
	    public bool CanHandle(string filePath)
	    {
	        if (string.IsNullOrEmpty(filePath))
	        {
	            return false;
	        }

	        string fileName = Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant();
	        string extension = Path.GetExtension(filePath).ToLowerInvariant();

	        bool isAssemblyInfo = fileName.Equals("assemblyinfo", StringComparison.OrdinalIgnoreCase);
	        bool isSupportedExtension = extension == ".cs" || extension == ".vb" || extension == ".fs";

	        return isAssemblyInfo && isSupportedExtension;
	    }

	    /// <inheritdoc />
	    public string? GetVersion(string fileContent)
	    {
	        if (string.IsNullOrWhiteSpace(fileContent))
	        {
	            return null;
	        }

	        MatchCollection matches = VersionRegex.Matches(fileContent);

	        foreach (Match match in matches)
	        {
	            if (match.Groups[1].Value.Equals("AssemblyVersion", StringComparison.OrdinalIgnoreCase))
	            {
	                string value = match.Groups[2].Value;

	                return value;
	            }
	        }

	        foreach (Match match in matches)
	        {
	            if (match.Groups[1].Value.Equals("AssemblyFileVersion", StringComparison.OrdinalIgnoreCase))
	            {
	                string value = match.Groups[2].Value;

	                return value;
	            }
	        }

	        if (matches.Count > 0)
	        {
	            string value = matches[0].Groups[2].Value;

	            return value;
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

	        string result = VersionRegex.Replace(fileContent, match =>
	        {
	            string attributeName = match.Groups[1].Value;
	            string rawMatch = match.Value;
	            string rawValue = match.Groups[2].Value;

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
}
