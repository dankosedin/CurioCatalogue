namespace CurioCatalogue.CompatTests;

/// <summary>The game server with our mod and the pinned other mods (<see cref="PinnedMods"/>), one for all tests of this project.</summary>
public sealed class CompatServerFixture() : GameServerFixture(PinnedMods.Fetch());

[CollectionDefinition(Name)]
public class CompatCollection : ICollectionFixture<CompatServerFixture>
{
    public const string Name = "game server with other mods";
}
