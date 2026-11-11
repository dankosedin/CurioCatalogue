using System;
using System.IO;
using System.Linq;
using Cake.Common;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Clean;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Frosting;
using Cake.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;

namespace CakeBuild;

public static class Program
{
    public static int Main(string[] args)
    {
        return new CakeHost()
            .UseContext<BuildContext>()
            .Run(args);
    }
}

public class BuildContext : FrostingContext
{
    public const string ProjectName = "curiocatalogue";
    public string BuildConfiguration { get; }
    public string Version { get; }
    public string Name { get; }
    public bool SkipJsonValidation { get; }

    public BuildContext(ICakeContext context)
        : base(context)
    {
        BuildConfiguration = context.Argument("configuration", "Release");
        SkipJsonValidation = context.Argument("skipJsonValidation", false);
        var modInfo = context.DeserializeJsonFromFile<ModInfo>($"../{ProjectName}/modinfo.json");
        Version = modInfo.Version;
        Name = modInfo.ModID;
    }
}

[TaskName("ValidateJson")]
public sealed class ValidateJsonTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        if (context.SkipJsonValidation)
        {
            return;
        }
        var jsonFiles = context.GetFiles($"../{BuildContext.ProjectName}/assets/**/*.json");
        foreach (var file in jsonFiles)
        {
            try
            {
                var json = File.ReadAllText(file.FullPath);
                JToken.Parse(json);
            }
            catch (JsonException ex)
            {
                throw new Exception($"Validation failed for JSON file: {file.FullPath}{Environment.NewLine}{ex.Message}", ex);
            }
        }
    }
}

[TaskName("Build")]
[IsDependentOn(typeof(ValidateJsonTask))]
public sealed class BuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.DotNetClean($"../{BuildContext.ProjectName}/{BuildContext.ProjectName}.csproj",
            new DotNetCleanSettings
            {
                Configuration = context.BuildConfiguration
            });


        context.DotNetPublish($"../{BuildContext.ProjectName}/{BuildContext.ProjectName}.csproj",
            new DotNetPublishSettings
            {
                Configuration = context.BuildConfiguration
            });
    }
}

[TaskName("Package")]
[IsDependentOn(typeof(BuildTask))]
public sealed class PackageTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        // Only this build's own folder and zip: anything else in Releases (older zips kept to compare) stays.
        var zip = $"../Releases/{context.Name}_{context.Version}.zip";
        context.EnsureDirectoryExists($"../Releases/{context.Name}");
        context.CleanDirectory($"../Releases/{context.Name}");
        if (context.FileExists(zip)) context.DeleteFile(zip);
        context.CopyFiles($"../{BuildContext.ProjectName}/bin/{context.BuildConfiguration}/Mods/mod/publish/*", $"../Releases/{context.Name}");
        if (context.DirectoryExists($"../{BuildContext.ProjectName}/assets"))
        {
            context.CopyDirectory($"../{BuildContext.ProjectName}/assets", $"../Releases/{context.Name}/assets");
        }
        context.CopyFile($"../{BuildContext.ProjectName}/modinfo.json", $"../Releases/{context.Name}/modinfo.json");
        if (context.FileExists($"../{BuildContext.ProjectName}/modicon.png"))
        {
            context.CopyFile($"../{BuildContext.ProjectName}/modicon.png", $"../Releases/{context.Name}/modicon.png");
        }
        context.Zip($"../Releases/{context.Name}", zip);
    }
}

[TaskName("Default")]
[IsDependentOn(typeof(PackageTask))]
public sealed class DefaultTask : FrostingTask
{
}

/// <summary>
/// The ModDB page from docs/moddb/description.md, description.uk.md and template.html (docs/releasing.md, ModDB).
/// Arguments: --ref=&lt;tag or SHA&gt; (default v&lt;version&gt;), --moddbLinks=false (leave Source and Report a bug out of the buttons; default true),
/// --out=&lt;file&gt; (default Releases/moddb.html), --rawBase / --blobBase (to look at the page before the files are on GitHub,
/// e.g. file:///C:/Projects/.../), --descriptionEn / --descriptionUk (other descriptions, read as if they lay in docs/moddb/).
/// </summary>
[TaskName("ModDbPage")]
public sealed class ModDbPageTask : FrostingTask<BuildContext>
{
    const string Repo = "dankosedin/CurioCatalogue";

    public override void Run(BuildContext context)
    {
        var modInfo = JObject.Parse(File.ReadAllText($"../{BuildContext.ProjectName}/modinfo.json"));
        var langs = Directory.GetFiles($"../{BuildContext.ProjectName}/assets/{BuildContext.ProjectName}/lang", "*.json")
            .Select(f => Path.GetFileNameWithoutExtension(f)!).OrderBy(c => c, StringComparer.Ordinal).ToList();
        var gitRef = context.Argument("ref", $"v{context.Version}");
        var input = new CurioCatalogue.PageGen.PageInput
        {
            DescriptionEn = File.ReadAllText(context.Argument("descriptionEn", "../docs/moddb/description.md")),
            DescriptionUk = File.ReadAllText(context.Argument("descriptionUk", "../docs/moddb/description.uk.md")),
            Template = File.ReadAllText("../docs/moddb/template.html"),
            Version = context.Version,
            GameVersion = (string?)modInfo["dependencies"]?["game"] ?? throw new Exception("modinfo.json has no dependencies.game"),
            Side = Capitalize((string?)modInfo["side"] ?? "universal"),
            LangCodes = langs,
            RawBase = context.Argument("rawBase", $"https://raw.githubusercontent.com/{Repo}/{gitRef}/"),
            BlobBase = context.Argument("blobBase", $"https://github.com/{Repo}/blob/{gitRef}/"),
            RepoUrl = $"https://github.com/{Repo}",
            ModDbLinks = context.Argument("moddbLinks", true),
        };

        string html;
        try
        {
            html = CurioCatalogue.PageGen.ModDbPage.Generate(input);
        }
        catch (CurioCatalogue.PageGen.ContractException ex)
        {
            throw new Exception(ex.Message, ex);
        }

        var output = context.Argument("out", "../Releases/moddb.html");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllText(output, html);
        // For looking at it in a browser (a bare fragment has no charset); the file to paste is the one above.
        File.WriteAllText(Path.ChangeExtension(output, ".preview.html"),
            "<!doctype html>\n<html><head><meta charset=\"utf-8\"><title>Curio Catalogue (preview)</title></head>\n" +
            "<body style=\"margin: 0; padding: 24px; background: #888\">\n" + html + "</body></html>\n");
        context.Log.Information("ModDB page for {0} at {1}: {2} ({3} KB)", context.Version, gitRef, Path.GetFullPath(output), html.Length / 1024);
    }

    static string Capitalize(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..].ToLowerInvariant();
}
