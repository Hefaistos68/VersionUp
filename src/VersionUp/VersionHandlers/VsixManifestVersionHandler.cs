using System;
using System.IO;
using System.Xml.Linq;

namespace VersionUp.VersionHandlers
{
	/// <summary>
	/// Handles parsing and updating the identity version in Visual Studio VSIX extension manifests.
	/// </summary>
	public class VsixManifestVersionHandler : IVersionFileHandler
	{
	    /// <inheritdoc />
	    public bool CanHandle(string filePath)
	    {
	        if (string.IsNullOrEmpty(filePath))
	        {
	            return false;
	        }

	        string fileName = Path.GetFileName(filePath).ToLowerInvariant();

	        return fileName == "source.extension.vsixmanifest";
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

	            return identityElement?.Attribute("Version")?.Value;
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

	        identityElement?.SetAttributeValue("Version", newVersion);

	        string result = doc.ToString();

	        return result;
	    }

	    /// <summary>
	    /// Searches for the <c>&lt;Identity&gt;</c> element inside <c>&lt;Metadata&gt;</c>,
	    /// respecting the VSIX manifest XML namespace.
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
	        XElement? metadata = doc.Root.Element(ns + "Metadata");

	        if (metadata != null)
	        {
	            XElement? identity = metadata.Element(ns + "Identity");

	            return identity;
	        }

	        return null;
	    }
	}
}
