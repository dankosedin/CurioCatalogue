namespace CurioCatalogue.ServerTests;

/// <summary>Every test of this project shares one server: the game's mods and ours (<see cref="GameServerFixture"/>).</summary>
[CollectionDefinition(Name)]
public class ServerCollection : ICollectionFixture<GameServerFixture>
{
    public const string Name = "game server";
}
