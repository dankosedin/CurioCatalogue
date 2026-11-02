namespace CurioCatalogue.CompatTests;

/// <summary>
/// The game server with our mod, the pinned other mods (<see cref="PinnedMods"/>) and our test mods (<see cref="TestMods"/>), one
/// for all tests of this project.
/// </summary>
public sealed class CompatServerFixture() : GameServerFixture([.. PinnedMods.Fetch(), .. TestMods.All()]);

[CollectionDefinition(Name)]
public class CompatCollection : ICollectionFixture<CompatServerFixture>
{
    public const string Name = "game server with other mods";
}
