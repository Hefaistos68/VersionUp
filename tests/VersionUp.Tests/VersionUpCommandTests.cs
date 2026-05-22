namespace VersionUp.Tests
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Runtime.InteropServices;
	using System.Threading;
	using System.Windows.Threading;
	using Moq;
	using NUnit.Framework;
	using Shouldly;
	using VersionUp;
	using Microsoft.VisualStudio;
	using Microsoft.VisualStudio.Shell;
	using Microsoft.VisualStudio.Shell.Interop;

	/// <summary>
	/// Unit tests for settings serialization in the <see cref="VersionUpCommand"/> class.
	/// </summary>
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class VersionUpCommandTests
	{
	    private Mock<IVsWritableSettingsStore> _mockWritableStore = null!;
	    private Mock<IVsSettingsManager> _mockSettingsManager = null!;

	    /// <summary>
	    /// Sets up the mock Visual Studio service provider, settings manager, and dispatcher before each test.
	    /// </summary>
	    [SetUp]
	    public void SetUp()
	    {
	        _mockWritableStore = new Mock<IVsWritableSettingsStore>();
	        _mockSettingsManager = new Mock<IVsSettingsManager>();

	        IVsWritableSettingsStore outStore = _mockWritableStore.Object;

	        _mockSettingsManager
	            .Setup(m => m.GetWritableSettingsStore(It.IsAny<uint>(), out outStore))
	            .Returns(VSConstants.S_OK);

	        MockServiceProvider mockServiceProvider = new MockServiceProvider(_mockSettingsManager.Object);
	        Microsoft.VisualStudio.Shell.ServiceProvider serviceProvider = new Microsoft.VisualStudio.Shell.ServiceProvider(mockServiceProvider);

	        try
	        {
	            FieldInfo? fieldProvider = typeof(Microsoft.VisualStudio.Shell.ServiceProvider).GetField("globalProvider", BindingFlags.Static | BindingFlags.NonPublic);

	            if (fieldProvider != null)
	            {
	                fieldProvider.SetValue(null, serviceProvider);
	            }

	            // Force initialization of ThreadHelper.Generic to set up the singleton
	            var generic = ThreadHelper.Generic;

	            if (generic != null)
	            {
	                FieldInfo? fieldDispatcher = typeof(ThreadHelper).GetField("uiThreadDispatcher", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance);

	                if (fieldDispatcher != null)
	                {
	                    fieldDispatcher.SetValue(fieldDispatcher.IsStatic ? null : generic, Dispatcher.CurrentDispatcher);
	                }
	            }
	        }
	        catch
	        {
	            // Ignore if reflection fails
	        }
	    }

	    /// <summary>
	    /// Verifies that <c>SaveAlignmentDecision</c> correctly creates the collection and writes the boolean setting.
	    /// </summary>
	    [Test]
	    public void SaveAlignmentDecision_ShouldCreateCollectionAndSetBool()
	    {
	        string projectPath = @"C:\TestProject\App.csproj";
	        int exists = 0;

	        _mockWritableStore
	            .Setup(s => s.CollectionExists(It.IsAny<string>(), out exists))
	            .Returns(VSConstants.S_OK);

	        _mockWritableStore
	            .Setup(s => s.CreateCollection(It.IsAny<string>()))
	            .Returns(VSConstants.S_OK);

	        MethodInfo? saveMethod = typeof(VersionUpCommand).GetMethod("SaveAlignmentDecision", BindingFlags.Static | BindingFlags.NonPublic);

	        saveMethod.ShouldNotBeNull();
	        saveMethod.Invoke(null, new object[] { projectPath, true });

	        _mockWritableStore.Verify(s => s.CreateCollection(@"VersionUp\AlignmentDecisions"), Times.Once);
	        _mockWritableStore.Verify(s => s.SetBool(@"VersionUp\AlignmentDecisions", projectPath, 1), Times.Once);
	    }

	    /// <summary>
	    /// Verifies that <c>LoadAlignmentDecisions</c> correctly reads properties and populates the dictionary cache.
	    /// </summary>
	    [Test]
	    public void LoadAlignmentDecisions_ShouldReadSettingsIntoCache()
	    {
	        string projectPath = @"C:\TestProject\App.csproj";
	        int exists = 1;
	        uint propertyCount = 1;
	        string propertyName = projectPath;
	        int val = 1;

	        _mockWritableStore
	            .Setup(s => s.CollectionExists(It.IsAny<string>(), out exists))
	            .Returns(VSConstants.S_OK);

	        _mockWritableStore
	            .Setup(s => s.GetPropertyCount(It.IsAny<string>(), out propertyCount))
	            .Returns(VSConstants.S_OK);

	        _mockWritableStore
	            .Setup(s => s.GetPropertyName(It.IsAny<string>(), 0, out propertyName))
	            .Returns(VSConstants.S_OK);

	        _mockWritableStore
	            .Setup(s => s.GetBool(It.IsAny<string>(), projectPath, out val))
	            .Returns(VSConstants.S_OK);

	        MethodInfo? loadMethod = typeof(VersionUpCommand).GetMethod("LoadAlignmentDecisions", BindingFlags.Static | BindingFlags.NonPublic);

	        loadMethod.ShouldNotBeNull();
	        loadMethod.Invoke(null, null);

	        FieldInfo? dictField = typeof(VersionUpCommand).GetField("StoredAlignDecisions", BindingFlags.Static | BindingFlags.NonPublic);

	        dictField.ShouldNotBeNull();

	        var dict = dictField.GetValue(null) as Dictionary<string, bool>;

	        dict.ShouldNotBeNull();
	        dict.ContainsKey(projectPath).ShouldBeTrue();
	        dict[projectPath].ShouldBeTrue();
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
