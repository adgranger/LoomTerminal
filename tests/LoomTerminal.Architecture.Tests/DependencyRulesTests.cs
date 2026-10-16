using System.Reflection;
using System.Xml.Linq;

namespace LoomTerminal.Architecture.Tests;

/// <summary>
/// Dependency rules of docs/ARCHITECTURE.md: the emulator and the presentation layer know no UI framework, and
/// dependencies go one way only: WinUI (and later Avalonia) → Presentation → Core, plus Pty, which stands alone.
/// </summary>
public class DependencyRulesTests
{
    /// <summary>
    /// For each project, the ONLY projects it may reference directly. If you need to change this table,
    /// you are changing the architecture: update docs/ARCHITECTURE.md and write an ADR.
    /// </summary>
    private static readonly Dictionary<string, string[]> AllowedReferences = new()
    {
        ["LoomTerminal.Core"] = [],
        ["LoomTerminal.Pty"] = [],
        ["LoomTerminal.Presentation"] = ["LoomTerminal.Core"],
        ["LoomTerminal.WinUI"] = ["LoomTerminal.Presentation", "LoomTerminal.Pty"],
    };

    private static readonly string[] UiPackages =
        ["Microsoft.WindowsAppSDK", "Microsoft.Graphics.Win2D", "Microsoft.Windows.CsWinRT", "Avalonia", "SkiaSharp"];

    public static TheoryData<string> SourceProjectNames => new(AllowedReferences.Keys);

    /// <summary>Projects that must load anywhere .NET runs: no UI, no Windows TFM.</summary>
    public static TheoryData<string> UiIndependentProjects => new()
    {
        "LoomTerminal.Core",
        "LoomTerminal.Pty",
        "LoomTerminal.Presentation",
    };

    [Fact]
    public void EverySourceProject_IsListedInTheRules()
    {
        Assert.Equal(AllowedReferences.Keys.Order(), RepositoryLayout.SourceProjects.Keys.Order());
    }

    [Theory]
    [MemberData(nameof(SourceProjectNames))]
    public void Project_ReferencesOnlyAllowedProjects(string projectName)
    {
        XDocument project = RepositoryLayout.SourceProjects[projectName];

        string[] forbidden = RepositoryLayout.GetProjectReferences(project).Except(AllowedReferences[projectName]).ToArray();

        Assert.True(forbidden.Length == 0,
            $"{projectName} must not reference {string.Join(", ", forbidden)}. See docs/ARCHITECTURE.md (dependency rules).");
    }

    [Theory]
    [MemberData(nameof(UiIndependentProjects))]
    public void UiIndependentProject_TargetsPlainDotNet(string projectName)
    {
        XDocument project = RepositoryLayout.SourceProjects[projectName];

        Assert.Equal("$(LoomNetTargetFramework)", RepositoryLayout.GetProperty(project, "TargetFramework"));
        Assert.Null(RepositoryLayout.GetProperty(project, "UseWinUI"));
    }

    [Theory]
    [MemberData(nameof(UiIndependentProjects))]
    public void UiIndependentProject_DoesNotReferenceUiPackages(string projectName)
    {
        XDocument project = RepositoryLayout.SourceProjects[projectName];

        Assert.Empty(RepositoryLayout.GetPackageReferences(project).Intersect(UiPackages));
    }

    [Theory]
    [MemberData(nameof(UiIndependentProjects))]
    public void UiIndependentAssembly_ReferencesOnlyDotNetAndLoomTerminal(string assemblyName)
    {
        Assembly assembly = Assembly.Load(assemblyName);

        string[] unexpected = assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .Where(name => !name.StartsWith("System", StringComparison.Ordinal)
                && !name.StartsWith("Microsoft.Win32", StringComparison.Ordinal)
                && !name.StartsWith("LoomTerminal.", StringComparison.Ordinal)
                && name != "netstandard")
            .ToArray();

        Assert.True(unexpected.Length == 0, $"{assemblyName} references {string.Join(", ", unexpected)}.");
    }
}
