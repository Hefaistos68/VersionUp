namespace VersionUp.Tests;

using NUnit.Framework;
using Shouldly;
using VersionUp;

/// <summary>
/// Unit tests for the <see cref="CsprojVersionHandler"/> class.
/// </summary>
[TestFixture]
public class CsprojVersionHandlerTests
{
    /// <summary>
    /// Verifies that the handler correctly identifies supported files based on path/extension.
    /// </summary>
    [Test]
    public void CanHandle_ShouldReturnExpectedResults()
    {
        CsprojVersionHandler handler = new CsprojVersionHandler();

        handler.CanHandle("P:\\Source\\MyProject.csproj").ShouldBeTrue();
        handler.CanHandle("P:\\Source\\MyFSharpProject.fsproj").ShouldBeTrue();
        handler.CanHandle("P:\\Source\\MyVbProject.vbproj").ShouldBeTrue();
        handler.CanHandle("P:\\Source\\Directory.Build.props").ShouldBeTrue();
        handler.CanHandle("P:\\Source\\Directory.Build.targets").ShouldBeTrue();
        handler.CanHandle("P:\\Source\\Program.cs").ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that GetVersion parses the version from MSBuild XML format.
    /// </summary>
    [Test]
    public void GetVersion_ShouldParseVersionCorrectly()
    {
        CsprojVersionHandler handler = new CsprojVersionHandler();
        string xml = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><Version>1.2.3</Version></PropertyGroup></Project>";

        string? result = handler.GetVersion(xml);

        result.ShouldBe("1.2.3");
    }

    /// <summary>
    /// Verifies that UpdateVersion modifies the version element inside MSBuild XML.
    /// </summary>
    [Test]
    public void UpdateVersion_ShouldUpdateVersionCorrectly()
    {
        CsprojVersionHandler handler = new CsprojVersionHandler();
        string xml = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><Version>1.2.3</Version></PropertyGroup></Project>";

        string result = handler.UpdateVersion(xml, "2.0.0");

        result.ShouldContain("<Version>2.0.0</Version>");
    }

    /// <summary>
    /// Verifies that UpdateVersion inserts a new version element when no version element exists.
    /// </summary>
    [Test]
    public void UpdateVersion_WithNoExistingVersion_ShouldInsertVersionElement()
    {
        CsprojVersionHandler handler = new CsprojVersionHandler();

        string xml = "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><Nullable>enable</Nullable></PropertyGroup></Project>";

        string result = handler.UpdateVersion(xml, "1.0.0");

        result.ShouldContain("<Version>1.0.0</Version>");
    }
}

