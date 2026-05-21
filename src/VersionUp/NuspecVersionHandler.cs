namespace VersionUp;

using System;
using System.IO;
using System.Xml.Linq;

/// <summary>
/// Handles parsing and updating version elements in NuGet specification (.nuspec) files.
/// </summary>
public class NuspecVersionHandler : IVersionFileHandler
{
    /// <inheritdoc />
    public bool CanHandle(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return false;
        }

        string extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension == ".nuspec";
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
            XElement? versionElement = FindVersionElement(doc);

            return versionElement?.Value;
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
            fileContent = "<package><metadata></metadata></package>";
        }

        XDocument doc = XDocument.Parse(fileContent);
        XElement? metadata = doc.Root?.Element("metadata");

        if (metadata == null)
        {
            metadata = new XElement("metadata");
            doc.Root?.Add(metadata);
        }

        XElement? versionElement = FindVersionElement(doc);

        if (versionElement == null)
        {
            versionElement = new XElement("version", newVersion);
            metadata.Add(versionElement);
        }
        else
        {
            versionElement.Value = newVersion;
        }

        string result = doc.ToString();

        return result;
    }

    /// <summary>
    /// Searches for the <c>&lt;version&gt;</c> element inside <c>&lt;metadata&gt;</c>.
    /// </summary>
    /// <param name="doc">The parsed XML document.</param>
    /// <returns>The version element, or <see langword="null"/> if not found.</returns>
    private static XElement? FindVersionElement(XDocument doc)
    {
        if (doc.Root == null)
        {
            return null;
        }

        XElement? metadata = doc.Root.Element("metadata");

        if (metadata != null)
        {
            XElement? version = metadata.Element("version");

            return version;
        }

        return null;
    }
}
