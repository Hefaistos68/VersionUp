namespace VersionUp.Tests
{
	using System;
	using System.Reflection;
	using System.Runtime.InteropServices;
	using System.Threading;
	using Moq;
	using NUnit.Framework;
	using Shouldly;
	using Microsoft.VisualStudio.Shell.Interop;
	using VersionUp.Dialogs;

	/// <summary>
	/// Unit tests for the <see cref="SetVersionDialog"/> class.
	/// </summary>
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class SetVersionDialogTests
	{
	    /// <summary>
	    /// Set up a mock Visual Studio environment for the dialog window.
	    /// </summary>
	    [OneTimeSetUp]
	    public void OneTimeSetUp()
	    {
	        var mockSettingsStore = new Mock<IVsSettingsStore>();
	        var mockSettingsManager = new Mock<IVsSettingsManager>();

	        IVsSettingsStore outStore = mockSettingsStore.Object;

	        mockSettingsManager
	            .Setup(m => m.GetReadOnlySettingsStore(It.IsAny<uint>(), out outStore))
	            .Returns(0);

	        MockServiceProvider mockServiceProvider = new MockServiceProvider(mockSettingsManager.Object);
	        Microsoft.VisualStudio.Shell.ServiceProvider serviceProvider = new Microsoft.VisualStudio.Shell.ServiceProvider(mockServiceProvider);

	        try
	        {
	            var field = typeof(Microsoft.VisualStudio.Shell.ServiceProvider).GetField("globalProvider", BindingFlags.Static | BindingFlags.NonPublic);

	            if (field != null)
	            {
	                field.SetValue(null, serviceProvider);
	            }
	        }
	        catch
	        {
	            // Ignore if reflection fails
	        }
	    }

	    /// <summary>
	    /// Verifies that the dialog initializes correctly with the provided version.
	    /// </summary>
	    [Test]
	    public void Constructor_ShouldInitializeWithCurrentVersion()
	    {
	        string currentVersion = "1.2.3.4";

	        SetVersionDialog dialog = new SetVersionDialog(currentVersion);

	        dialog.VersionResult.ShouldBe(currentVersion);
	    }

	    /// <summary>
	    /// Verifies that the dialog initializes with an empty string when the provided version is null.
	    /// </summary>
	    [Test]
	    public void Constructor_ShouldInitializeWithEmptyString_WhenCurrentVersionIsNull()
	    {
	        SetVersionDialog dialog = new SetVersionDialog(null!);

	        dialog.VersionResult.ShouldBe(string.Empty);
	    }

	    /// <summary>
	    /// Custom mock service provider implementing OLE IServiceProvider.
	    /// </summary>
	    private class MockServiceProvider : Microsoft.VisualStudio.OLE.Interop.IServiceProvider
	    {
	        private readonly object _settingsManager;

	        public MockServiceProvider(object settingsManager)
	        {
	            _settingsManager = settingsManager;
	        }

	        public int QueryService(ref Guid guidService, ref Guid riid, out IntPtr ppvObject)
	        {
	            ppvObject = IntPtr.Zero;

	            if (guidService == typeof(SVsSettingsManager).GUID)
	            {
	                IntPtr unknown = Marshal.GetIUnknownForObject(_settingsManager);
	                int hr = Marshal.QueryInterface(unknown, ref riid, out ppvObject);

	                Marshal.Release(unknown);

	                return hr;
	            }

	            return -2147467262; // E_NOINTERFACE
	        }
	    }
	}
}
