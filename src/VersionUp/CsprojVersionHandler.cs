namespace VersionUp;

using System;
using System.IO;
using System.Xml.Linq;

/// <summary>
/// Handles parsing and updating version elements in MSBuild project and properties files.
/// </summary>
public class CsprojVersionHandler : IVersionFileHandler
{
    /// <inheritdoc />
    public bool CanHandle(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return false;
        }

        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        string fileName = Path.GetFileName(filePath).ToLowerInvariant();

        bool isProject = extension == ".csproj" || extension == ".fsproj" || extension == ".vbproj";
        bool isBuildProps = fileName == "directory.build.props" || fileName == "directory.build.targets";

        return isProject || isBuildProps;
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
            fileContent = "<Project><PropertyGroup></PropertyGroup></Project>";
        }

        XDocument doc = XDocument.Parse(fileContent);
        XElement? propertyGroup = doc.Root?.Element("PropertyGroup");

        if (propertyGroup == null)
        {
            propertyGroup = new XElement("PropertyGroup");
            doc.Root?.Add(propertyGroup);
        }

        XElement? versionElement = FindVersionElement(doc);

        if (versionElement == null)
        {
            versionElement = new XElement("Version", newVersion);
            propertyGroup.Add(versionElement);
        }
        else
        {
            versionElement.Value = newVersion;
        }

        string result = doc.ToString();

        return result;
    }

    /// <summary>
    /// Searches the XML document for the first <c>&lt;Version&gt;</c> or
    /// <c>&lt;PackageVersion&gt;</c> element inside any <c>&lt;PropertyGroup&gt;</c>.
    /// </summary>
    /// <param name="doc">The parsed XML document.</param>
    /// <returns>The version element, or <see langword="null"/> if not found.</returns>
    private static XElement? FindVersionElement(XDocument doc)
    {
        if (doc.Root == null)
        {
            return null;
        }

        foreach (XElement propertyGroup in doc.Root.Elements("PropertyGroup"))
        {
            XElement? version = propertyGroup.Element("Version");

            if (version != null)
            {
                return version;
            }

            XElement? packageVersion = propertyGroup.Element("PackageVersion");

            if (packageVersion != null)
            {
                return packageVersion;
            }
        }

        return null;
    }
}
