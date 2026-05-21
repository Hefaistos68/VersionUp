namespace VersionUp;

using System;
using System.IO;
using System.Xml.Linq;

/// <summary>
/// Handles parsing and updating the version attribute in WiX installer setup (.wxs) files.
/// </summary>
public class WxsVersionHandler : IVersionFileHandler
{
    /// <inheritdoc />
    public bool CanHandle(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return false;
        }

        string extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension == ".wxs";
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
            XElement? versionedElement = FindVersionedElement(doc);

            return versionedElement?.Attribute("Version")?.Value;
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
        XElement? versionedElement = FindVersionedElement(doc);

        if (versionedElement != null)
        {
            versionedElement.SetAttributeValue("Version", newVersion);
        }

        string result = doc.ToString();

        return result;
    }

    /// <summary>
    /// Searches the WiX document for the first versioned element: <c>&lt;Product&gt;</c>,
    /// <c>&lt;Package&gt;</c>, or <c>&lt;Module&gt;</c>, in that order of preference.
    /// </summary>
    /// <param name="doc">The parsed XML document.</param>
    /// <returns>The first matching element, or <see langword="null"/> if none is found.</returns>
    private static XElement? FindVersionedElement(XDocument doc)
    {
        if (doc.Root == null)
        {
            return null;
        }

        XNamespace ns = doc.Root.Name.Namespace;
        XElement? product = doc.Root.Element(ns + "Product");

        if (product != null)
        {
            return product;
        }

        XElement? package = doc.Root.Element(ns + "Package");

        if (package != null)
        {
            return package;
        }

        XElement? module = doc.Root.Element(ns + "Module");

        if (module != null)
        {
            return module;
        }

        return null;
    }
}
