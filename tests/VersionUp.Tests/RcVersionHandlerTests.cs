namespace VersionUp.Tests;

using NUnit.Framework;
using Shouldly;
using VersionUp;

/// <summary>
/// Unit tests for the <see cref="RcVersionHandler"/> class.
/// </summary>
[TestFixture]
public class RcVersionHandlerTests
{
    /// <summary>
    /// Verifies resource script targeting.
    /// </summary>
    [Test]
    public void CanHandle_ShouldReturnExpectedResults()
    {
        RcVersionHandler handler = new RcVersionHandler();

        handler.CanHandle("P:\\Source\\Resources.rc").ShouldBeTrue();
        handler.CanHandle("P:\\Source\\Resources.h").ShouldBeFalse();
    }

    /// <summary>
    /// Verifies parsing of value string in resource block.
    /// </summary>
    [Test]
    public void GetVersion_ShouldParseFromValueBlockCorrectly()
    {
        RcVersionHandler handler = new RcVersionHandler();
        string content = "VS_VERSION_INFO VERSIONINFO\n FILEVERSION 1,0,0,0\nBEGIN\n    BLOCK \"StringFileInfo\"\n    BEGIN\n        VALUE \"FileVersion\", \"2.3.4.5\"\n    END\nEND";

        string? result = handler.GetVersion(content);

        result.ShouldBe("2.3.4.5");
    }

    /// <summary>
    /// Verifies parsing of keyword block when value is absent.
    /// </summary>
    [Test]
    public void GetVersion_ShouldParseFromKeywordBlockCorrectly_WhenValueIsAbsent()
    {
        RcVersionHandler handler = new RcVersionHandler();
        string content = "VS_VERSION_INFO VERSIONINFO\n FILEVERSION 1,2,3,4\nBEGIN\nEND";

        string? result = handler.GetVersion(content);

        result.ShouldBe("1.2.3.4");
    }

    /// <summary>
    /// Verifies synchronization of versions in native RC script files.
    /// </summary>
    [Test]
    public void UpdateVersion_ShouldUpdateAllOccurrencesCorrectly()
    {
        RcVersionHandler handler = new RcVersionHandler();
        string content = "FILEVERSION 1,0,0,0\nPRODUCTVERSION 1,0,0,0\nVALUE \"FileVersion\", \"1.0.0.0\"\nVALUE \"ProductVersion\", \"1.0.0.0\"";

        string result = handler.UpdateVersion(content, "2.4.6.8");

        result.ShouldContain("FILEVERSION 2,4,6,8");
        result.ShouldContain("PRODUCTVERSION 2,4,6,8");
        result.ShouldContain("VALUE \"FileVersion\", \"2.4.6.8\"");
        result.ShouldContain("VALUE \"ProductVersion\", \"2.4.6.8\"");
    }
}
