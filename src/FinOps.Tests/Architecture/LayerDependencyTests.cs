using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;
using FinOps.Application.Cloud;
using FinOps.Domain.Costs;
using FinOps.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

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
        Assembly[] assemblies =
        [
            typeof(CloudCostDaily).Assembly,
            typeof(CloudCostSyncService).Assembly,
            typeof(DependencyInjection).Assembly,
            Assembly.Load(new AssemblyName("FinOps.Api")),
            Assembly.Load(new AssemblyName("FinOps.Worker"))
        ];

        var violations = assemblies
            .SelectMany(FindDatabaseSchemaApiReferences)
            .OrderBy(violation => violation)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void Database_schema_api_scanner_detects_method_group_aliases()
    {
        Func<DatabaseFacade, Func<CancellationToken, Task>> fixture =
            CreateMigrationDelegate;
        GC.KeepAlive(fixture);

        var violations = FindDatabaseSchemaApiReferences(
            typeof(LayerDependencyTests).Assembly);

        Assert.Contains(
            violations,
            violation => violation.EndsWith(
                "Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.MigrateAsync",
                StringComparison.Ordinal));
    }

    [Fact]
    public void Database_schema_api_scanner_detects_reflection_method_names()
    {
        GC.KeepAlive(FindMigrationMethodByReflection());

        var violations = FindDatabaseSchemaApiReferences(
            typeof(LayerDependencyTests).Assembly);

        Assert.Contains(
            violations,
            violation => violation.EndsWith(
                "reflection string MigrateAsync",
                StringComparison.Ordinal));
    }

    private static Func<CancellationToken, Task> CreateMigrationDelegate(
        DatabaseFacade database) =>
        database.MigrateAsync;

    private static MethodInfo? FindMigrationMethodByReflection() =>
        typeof(RelationalDatabaseFacadeExtensions)
            .GetMethods()
            .FirstOrDefault(method => method.Name == "MigrateAsync");

    private static string[] FindDatabaseSchemaApiReferences(Assembly assembly)
    {
        var assemblyPath = assembly.Location;
        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var metadata = peReader.GetMetadataReader();
        var violations = new List<string>();

        foreach (var handle in metadata.MemberReferences)
        {
            var member = metadata.GetMemberReference(handle);
            var methodName = metadata.GetString(member.Name);
            if (!IsDatabaseSchemaMethod(methodName))
            {
                continue;
            }

            var declaringType = GetDeclaringTypeName(metadata, member.Parent);
            if (
                declaringType.StartsWith(
                    "Microsoft.EntityFrameworkCore.",
                    StringComparison.Ordinal) ||
                declaringType.StartsWith(
                    "Microsoft.EntityFrameworkCore.Relational",
                    StringComparison.Ordinal)
            )
            {
                violations.Add(
                    $"{assembly.GetName().Name}: {declaringType}.{methodName}");
            }
        }

        foreach (var handle in metadata.MethodDefinitions)
        {
            var method = metadata.GetMethodDefinition(handle);
            if (method.RelativeVirtualAddress == 0)
            {
                continue;
            }

            var body = peReader.GetMethodBody(method.RelativeVirtualAddress);
            var il = body.GetILBytes();
            if (il is null)
            {
                continue;
            }

            for (var index = 0; index <= il.Length - sizeof(int) - 1; index++)
            {
                const byte loadStringOpcode = 0x72;
                if (il[index] != loadStringOpcode)
                {
                    continue;
                }

                var token = BitConverter.ToInt32(il, index + 1);
                if ((token & unchecked((int)0xFF000000)) != 0x70000000)
                {
                    continue;
                }

                var value = metadata.GetUserString(
                    MetadataTokens.UserStringHandle(token));
                if (IsDatabaseSchemaMethod(value))
                {
                    violations.Add(
                        $"{assembly.GetName().Name}: reflection string {value}");
                }
            }
        }

        return [.. violations.Distinct(StringComparer.Ordinal)];
    }

    private static bool IsDatabaseSchemaMethod(string methodName) =>
        methodName is "Migrate" or "MigrateAsync" or
            "EnsureCreated" or "EnsureCreatedAsync";

    private static string GetDeclaringTypeName(
        MetadataReader metadata,
        EntityHandle handle)
    {
        if (handle.Kind == HandleKind.TypeReference)
        {
            var type = metadata.GetTypeReference((TypeReferenceHandle)handle);
            var typeNamespace = metadata.GetString(type.Namespace);
            var typeName = metadata.GetString(type.Name);
            return string.IsNullOrWhiteSpace(typeNamespace)
                ? typeName
                : $"{typeNamespace}.{typeName}";
        }

        if (handle.Kind == HandleKind.TypeDefinition)
        {
            var type = metadata.GetTypeDefinition((TypeDefinitionHandle)handle);
            var typeNamespace = metadata.GetString(type.Namespace);
            var typeName = metadata.GetString(type.Name);
            return string.IsNullOrWhiteSpace(typeNamespace)
                ? typeName
                : $"{typeNamespace}.{typeName}";
        }

        return handle.Kind.ToString();
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
