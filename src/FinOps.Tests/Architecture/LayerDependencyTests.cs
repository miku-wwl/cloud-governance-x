using System.Reflection;
using System.Xml.Linq;
using FinOps.Application.Cloud;
using FinOps.Domain.Costs;
using FinOps.Infrastructure;

namespace FinOps.Tests.Architecture;

public sealed class LayerDependencyTests
{
    private static readonly string[] DomainForbiddenReferences =
    [
        "FinOps.Application",
        "FinOps.Infrastructure",
        "FinOps.Api",
        "FinOps.Worker",
        "FinOps.Migrator",
        "Azure.",
        "Azure",
        "Microsoft.AspNetCore",
        "Microsoft.EntityFrameworkCore",
        "Microsoft.Extensions.Hosting",
        "Npgsql"
    ];

    private static readonly string[] ApplicationForbiddenReferences =
    [
        "FinOps.Infrastructure",
        "FinOps.Api",
        "FinOps.Worker",
        "FinOps.Migrator",
        "Azure.",
        "Azure",
        "Microsoft.AspNetCore",
        "Microsoft.EntityFrameworkCore",
        "Microsoft.Extensions.Hosting",
        "Npgsql"
    ];

    public static TheoryData<Assembly, string[]> CoreAssemblyRules => new()
    {
        { typeof(CloudCostDaily).Assembly, DomainForbiddenReferences },
        { typeof(CloudCostSyncService).Assembly, ApplicationForbiddenReferences }
    };

    [Theory]
    [MemberData(nameof(CoreAssemblyRules))]
    public void Core_layers_do_not_reference_forbidden_assemblies(
        Assembly assembly,
        string[] forbiddenPrefixes)
    {
        var referencedAssemblyNames = assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .OrderBy(name => name)
            .ToArray();

        var forbiddenReferences = referencedAssemblyNames
            .Where(reference => forbiddenPrefixes.Any(prefix =>
                reference.Equals(prefix, StringComparison.Ordinal) ||
                reference.StartsWith(prefix, StringComparison.Ordinal)))
            .ToArray();

        Assert.Empty(forbiddenReferences);
    }

    [Fact]
    public void Project_references_follow_clean_architecture_direction()
    {
        var projectReferences = LoadProjectReferences();

        Assert.Empty(projectReferences["FinOps.Domain"]);
        Assert.Equal(["FinOps.Domain"], projectReferences["FinOps.Application"]);
        Assert.Equal(
            ["FinOps.Application", "FinOps.Domain"],
            projectReferences["FinOps.Infrastructure"]);
        Assert.Equal(
            ["FinOps.Infrastructure"],
            projectReferences["FinOps.Migrator"]);
        Assert.Equal(
            ["FinOps.Application", "FinOps.Infrastructure"],
            projectReferences["FinOps.Api"]);
        Assert.Equal(
            ["FinOps.Application", "FinOps.Infrastructure"],
            projectReferences["FinOps.Worker"]);
    }

    [Fact]
    public void Cloud_and_database_implementation_packages_stay_in_infrastructure()
    {
        var packagesByProject = LoadPackageReferences();
        var violations = new List<string>();

        foreach (var (projectName, packageNames) in packagesByProject)
        {
            if (projectName == "FinOps.Infrastructure")
            {
                continue;
            }

            var forbiddenPackages = packageNames
                .Where(IsInfrastructureOnlyPackage)
                .ToArray();

            violations.AddRange(forbiddenPackages.Select(packageName =>
                $"{projectName} references infrastructure-only package {packageName}."));
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void Host_projects_reference_application_and_infrastructure()
    {
        var apiReferences = Assembly
            .Load(new AssemblyName("FinOps.Api"))
            .GetReferencedAssemblies();
        var workerReferences = Assembly
            .Load(new AssemblyName("FinOps.Worker"))
            .GetReferencedAssemblies();

        Assert.Contains(apiReferences, reference => reference.Name == "FinOps.Application");
        Assert.Contains(apiReferences, reference => reference.Name == "FinOps.Infrastructure");
        Assert.Contains(workerReferences, reference => reference.Name == "FinOps.Application");
        Assert.Contains(workerReferences, reference => reference.Name == "FinOps.Infrastructure");
    }

    [Fact]
    public void Only_the_migrator_may_invoke_database_schema_apis()
    {
        var repositoryRoot = FindRepositoryRoot();
        var srcDirectory = Path.Combine(repositoryRoot, "src");
        var migratorDirectory = Path.Combine(srcDirectory, "FinOps.Migrator");
        var testsDirectory = Path.Combine(srcDirectory, "FinOps.Tests");
        const string forbiddenSchemaCallPattern =
            @"(?<![A-Za-z0-9_])(?:Migrate|MigrateAsync|EnsureCreated|EnsureCreatedAsync)\s*\(";

        var violations = Directory
            .EnumerateFiles(srcDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsWithinDirectory(path, migratorDirectory))
            .Where(path => !IsWithinDirectory(path, testsDirectory))
            .Where(path => System.Text.RegularExpressions.Regex.IsMatch(
                File.ReadAllText(path),
                forbiddenSchemaCallPattern))
            .Select(path => Path.GetRelativePath(repositoryRoot, path))
            .OrderBy(path => path)
            .ToArray();

        Assert.Empty(violations);
    }

    private static bool IsWithinDirectory(string path, string directory)
    {
        var relativePath = Path.GetRelativePath(directory, path);
        return relativePath != ".." &&
            !relativePath.StartsWith(
                $"..{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal);
    }

    private static bool IsInfrastructureOnlyPackage(string packageName) =>
        packageName.StartsWith("Azure.", StringComparison.Ordinal) ||
        packageName.Equals("Npgsql", StringComparison.Ordinal) ||
        packageName.StartsWith("Npgsql.", StringComparison.Ordinal) ||
        packageName.Equals("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) ||
        packageName.StartsWith("Microsoft.EntityFrameworkCore.", StringComparison.Ordinal);

    private static IReadOnlyDictionary<string, string[]> LoadProjectReferences()
    {
        return LoadProjectFiles()
            .ToDictionary(
                project => project.ProjectName,
                project => project.Document
                    .Descendants("ProjectReference")
                    .Select(reference => reference.Attribute("Include")?.Value)
                    .OfType<string>()
                    .Where(include => !string.IsNullOrWhiteSpace(include))
                    .Select(include => Path.GetFileNameWithoutExtension(
                        include
                            .Replace('\\', Path.DirectorySeparatorChar)
                            .Replace('/', Path.DirectorySeparatorChar)))
                    .OfType<string>()
                    .OrderBy(projectName => projectName)
                    .ToArray());
    }

    private static IReadOnlyDictionary<string, string[]> LoadPackageReferences()
    {
        return LoadProjectFiles()
            .ToDictionary(
                project => project.ProjectName,
                project => project.Document
                    .Descendants("PackageReference")
                    .Select(reference => reference.Attribute("Include")?.Value)
                    .OfType<string>()
                    .Where(packageName => !string.IsNullOrWhiteSpace(packageName))
                    .OrderBy(packageName => packageName)
                    .ToArray());
    }

    private static IEnumerable<(string ProjectName, XDocument Document)> LoadProjectFiles()
    {
        var srcDirectory = Path.Combine(FindRepositoryRoot(), "src");

        foreach (var projectPath in Directory.EnumerateFiles(
            srcDirectory,
            "*.csproj",
            SearchOption.AllDirectories))
        {
            var projectName = Path.GetFileNameWithoutExtension(projectPath);
            if (projectName == "FinOps.Tests")
            {
                continue;
            }

            yield return (projectName, XDocument.Load(projectPath));
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FinOpsPlatform.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be located.");
    }
}
