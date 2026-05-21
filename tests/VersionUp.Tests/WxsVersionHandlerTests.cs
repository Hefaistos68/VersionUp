namespace VersionUp.Tests;

using NUnit.Framework;
using Shouldly;
using VersionUp;

/// <summary>
/// Unit tests for the <see cref="WxsVersionHandler"/> class.
/// </summary>
[TestFixture]
public class WxsVersionHandlerTests
{
    /// <summary>
    /// Verifies WiX file targeting.
    /// </summary>
    [Test]
    public void CanHandle_ShouldReturnExpectedResults()
    {
        WxsVersionHandler handler = new WxsVersionHandler();

        handler.CanHandle("P:\\Source\\Setup.wxs").ShouldBeTrue();
        handler.CanHandle("P:\\Source\\Setup.wxi").ShouldBeFalse();
    }

    /// <summary>
    /// Verifies version parsing of WiX v3 Product definitions.
    /// </summary>
    [Test]
    public void GetVersion_ShouldParseVersionFromProductCorrectly()
    {
        WxsVersionHandler handler = new WxsVersionHandler();
        string xml = "<Wix xmlns=\"http://schemas.microsoft.com/wix/2006/wi\"><Product Version=\"1.0.3.0\" /></Wix>";

        string? result = handler.GetVersion(xml);

        result.ShouldBe("1.0.3.0");
    }

    /// <summary>
    /// Verifies version parsing of WiX v4 Package definitions.
    /// </summary>
    [Test]
    public void GetVersion_ShouldParseVersionFromPackageCorrectly()
    {
        WxsVersionHandler handler = new WxsVersionHandler();
        string xml = "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\"><Package Version=\"4.0.0.1\" /></Wix>";

        string? result = handler.GetVersion(xml);

        result.ShouldBe("4.0.0.1");
    }

    /// <summary>
    /// Verifies version updates.
    /// </summary>
    [Test]
    public void UpdateVersion_ShouldUpdateVersionCorrectly()
    {
        WxsVersionHandler handler = new WxsVersionHandler();
        string xml = "<Wix xmlns=\"http://schemas.microsoft.com/wix/2006/wi\"><Product Version=\"1.0.3.0\" /></Wix>";

        string result = handler.UpdateVersion(xml, "2.0.0.0");

        result.ShouldContain("Version=\"2.0.0.0\"");
    }
}
