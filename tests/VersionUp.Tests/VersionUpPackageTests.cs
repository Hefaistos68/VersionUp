namespace VersionUp.Tests
{
	using NUnit.Framework;
	using Shouldly;
	using VersionUp;

	/// <summary>
	/// Unit tests for the <see cref="VersionUpPackage"/> class.
	/// </summary>
	[TestFixture]
	public class VersionUpPackageTests
	{
	    /// <summary>
	    /// Verifies that the PackageGuidString is correct and matching the defined extension ID.
	    /// </summary>
	    [Test]
	    public void PackageGuidString_ShouldBeCorrect()
	    {
	        string guid = VersionUpPackage.PackageGuidString;

	        guid.ShouldBe("d3f962f7-a630-4c98-9b21-7b1a9908f87c");
	    }
	}
}
