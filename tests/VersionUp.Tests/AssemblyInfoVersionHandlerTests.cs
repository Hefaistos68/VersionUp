namespace VersionUp.Tests
{
	using NUnit.Framework;
	using Shouldly;
	using VersionUp.VersionHandlers;

	/// <summary>
	/// Unit tests for the <see cref="AssemblyInfoVersionHandler"/> class.
	/// </summary>
	[TestFixture]
	public class AssemblyInfoVersionHandlerTests
	{
	    /// <summary>
	    /// Verifies that AssemblyInfo file types are matched correctly.
	    /// </summary>
	    [Test]
	    public void CanHandle_ShouldReturnExpectedResults()
	    {
	        AssemblyInfoVersionHandler handler = new AssemblyInfoVersionHandler();

	        handler.CanHandle("P:\\Source\\Properties\\AssemblyInfo.cs").ShouldBeTrue();
	        handler.CanHandle("P:\\Source\\MyProject\\AssemblyInfo.vb").ShouldBeTrue();
	        handler.CanHandle("P:\\Source\\MyProject\\AssemblyInfo.fs").ShouldBeTrue();
	        handler.CanHandle("P:\\Source\\MyProject\\AssemblyInfo.cs").ShouldBeTrue();
	        handler.CanHandle("P:\\Source\\MyProject\\Program.cs").ShouldBeFalse();
	    }

	    /// <summary>
	    /// Verifies that the version attributes are parsed correctly.
	    /// </summary>
	    [Test]
	    public void GetVersion_ShouldParseAssemblyVersionCorrectly()
	    {
	        AssemblyInfoVersionHandler handler = new AssemblyInfoVersionHandler();
	        string content = "[assembly: AssemblyVersion(\"1.2.3.4\")]\n[assembly: AssemblyFileVersion(\"1.2.3.4\")]";

	        string? result = handler.GetVersion(content);

	        result.ShouldBe("1.2.3.4");
	    }

	    /// <summary>
	    /// Verifies that the version attributes are updated correctly in C#.
	    /// </summary>
	    [Test]
	    public void UpdateVersion_ShouldUpdateAssemblyInfoCorrectly()
	    {
	        AssemblyInfoVersionHandler handler = new AssemblyInfoVersionHandler();
	        string content = "[assembly: AssemblyVersion(\"1.2.3.4\")]\n[assembly: AssemblyFileVersion(\"1.0.0.0\")]";

	        string result = handler.UpdateVersion(content, "2.0.0.0");

	        result.ShouldContain("[assembly: AssemblyVersion(\"2.0.0.0\")]");
	        result.ShouldContain("[assembly: AssemblyFileVersion(\"2.0.0.0\")]");
	    }
	}
}
