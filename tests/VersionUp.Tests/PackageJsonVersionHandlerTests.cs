namespace VersionUp.Tests
{
	using NUnit.Framework;
	using Shouldly;
	using VersionUp.VersionHandlers;

	/// <summary>
	/// Unit tests for the <see cref="PackageJsonVersionHandler"/> class.
	/// </summary>
	[TestFixture]
	public class PackageJsonVersionHandlerTests
	{
	    /// <summary>
	    /// Verifies that package.json files are targeted correctly.
	    /// </summary>
	    [Test]
	    public void CanHandle_ShouldReturnExpectedResults()
	    {
	        PackageJsonVersionHandler handler = new PackageJsonVersionHandler();

	        handler.CanHandle("P:\\Source\\package.json").ShouldBeTrue();
	        handler.CanHandle("P:\\Source\\Package-lock.json").ShouldBeFalse();
	    }

	    /// <summary>
	    /// Verifies that the version key is parsed.
	    /// </summary>
	    [Test]
	    public void GetVersion_ShouldParseVersionCorrectly()
	    {
	        PackageJsonVersionHandler handler = new PackageJsonVersionHandler();
	        string json = "{\n  \"name\": \"my-app\",\n  \"version\": \"1.0.0-beta\"\n}";

	        string? result = handler.GetVersion(json);

	        result.ShouldBe("1.0.0-beta");
	    }

	    /// <summary>
	    /// Verifies that the version is updated without destroying JSON layout.
	    /// </summary>
	    [Test]
	    public void UpdateVersion_ShouldUpdateVersionCorrectly()
	    {
	        PackageJsonVersionHandler handler = new PackageJsonVersionHandler();
	        string json = "{\n  \"name\": \"my-app\",\n  \"version\": \"1.0.0-beta\"\n}";

	        string result = handler.UpdateVersion(json, "2.0.0");

	        result.ShouldContain("\"version\": \"2.0.0\"");
	    }
	}
}
