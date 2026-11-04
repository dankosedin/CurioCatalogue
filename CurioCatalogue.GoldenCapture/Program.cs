using CurioCatalogue.GoldenCapture;

// The golden stacks of a release (docs/development.md, Golden stacks; plan docs/plans/2026-11-04-backward-compat.md): the test
// server with our mod of the release zip, the items made the way a player makes them (Scenario), and what they hold written out
// (Golden). Run by hand, once per release:
//   dotnet run --project CurioCatalogue.GoldenCapture                 writes CurioCatalogue.ServerTests/Golden/v<version>/
//   dotnet run --project CurioCatalogue.GoldenCapture -- --out <dir>  writes into another folder (to compare two runs)
// A set already written is never written over: old sets stay as their release made them.

string outDir = args.Length == 2 && args[0] == "--out" ? Path.GetFullPath(args[1])
    : args.Length == 0 ? TestPaths.Get("GoldenFolder")
    : throw new ArgumentException("usage: [--out <folder>]");
if (File.Exists(Path.Combine(outDir, Golden.StacksFile)))
    throw new InvalidOperationException($"{outDir} already holds a golden set; a set is never captured again (pass --out to write elsewhere)");

var release = new OtherMod("curiocatalogue", TestPaths.Get("ReleaseVersion"), TestPaths.Get("ReleaseZip"));
var server = new CaptureServer(release);
string dataPath = server.DataPath;
try
{
    var mod = server.Api.ModLoader.GetMod("curiocatalogue");
    Console.WriteLine($"server up in {server.StartTime.TotalSeconds:0.0} s; our mod {mod.Info.Version} from {mod.SourcePath}");
    if (mod.Info.Version != release.Version) throw new InvalidOperationException($"the server runs our mod {mod.Info.Version}, not the release {release.Version}");

    // The config as the release wrote it on its first start, before anything could change it (Decisions, "The scenario").
    byte[] config = File.ReadAllBytes(Path.Combine(dataPath, "ModConfig", Golden.ConfigFile));

    var scenario = new Scenario(server);
    var stacks = scenario.Run();

    // The fixture world of the manual upgrade pass: the items laid out where a player leaves them; the world is kept (B4).
    string layout = new Fixture(server, scenario.Made).Lay(release.Version);
    File.WriteAllText(Path.Combine(dataPath, "fixture-layout.txt"), layout);
    Console.Write(layout);

    // The set last: a run that fails before this leaves no set behind, so the next run is not refused.
    Golden.Write(outDir, server, stacks, config);
    Console.WriteLine($"{stacks.Count} stacks and the config written to {outDir}");
}
catch (Exception e)
{
    // Printed here: a failing stop in finally would otherwise replace it.
    Console.Error.WriteLine(e);
    throw;
}
finally
{
    server.Dispose();
}
Console.WriteLine($"data folder kept: {dataPath}");

namespace CurioCatalogue.GoldenCapture
{
    sealed class CaptureServer(OtherMod release) : GameServerFixture([], release, keepData: true);
}
