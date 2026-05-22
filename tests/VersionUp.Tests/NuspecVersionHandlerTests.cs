namespace VersionUp.Tests
{
	using NUnit.Framework;
	using Shouldly;
	using VersionUp.VersionHandlers;

	/// <summary>
	/// Unit tests for the <see cref="NuspecVersionHandler"/> class.
	/// </summary>
	[TestFixture]
	public class NuspecVersionHandlerTests
	{
	    /// <summary>
	    /// Verifies that the handler identifies .nuspec files.
	    /// </summary>
	    [Test]
	    public void CanHandle_ShouldReturnExpectedResults()
	    {
	        NuspecVersionHandler handler = new NuspecVersionHandler();

	        handler.CanHandle("P:\\Source\\Package.nuspec").ShouldBeTrue();
	        handler.CanHandle("P:\\Source\\Package.xml").ShouldBeFalse();
	    }

	    /// <summary>
	    /// Verifies version parsing.
	    /// </summary>
	    [Test]
	    public void GetVersion_ShouldParseVersionCorrectly()
	    {
	        NuspecVersionHandler handler = new NuspecVersionHandler();
	        string xml = "<package><metadata><version>1.4.5</version></metadata></package>";

	        string? result = handler.GetVersion(xml);

	        result.ShouldBe("1.4.5");
	    }

	    /// <summary>
	    /// Verifies version updates.
	    /// </summary>
	    [Test]
	    public void UpdateVersion_ShouldUpdateVersionCorrectly()
	    {
	        NuspecVersionHandler handler = new NuspecVersionHandler();
	        string xml = "<package><metadata><version>1.4.5</version></metadata></package>";

	        string result = handler.UpdateVersion(xml, "2.0.0");

	        result.ShouldContain("<version>2.0.0</version>");
	    }
	}
}
