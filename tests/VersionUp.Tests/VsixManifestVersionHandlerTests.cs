namespace VersionUp.Tests
{
	using NUnit.Framework;
	using Shouldly;
	using VersionUp.VersionHandlers;

	/// <summary>
	/// Unit tests for the <see cref="VsixManifestVersionHandler"/> class.
	/// </summary>
	[TestFixture]
	public class VsixManifestVersionHandlerTests
	{
	    /// <summary>
	    /// Verifies that vsixmanifest files are targeted correctly.
	    /// </summary>
	    [Test]
	    public void CanHandle_ShouldReturnExpectedResults()
	    {
	        VsixManifestVersionHandler handler = new VsixManifestVersionHandler();

	        handler.CanHandle("P:\\Source\\source.extension.vsixmanifest").ShouldBeTrue();
	        handler.CanHandle("P:\\Source\\manifest.json").ShouldBeFalse();
	    }

	    /// <summary>
	    /// Verifies version parsing of extension manifest identity.
	    /// </summary>
	    [Test]
	    public void GetVersion_ShouldParseVersionCorrectly()
	    {
	        VsixManifestVersionHandler handler = new VsixManifestVersionHandler();
	        string xml = "<PackageManifest xmlns=\"http://schemas.microsoft.com/developer/vsx-schema/2011\"><Metadata><Identity Version=\"1.0.5\" /></Metadata></PackageManifest>";

	        string? result = handler.GetVersion(xml);

	        result.ShouldBe("1.0.5");
	    }

	    /// <summary>
	    /// Verifies version attribute updates.
	    /// </summary>
	    [Test]
	    public void UpdateVersion_ShouldUpdateVersionCorrectly()
	    {
	        VsixManifestVersionHandler handler = new VsixManifestVersionHandler();
	        string xml = "<PackageManifest xmlns=\"http://schemas.microsoft.com/developer/vsx-schema/2011\"><Metadata><Identity Version=\"1.0.5\" /></Metadata></PackageManifest>";

	        string result = handler.UpdateVersion(xml, "2.0.0");

	        result.ShouldContain("Version=\"2.0.0\"");
	    }
	}
}
