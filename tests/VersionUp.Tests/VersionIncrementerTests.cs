namespace VersionUp.Tests
{
	using System;
	using Moq;
	using NUnit.Framework;
	using Shouldly;
	using VersionUp;

	/// <summary>
	/// Unit tests for the <see cref="VersionIncrementer"/> class.
	/// </summary>
	[TestFixture]
	public class VersionIncrementerTests
	{
	    /// <summary>
	    /// Verifies that the constructor throws <see cref="ArgumentNullException"/> when the logger dependency is null.
	    /// </summary>
	    [Test]
	    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
	    {
	        Should.Throw<ArgumentNullException>(() =>
	        {
	            new VersionIncrementer(null!);
	        });
	    }

	    /// <summary>
	    /// Verifies that the Major version segment is correctly incremented and subsequent segments reset.
	    /// </summary>
	    [Test]
	    public void Increment_ShouldIncrementMajor_WhenMajorSegmentSelected()
	    {
	        Mock<IVersionLogger> mockLogger = new Mock<IVersionLogger>();
	        VersionIncrementer incrementer = new VersionIncrementer(mockLogger.Object);

	        string result = incrementer.Increment("1.2.3", VersionSegment.Major);

	        result.ShouldBe("2.0.0");
	        mockLogger.Verify(x => x.Log(It.IsAny<string>()), Times.Once);
	    }

	    /// <summary>
	    /// Verifies that the Minor version segment is correctly incremented and subsequent segments reset.
	    /// </summary>
	    [Test]
	    public void Increment_ShouldIncrementMinor_WhenMinorSegmentSelected()
	    {
	        Mock<IVersionLogger> mockLogger = new Mock<IVersionLogger>();
	        VersionIncrementer incrementer = new VersionIncrementer(mockLogger.Object);

	        string result = incrementer.Increment("1.2.3.4", VersionSegment.Minor);

	        result.ShouldBe("1.3.0");
	        mockLogger.Verify(x => x.Log(It.IsAny<string>()), Times.Once);
	    }

	    /// <summary>
	    /// Verifies that the Build version segment is correctly incremented.
	    /// </summary>
	    [Test]
	    public void Increment_ShouldIncrementBuild_WhenBuildSegmentSelected()
	    {
	        Mock<IVersionLogger> mockLogger = new Mock<IVersionLogger>();
	        VersionIncrementer incrementer = new VersionIncrementer(mockLogger.Object);

	        string result = incrementer.Increment("1.2.3.4", VersionSegment.Build);

	        result.ShouldBe("1.2.4");
	        mockLogger.Verify(x => x.Log(It.IsAny<string>()), Times.Once);
	    }

	    /// <summary>
	    /// Verifies that the Revision version segment is correctly incremented.
	    /// </summary>
	    [Test]
	    public void Increment_ShouldIncrementRevision_WhenRevisionSegmentSelected()
	    {
	        Mock<IVersionLogger> mockLogger = new Mock<IVersionLogger>();
	        VersionIncrementer incrementer = new VersionIncrementer(mockLogger.Object);

	        string result = incrementer.Increment("1.2.3.4", VersionSegment.Revision);

	        result.ShouldBe("1.2.3.5");
	        mockLogger.Verify(x => x.Log(It.IsAny<string>()), Times.Once);
	    }

	    /// <summary>
	    /// Verifies that a default version string is returned when parsing fails.
	    /// </summary>
	    [Test]
	    public void Increment_ShouldReturnDefaultVersion_WhenInputIsInvalid()
	    {
	        Mock<IVersionLogger> mockLogger = new Mock<IVersionLogger>();
	        VersionIncrementer incrementer = new VersionIncrementer(mockLogger.Object);

	        string result = incrementer.Increment("invalid-version", VersionSegment.Minor);

	        result.ShouldBe("1.0.0");
	        mockLogger.Verify(x => x.Log(It.Is<string>(msg => msg.Contains("Failed to parse"))), Times.Once);
	    }
	}
}
