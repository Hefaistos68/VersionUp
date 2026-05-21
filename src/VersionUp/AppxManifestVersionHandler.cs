namespace VersionUp;

using System;
using System.IO;
using System.Xml.Linq;

/// <summary>
/// Handles parsing and updating the package identity version in AppX packaging manifests.
/// </summary>
public class AppxManifestVersionHandler : IVersionFileHandler
{
    /// <inheritdoc />
    public bool CanHandle(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return false;
        }

        string fileName = Path.GetFileName(filePath).ToLowerInvariant();

        return fileName == "package.appxmanifest";
    }

    /// <inheritdoc />
    public string? GetVersion(string fileContent)
    {
        if (string.IsNullOrWhiteSpace(fileContent))
        {
            return null;
        }

        try
        {
            XDocument doc = XDocument.Parse(fileContent);
            XElement? identityElement = FindIdentityElement(doc);
            string? rawVersion = identityElement?.Attribute("Version")?.Value;

            if (string.IsNullOrEmpty(rawVersion))
            {
                return null;
            }

            return NormalizeToFourSegments(rawVersion!);
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public string UpdateVersion(string fileContent, string newVersion)
    {
        if (string.IsNullOrWhiteSpace(fileContent))
        {
            return fileContent;
        }

        XDocument doc = XDocument.Parse(fileContent);
        XElement? identityElement = FindIdentityElement(doc);
        string formattedVersion = NormalizeToFourSegments(newVersion);

        if (identityElement != null)
        {
            identityElement.SetAttributeValue("Version", formattedVersion);
        }

        string result = doc.ToString();

        return result;
    }

    /// <summary>
    /// Normalizes a version string to have exactly four segments, padding with zeros if necessary.
    /// </summary>
    /// <param name="version">The input version string.</param>
    /// <returns>A version string with exactly four segments.</returns>
    private static string NormalizeToFourSegments(string version)
    {
        if (string.IsNullOrEmpty(version))
        {
            return "1.0.0.0";
        }

        string[] parts = version.Split('.');
        List<string> segments = new();

        for (int i = 0; i < 4; i++)
        {
            if (i < parts.Length)
            {
                segments.Add(parts[i]);
            }
            else
            {
                segments.Add("0");
            }
        }

        string result = string.Join(".", segments);

        return result;
    }

    /// <summary>
    /// Searches for the <c>&lt;Identity&gt;</c> element at the root level of the AppX manifest,
    /// respecting its XML namespace.
    /// </summary>
    /// <param name="doc">The parsed XML document.</param>
    /// <returns>The Identity element, or <see langword="null"/> if not found.</returns>
    private static XElement? FindIdentityElement(XDocument doc)
    {
        if (doc.Root == null)
        {
            return null;
        }

        XNamespace ns = doc.Root.Name.Namespace;
        XElement? identity = doc.Root.Element(ns + "Identity");

        return identity;
    }
}
