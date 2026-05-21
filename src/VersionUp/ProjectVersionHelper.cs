#pragma warning disable VSTHRD010

namespace VersionUp;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

/// <summary>
/// Provides utility methods for traversing solution projects and resolving their versions.
/// </summary>
public static class ProjectVersionHelper
{
	private static bool? _isRunningInVisualStudio;

	private static bool IsRunningInVisualStudio
	{
		get
		{
			if (!_isRunningInVisualStudio.HasValue)
			{
				try
				{
					using (System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess())
					{
						_isRunningInVisualStudio = process.ProcessName.Equals("devenv", StringComparison.OrdinalIgnoreCase);
					}
				}
				catch
				{
					_isRunningInVisualStudio = false;
				}
			}

			return _isRunningInVisualStudio.Value;
		}
	}

	/// <summary>
	/// Traverses the active solution recursively to find all projects.
	/// </summary>
	/// <returns>A list of all found <see cref="Project"/> objects in the solution.</returns>
	public static List<Project> GetAllProjects()
	{
		if (IsRunningInVisualStudio)
		{
			VerifyUIThread();
		}

		List<Project> projects = new();

		EnvDTE80.DTE2? dte = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as EnvDTE80.DTE2;

		if (dte == null || dte.Solution == null)
		{
			return projects;
		}

		foreach (Project project in dte.Solution.Projects)
		{
			if (project != null)
			{
				GetProjectsRecursive(project, projects);
			}
		}

		return projects;
	}

	/// <summary>
	/// Gets the version of a given project by checking its project file or recursively searching its items.
	/// </summary>
	/// <param name="project">The Visual Studio project.</param>
	/// <returns>The resolved version string, or <see langword="null"/> if not found.</returns>
	public static string? GetProjectVersion(Project project)
	{
		if (IsRunningInVisualStudio)
		{
			VerifyUIThread();
		}

		if (project == null || string.IsNullOrEmpty(project.FullName))
		{
			return null;
		}

		string projectPath = project.FullName;

		if (File.Exists(projectPath))
		{
			IVersionFileHandler? handler = VersionUpCommand.GetHandlerForFile(projectPath);

			if (handler != null)
			{
				try
				{
					string content = File.ReadAllText(projectPath);

					string? ver = handler.GetVersion(content);

					if (!string.IsNullOrEmpty(ver))
					{
						return ver;
					}
				}
				catch
				{
					// Ignore disk read or parsing errors
				}
			}
		}

		try
		{
			return FindVersionInProjectItems(project.ProjectItems);
		}
		catch
		{
			return null;
		}
	}

	/// <summary>
	/// Asserts that the code is executing on the Visual Studio main thread.
	/// </summary>
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
	private static void VerifyUIThread()
	{
		ThreadHelper.ThrowIfNotOnUIThread();
	}

	/// <summary>
	/// Recursively traverses a solution item or folder to gather projects.
	/// </summary>
	/// <param name="project">The project or solution folder.</param>
	/// <param name="list">The collection to populate with projects.</param>
	private static void GetProjectsRecursive(Project project, List<Project> list)
	{
		if (IsRunningInVisualStudio)
		{
			VerifyUIThread();
		}

		if (project == null)
		{
			return;
		}

		try
		{
			if (project.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
			{
				foreach (ProjectItem item in project.ProjectItems)
				{
					if (item.SubProject != null)
					{
						GetProjectsRecursive(item.SubProject, list);
					}
				}
			}
			else if (project.Kind == EnvDTE.Constants.vsProjectKindUnmodeled)
			{
				// Skip unmodeled projects which are not real projects or unloaded projects
			}
			else if (!string.IsNullOrEmpty(project.FullName))
			{
				list.Add(project);
			}
		}
		catch (Exception ex)
		{
			// Handle or log the exception if necessary
			Debug.WriteLine($"Error processing project '{project.Name}': {ex.Message}");
		}
	}

	/// <summary>
	/// Recursively searches project items for files handled by version handlers to resolve versions.
	/// </summary>
	/// <param name="items">The project items collection.</param>
	/// <returns>The resolved version string, or <see langword="null"/> if not found.</returns>
	private static string? FindVersionInProjectItems(ProjectItems items)
	{
		if (IsRunningInVisualStudio)
		{
			VerifyUIThread();
		}

		if (items == null)
		{
			return null;
		}

		foreach (ProjectItem item in items)
		{
			if (item.ProjectItems != null && item.ProjectItems.Count > 0)
			{
				string? ver = FindVersionInProjectItems(item.ProjectItems);

				if (!string.IsNullOrEmpty(ver))
				{
					return ver;
				}
			}

			try
			{
				for (short i = 1; i <= item.FileCount; i++)
				{
					string filePath = item.FileNames[i];

					if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
					{
						IVersionFileHandler? handler = VersionUpCommand.GetHandlerForFile(filePath);

						if (handler != null)
						{
							string content = File.ReadAllText(filePath);

							string? ver = handler.GetVersion(content);

							if (!string.IsNullOrEmpty(ver))
							{
								return ver;
							}
						}
					}
				}
			}
			catch
			{
				// FileNames throws or is empty for some non-file nodes
			}
		}

		return null;
	}

	/// <summary>
	/// Gathers all found version numbers across the project and its items, detecting if they are out of sync.
	/// </summary>
	/// <param name="project">The Visual Studio project.</param>
	/// <returns>A diagnostics object containing version details.</returns>
	public static ProjectVersionDiagnostics GetProjectVersionDiagnostics(Project project)
	{
		if (IsRunningInVisualStudio)
		{
			VerifyUIThread();
		}

		ProjectVersionDiagnostics diagnostics = new();

		if (project == null || string.IsNullOrEmpty(project.FullName))
		{
			return diagnostics;
		}

		string projectPath = project.FullName;

		if (File.Exists(projectPath))
		{
			IVersionFileHandler? handler = VersionUpCommand.GetHandlerForFile(projectPath);

			if (handler != null)
			{
				try
				{
					string content = File.ReadAllText(projectPath);

					string? ver = handler.GetVersion(content);

					if (!string.IsNullOrEmpty(ver))
					{
						diagnostics.Versions.Add(new VersionDetails
						{
							SourceName = Path.GetFileName(projectPath),
							FilePath   = projectPath,
							Version    = ver!
						});

						diagnostics.PrimaryVersion = ver;
					}
				}
				catch
				{
					// Ignore reading/parsing exceptions
				}
			}
		}

		try
		{
			ScanProjectItemsForVersions(project.ProjectItems, diagnostics.Versions);
		}
		catch
		{
			// Ignore
		}

		if (diagnostics.Versions.Count > 1)
		{
			string firstVer = diagnostics.Versions[0].Version;

			for (int i = 1; i < diagnostics.Versions.Count; i++)
			{
				if (diagnostics.Versions[i].Version != firstVer)
				{
					diagnostics.IsOutOfSync = true;

					break;
				}
			}
		}

		if (string.IsNullOrEmpty(diagnostics.PrimaryVersion) && diagnostics.Versions.Count > 0)
		{
			diagnostics.PrimaryVersion = diagnostics.Versions[0].Version;
		}

		return diagnostics;
	}

	/// <summary>
	/// Recursively scans project items for version files.
	/// </summary>
	/// <param name="items">The project items collection.</param>
	/// <param name="list">The list of version details to populate.</param>
	private static void ScanProjectItemsForVersions(ProjectItems items, List<VersionDetails> list)
	{
		if (IsRunningInVisualStudio)
		{
			VerifyUIThread();
		}

		if (items == null)
		{
			return;
		}

		foreach (ProjectItem item in items)
		{
			if (item.ProjectItems != null && item.ProjectItems.Count > 0)
			{
				ScanProjectItemsForVersions(item.ProjectItems, list);
			}

			try
			{
				for (short i = 1; i <= item.FileCount; i++)
				{
					string filePath = item.FileNames[i];

					if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
					{
						IVersionFileHandler? handler = VersionUpCommand.GetHandlerForFile(filePath);

						if (handler != null)
						{
							try
							{
								string content = File.ReadAllText(filePath);

								string? ver = handler.GetVersion(content);

								if (!string.IsNullOrEmpty(ver))
								{
									list.Add(new VersionDetails
									{
										SourceName = Path.GetFileName(filePath),
										FilePath   = filePath,
										Version    = ver!
									});
								}
							}
							catch
							{
								// Ignore disk read errors
							}
						}
					}
				}
			}
			catch
			{
				// Ignore FileNames exception for virtual nodes
			}
		}
	}
}

/// <summary>
/// Holds details about a single found version in a project file.
/// </summary>
public class VersionDetails
{
	/// <summary>Gets or sets the display name of the version source file.</summary>
	public string SourceName { get; set; } = string.Empty;

	/// <summary>Gets or sets the full file path containing the version.</summary>
	public string FilePath { get; set; } = string.Empty;

	/// <summary>Gets or sets the parsed version string.</summary>
	public string Version { get; set; } = string.Empty;
}

/// <summary>
/// Aggregates all version details for a project to assist with diagnostic checks.
/// </summary>
public class ProjectVersionDiagnostics
{
	/// <summary>Gets or sets the resolved primary/fallback version string.</summary>
	public string? PrimaryVersion { get; set; }

	/// <summary>Gets the collection of individual version details found.</summary>
	public List<VersionDetails> Versions { get; } = new();

	/// <summary>Gets or sets a value indicating whether versions within the project are out of sync.</summary>
	public bool IsOutOfSync { get; set; }
}
