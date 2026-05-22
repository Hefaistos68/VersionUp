namespace VersionUp.Tests
{
	using NUnit.Framework;
	using Shouldly;
	using VersionUp.VersionHandlers;

	/// <summary>
	/// Unit tests for the <see cref="AppxManifestVersionHandler"/> class.
	/// </summary>
	[TestFixture]
	public class AppxManifestVersionHandlerTests
	{
	    /// <summary>
	    /// Verifies appxmanifest targeting.
	    /// </summary>
	    [Test]
	    public void CanHandle_ShouldReturnExpectedResults()
	    {
	        AppxManifestVersionHandler handler = new AppxManifestVersionHandler();

	        handler.CanHandle("P:\\Source\\package.appxmanifest").ShouldBeTrue();
	        handler.CanHandle("P:\\Source\\AndroidManifest.xml").ShouldBeFalse();
	    }

	    /// <summary>
	    /// Verifies version parsing of identity attribute.
	    /// </summary>
	    [Test]
	    public void GetVersion_ShouldParseVersionCorrectly()
	    {
	        AppxManifestVersionHandler handler = new AppxManifestVersionHandler();
	        string xml = "<Package xmlns=\"http://schemas.microsoft.com/appx/manifest/foundation/windows10\"><Identity Version=\"1.2.3.4\" /></Package>";

	        string? result = handler.GetVersion(xml);

	        result.ShouldBe("1.2.3.4");
	    }

	    /// <summary>
	    /// Verifies version updates.
	    /// </summary>
	    [Test]
	    public void UpdateVersion_ShouldUpdateVersionCorrectly()
	    {
	        AppxManifestVersionHandler handler = new AppxManifestVersionHandler();
	        string xml = "<Package xmlns=\"http://schemas.microsoft.com/appx/manifest/foundation/windows10\"><Identity Version=\"1.2.3.4\" /></Package>";

	        string result = handler.UpdateVersion(xml, "2.0.0.0");

	        result.ShouldContain("Version=\"2.0.0.0\"");
	    }

	    /// <summary>
	    /// Verifies version parsing of identity attribute with fewer than 4 segments.
	    /// </summary>
	    [Test]
	    public void GetVersion_ShouldPadVersionToFourSegments()
	    {
	        AppxManifestVersionHandler handler = new AppxManifestVersionHandler();
	        string xml = "<Package xmlns=\"http://schemas.microsoft.com/appx/manifest/foundation/windows10\"><Identity Version=\"1.2\" /></Package>";

	        string? result = handler.GetVersion(xml);

	        result.ShouldBe("1.2.0.0");
	    }

	    /// <summary>
	    /// Verifies version updates with fewer than 4 segments.
	    /// </summary>
	    [Test]
	    public void UpdateVersion_ShouldPadVersionToFourSegments()
	    {
	        AppxManifestVersionHandler handler = new AppxManifestVersionHandler();
	        string xml = "<Package xmlns=\"http://schemas.microsoft.com/appx/manifest/foundation/windows10\"><Identity Version=\"1.2.3.4\" /></Package>";

	        string result = handler.UpdateVersion(xml, "2.0");

	        result.ShouldContain("Version=\"2.0.0.0\"");
	    }

	    /// <summary>
	    /// Verifies version updates with more than 4 segments.
	    /// </summary>
	    [Test]
	    public void UpdateVersion_ShouldTruncateVersionToFourSegments()
	    {
	        AppxManifestVersionHandler handler = new AppxManifestVersionHandler();
	        string xml = "<Package xmlns=\"http://schemas.microsoft.com/appx/manifest/foundation/windows10\"><Identity Version=\"1.2.3.4\" /></Package>";

	        string result = handler.UpdateVersion(xml, "2.0.3.4.5");

	        result.ShouldContain("Version=\"2.0.3.4\"");
	    }
	}
}
