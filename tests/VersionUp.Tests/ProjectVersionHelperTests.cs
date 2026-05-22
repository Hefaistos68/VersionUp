namespace VersionUp.Tests
{
	using System.Collections.Generic;
	using System.IO;
	using Moq;
	using NUnit.Framework;
	using Shouldly;
	using EnvDTE;
	using VersionUp.VersionHandlers;

	/// <summary>
	/// Unit tests for the <see cref="ProjectVersionHelper"/> class.
	/// </summary>
	[TestFixture]
	public class ProjectVersionHelperTests
	{
	    /// <summary>
	    /// Verifies that GetProjectVersion returns null when project is null.
	    /// </summary>
	    [Test]
	    public void GetProjectVersion_WithNullProject_ShouldReturnNull()
	    {
	        ProjectVersionHelper.GetProjectVersion(null!).ShouldBeNull();
	    }

	    /// <summary>
	    /// Verifies that GetProjectVersion returns null when project has no valid version files.
	    /// </summary>
	    [Test]
	    public void GetProjectVersion_WithMockedProjectNoFiles_ShouldReturnNull()
	    {
	        Mock<Project> mockProject = new();

	        mockProject.Setup(p => p.FullName).Returns(string.Empty);

	        string? result = ProjectVersionHelper.GetProjectVersion(mockProject.Object);

	        result.ShouldBeNull();
	    }

	    /// <summary>
	    /// Verifies that GetProjectVersionDiagnostics returns empty diagnostics when project is null.
	    /// </summary>
	    [Test]
	    public void GetProjectVersionDiagnostics_WithNullProject_ShouldReturnEmptyDiagnostics()
	    {
	        ProjectVersionDiagnostics result = ProjectVersionHelper.GetProjectVersionDiagnostics(null!);

	        result.ShouldNotBeNull();
	        result.PrimaryVersion.ShouldBeNull();
	        result.Versions.ShouldBeEmpty();
	        result.IsOutOfSync.ShouldBeFalse();
	    }

	    /// <summary>
	    /// Verifies that GetProjectVersionDiagnostics detects mismatched versions across project and assembly info files.
	    /// </summary>
	    [Test]
	    public void GetProjectVersionDiagnostics_WithMismatchedVersions_ShouldReturnOutOfSyncDiagnostics()
	    {
	        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

	        Directory.CreateDirectory(tempDir);

	        string tempProjFile = Path.Combine(tempDir, "TestProject.csproj");
	        string tempAssemblyInfoFile = Path.Combine(tempDir, "AssemblyInfo.cs");

	        try
	        {
	            File.WriteAllText(tempProjFile, "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><Version>1.2.3</Version></PropertyGroup></Project>");
	            File.WriteAllText(tempAssemblyInfoFile, "[assembly: AssemblyVersion(\"1.2.4\")]");

	            Mock<ProjectItem> mockItem = new();

	            mockItem.Setup(i => i.FileCount).Returns(1);
	            mockItem.Setup(i => i.get_FileNames(It.IsAny<short>())).Returns(tempAssemblyInfoFile);

	            List<ProjectItem> itemsList = new() { mockItem.Object };
	            Mock<ProjectItems> mockItems = new();

	            mockItems.Setup(m => m.GetEnumerator()).Returns(itemsList.GetEnumerator());

	            Mock<Project> mockProject = new();

	            mockProject.Setup(p => p.FullName).Returns(tempProjFile);
	            mockProject.Setup(p => p.ProjectItems).Returns(mockItems.Object);

	            ProjectVersionDiagnostics result = ProjectVersionHelper.GetProjectVersionDiagnostics(mockProject.Object);

	            result.ShouldNotBeNull();
	            result.IsOutOfSync.ShouldBeTrue();
	            result.PrimaryVersion.ShouldBe("1.2.3");
	            result.Versions.Count.ShouldBe(2);
	            result.Versions[0].Version.ShouldBe("1.2.3");
	            result.Versions[1].Version.ShouldBe("1.2.4");
	        }
	        finally
	        {
	            if (Directory.Exists(tempDir))
	            {
	                Directory.Delete(tempDir, true);
	            }
	        }
	    }

	    /// <summary>
	    /// Verifies that GetProjectVersionDiagnostics detects matching versions across project and assembly info files.
	    /// </summary>
	    [Test]
	    public void GetProjectVersionDiagnostics_WithMatchingVersions_ShouldReturnInSyncDiagnostics()
	    {
	        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

	        Directory.CreateDirectory(tempDir);

	        string tempProjFile = Path.Combine(tempDir, "TestProject.csproj");
	        string tempAssemblyInfoFile = Path.Combine(tempDir, "AssemblyInfo.cs");

	        try
	        {
	            File.WriteAllText(tempProjFile, "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><Version>1.2.3</Version></PropertyGroup></Project>");
	            File.WriteAllText(tempAssemblyInfoFile, "[assembly: AssemblyVersion(\"1.2.3\")]");

	            Mock<ProjectItem> mockItem = new();

	            mockItem.Setup(i => i.FileCount).Returns(1);
	            mockItem.Setup(i => i.get_FileNames(It.IsAny<short>())).Returns(tempAssemblyInfoFile);

	            List<ProjectItem> itemsList = new() { mockItem.Object };
	            Mock<ProjectItems> mockItems = new();

	            mockItems.Setup(m => m.GetEnumerator()).Returns(itemsList.GetEnumerator());

	            Mock<Project> mockProject = new();

	            mockProject.Setup(p => p.FullName).Returns(tempProjFile);
	            mockProject.Setup(p => p.ProjectItems).Returns(mockItems.Object);

	            ProjectVersionDiagnostics result = ProjectVersionHelper.GetProjectVersionDiagnostics(mockProject.Object);

	            result.ShouldNotBeNull();
	            result.IsOutOfSync.ShouldBeFalse();
	            result.PrimaryVersion.ShouldBe("1.2.3");
	            result.Versions.Count.ShouldBe(2);
	            result.Versions[0].Version.ShouldBe("1.2.3");
	            result.Versions[1].Version.ShouldBe("1.2.3");
	        }
	        finally
	        {
	            if (Directory.Exists(tempDir))
	            {
	                Directory.Delete(tempDir, true);
	            }
	        }
	    }

	    /// <summary>
	    /// Verifies that GetProjectVersionDiagnostics handles placeholder versions like $version$ as correct and in sync.
	    /// </summary>
	    [Test]
	    public void GetProjectVersionDiagnostics_WithPlaceholderVersion_ShouldReturnInSyncDiagnostics()
	    {
	        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

	        Directory.CreateDirectory(tempDir);

	        string tempProjFile = Path.Combine(tempDir, "TestProject.csproj");
	        string tempNuspecFile = Path.Combine(tempDir, "TestProject.nuspec");

	        try
	        {
	            File.WriteAllText(tempProjFile, "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><Version>1.2.3</Version></PropertyGroup></Project>");
	            File.WriteAllText(tempNuspecFile, "<package><metadata><version>$version$</version></metadata></package>");

	            Mock<ProjectItem> mockItem = new();

	            mockItem.Setup(i => i.FileCount).Returns(1);
	            mockItem.Setup(i => i.get_FileNames(It.IsAny<short>())).Returns(tempNuspecFile);

	            List<ProjectItem> itemsList = new() { mockItem.Object };
	            Mock<ProjectItems> mockItems = new();

	            mockItems.Setup(m => m.GetEnumerator()).Returns(itemsList.GetEnumerator());

	            Mock<Project> mockProject = new();

	            mockProject.Setup(p => p.FullName).Returns(tempProjFile);
	            mockProject.Setup(p => p.ProjectItems).Returns(mockItems.Object);

	            ProjectVersionDiagnostics result = ProjectVersionHelper.GetProjectVersionDiagnostics(mockProject.Object);

	            result.ShouldNotBeNull();
	            result.IsOutOfSync.ShouldBeFalse();
	            result.PrimaryVersion.ShouldBe("1.2.3");
	            result.Versions.Count.ShouldBe(2);
	            result.Versions[0].Version.ShouldBe("1.2.3");
	            result.Versions[1].Version.ShouldBe("$version$");
	        }
	        finally
	        {
	            if (Directory.Exists(tempDir))
	            {
	                Directory.Delete(tempDir, true);
	            }
	        }
	    }
	}
}
