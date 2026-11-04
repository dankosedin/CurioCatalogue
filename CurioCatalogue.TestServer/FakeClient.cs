using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.Server;

namespace CurioCatalogue.TestServer;

/// <summary>
/// A player without a game client: a fake connection that sends the server the packets a client sends when it joins, so
/// the server makes a real <see cref="IServerPlayer"/> through its own join path (events, systems, the entity spawned).
/// Requests to our channel go the same way. What the server sends back is kept in <see cref="Received"/>.
/// Engine 1.22.7, decompiled VintagestoryLib (docs/modding.md, "A player without a client").
/// </summary>
sealed class FakeClient : NetConnection
{
    // The packet ids are the engine's own constants (Packet_ClientIdEnum, Packet_ServerIdEnum), so a new game version brings its own.
    const int Identification = Packet_ClientIdEnum.PlayerIdentification, RequestJoin = Packet_ClientIdEnum.RequestJoin,
        ClientLoaded = Packet_ClientIdEnum.ClientLoaded, PlayerReady = Packet_ClientIdEnum.ClientPlaying,
        CustomPacket = Packet_ClientIdEnum.CustomPacket, PositionsOverTcp = Packet_ClientIdEnum.RequestPositionTCP;
    const int ChannelsPacket = Packet_ServerIdEnum.NetworkChannels;

    // ServerMain.HandlePlayerIdentification disconnects a player whose name does not match this (and one with another
    // network version: Join sends GameVersion.NetworkVersion, the constant of the API the tests are built against).
    static readonly Regex PlayerName = new(@"^(\w|-){1,16}$");

    // A server answer that has not come in this time will not come: a request is handled at the next tick or two (50 ms
    // each); the margin is for a slow CI runner while a new player's spawn chunks are sent.
    static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(30);
    // How often a wait asks the main thread: short against the 50 ms tick, long enough not to flood its task queue.
    static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(20);

    // Every test joins a player of its own, so that no test starts with another one's inventory (one server per process).
    static readonly HashSet<string> Joined = [];

    readonly GameServerFixture game;
    readonly FakeSocket socket;
    readonly ConcurrentQueue<Packet_Server> received = new();

    public string PlayerUid { get; }

    FakeClient(GameServerFixture game, FakeSocket socket, string playerUid)
    {
        this.game = game;
        this.socket = socket;
        PlayerUid = playerUid;
    }

    /// <summary>Everything the server sent this client so far, in order.</summary>
    public IReadOnlyList<Packet_Server> Received => [.. received];

    /// <summary>The player, read on the server's main thread.</summary>
    public IServerPlayer Player => (IServerPlayer)game.Api.World.PlayerByUid(PlayerUid);

    /// <summary>
    /// Joins the server as <paramref name="name"/> with the client language <paramref name="language"/> (it becomes the player's
    /// <c>LanguageCode</c>) and returns when the player is playing. The server must not verify
    /// players with the auth server (the fixture sets <c>VerifyPlayerAuth = false</c>); the whitelist is skipped for a
    /// local address (127.0.0.1).
    /// </summary>
    public static FakeClient Join(GameServerFixture game, string name, string language = "en")
    {
        if (!PlayerName.IsMatch(name)) throw new ArgumentException($"the server refuses the player name '{name}': it must match {PlayerName}");
        lock (Joined)
            if (!Joined.Add(name)) throw new ArgumentException($"a player '{name}' has already joined: every test needs a player of its own");
        var fake = new FakeClient(game, FakeSocket.Of(game), name + "-uid");
        fake.socket.Connect(fake);
        // Without it, FinalizePlayerIdentification starts a task that checks 10 s later whether UDP came, and that task throws
        // a NullReferenceException on a thread-pool thread, which kills the test process (seen 02.11.2026). A real client
        // sends it when its UDP does not get through.
        fake.Send(PositionsOverTcp, _ => { });
        // NetConnection.client: the server's ConnectedClient for this connection, set when it takes the connect message.
        fake.Send(Identification, p => p.Identification = new Packet_ClientIdentification
        {
            Playername = name, PlayerUID = fake.PlayerUid, NetworkVersion = GameVersion.NetworkVersion, ShortGameVersion = "",
            MdProtocolVersion = "", MpToken = "", ServerPassword = "", ViewDistance = 128,
        });
        fake.WaitFor(() => fake.client?.Player != null, "the server to identify the player");
        fake.Send(RequestJoin, p => p.RequestJoin = new Packet_ClientRequestJoin { Language = language });
        fake.WaitFor(() => fake.client!.ServerAssetsSent, "the server to answer the join request");
        fake.Send(ClientLoaded, p => p.ClientPlaying = new Packet_ClientPlaying());
        fake.WaitFor(() => fake.client!.State == EnumClientState.Connected, "the server to take the client as loaded");
        fake.Send(PlayerReady, _ => { });
        fake.WaitFor(() => fake.client!.State == EnumClientState.Playing, "the player to be playing");
        return fake;
    }

    /// <summary>
    /// Sends <paramref name="message"/> to our channel, as the client side of <c>ModNetwork</c> does. The server handles it
    /// on its main thread at a later tick: wait for its effect with <see cref="WaitFor"/>.
    /// </summary>
    public void SendToMod<T>(T message)
    {
        int messageId = MessageId<T>();
        using var data = new MemoryStream();
        ProtoBuf.Serializer.Serialize(data, message);
        Send(CustomPacket, p => p.CustomPacket = new Packet_CustomPacket { ChannelId = OurChannelId(), MessageId = messageId, Data = data.ToArray() });
    }

    /// <summary>
    /// The messages of type <typeparamref name="T"/> our channel sent this client, in order. Other channels send custom packets
    /// too (the vanilla mods, at join), so a test that waits for one of ours must name the channel and the type.
    /// </summary>
    public List<T> FromMod<T>()
    {
        int messageId = MessageId<T>();
        int channelId = OurChannelId();
        return Received.Where(p => p.CustomPacket is { } c && c.ChannelId == channelId && c.MessageId == messageId)
            .Select(p => ProtoBuf.Serializer.Deserialize<T>(new MemoryStream(p.CustomPacket.Data))).ToList();
    }

    /// <summary>
    /// The id of a message type on our channel, as the server's channel registered it (ModNetwork.Register; the channel is named
    /// after the mod id). Read from the channel itself (decompiled VintagestoryLib 1.22.7: <c>NetworkChannelBase.messageTypes</c>,
    /// internal), so a change of the registration needs no change here.
    /// </summary>
    int MessageId<T>()
    {
        var types = game.OnMain(() =>
        {
            var channel = (NetworkChannelBase)game.Api.Network.GetChannel(GameServerFixture.ModId);
            var field = typeof(NetworkChannelBase).GetField("messageTypes", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("NetworkChannelBase.messageTypes is gone: see how the engine numbers a channel's messages now");
            return new Dictionary<Type, int>((Dictionary<Type, int>)field.GetValue(channel)!);
        });
        return types.TryGetValue(typeof(T), out int id) ? id : throw new ArgumentException($"{typeof(T).Name} is not one of our message types");
    }

    int OurChannelId()
    {
        // The server tells a joining client the ids of its channels (NetworkAPI.SendChannelsPacket).
        var channels = Received.First(p => p.Id == ChannelsPacket).NetworkChannels;
        int index = Array.IndexOf(channels.GetChannelNames(), GameServerFixture.ModId);
        if (index < 0) throw new InvalidOperationException($"the server has no channel {GameServerFixture.ModId}");
        return channels.GetChannelIds()[index];
    }

    /// <summary>Waits until <paramref name="done"/> (run on the server's main thread) is true.</summary>
    public void WaitFor(Func<bool> done, string what)
    {
        var until = DateTime.UtcNow + WaitTimeout;
        while (!game.OnMain(done))
        {
            // A disconnected player gets no answer at all: say why at once instead of waiting it out.
            if (Received.FirstOrDefault(p => p.Id == Packet_ServerIdEnum.DisconnectPlayer) is { } kicked)
                throw new InvalidOperationException($"the server disconnected {PlayerUid} while waiting for {what}: {kicked.DisconnectPlayer?.DisconnectReason}");
            if (DateTime.UtcNow > until) throw new TimeoutException($"waited {WaitTimeout} for {what}; the end of the server log:{Environment.NewLine}" + string.Join(Environment.NewLine, game.LogEntries().TakeLast(12)));
            Thread.Sleep(PollInterval);
        }
    }

    void Send(int id, Action<Packet_Client> fill)
    {
        var packet = new Packet_Client { Id = id };
        fill(packet);
        socket.Receive(this, Packet_ClientSerializer.SerializeToBytes(packet));
    }

    void Keep(byte[] data, int length) => received.Enqueue(Packet_ServerSerializer.DeserializeBuffer(data, length, new Packet_Server()));

    // NetConnection: what the server sends this client. ServerMain.SendPacket compresses a packet over 5 KB for any socket but
    // the singleplayer one (DummyNetConnection), so a large packet comes compressed.
    public override IPEndPoint RemoteEndPoint() => new(IPAddress.Loopback, 0);

    public override EnumSendResult Send(byte[] data, bool compressed = false)
    {
        if (compressed) data = Compression.Decompress(data);
        Keep(data, data.Length);
        return EnumSendResult.Ok;
    }

    public override EnumSendResult HiPerformanceSend(BoxedPacket box, ILogger logger, bool compressionAllowed) =>
        SendPreparedPacket(PreparePacketForSending(box, false, out _), false, logger);

    public override byte[] PreparePacketForSending(BoxedPacket box, bool compressionAllowed, out bool compressed)
    {
        compressed = false;
        box.LengthSent = box.Length;
        return box.buffer[..box.Length];
    }

    public override EnumSendResult SendPreparedPacket(byte[] packet, bool compressed, ILogger logger) => Send(packet, compressed);
    public override bool EqualsConnection(NetConnection connection) => ReferenceEquals(this, connection);
    public override void Shutdown() { }
    public override void Close() { }

    /// <summary>
    /// The server's socket for fake clients: <c>ServerMain.MainSockets[0]</c>, empty on a dedicated server, read by the
    /// server's packet loop like the TCP socket in <c>MainSockets[1]</c>.
    /// </summary>
    sealed class FakeSocket : NetServer
    {
        readonly ConcurrentQueue<NetIncomingMessage> incoming = new();

        public static FakeSocket Of(GameServerFixture game) => game.OnMain(() =>
        {
            var server = (ServerMain)game.Api.World;
            if (server.MainSockets[0] is FakeSocket existing) return existing;
            if (server.MainSockets[0] != null) throw new InvalidOperationException($"MainSockets[0] is taken: {server.MainSockets[0]}");
            var socket = new FakeSocket();
            server.MainSockets[0] = socket;
            return socket;
        });

        public void Connect(FakeClient client) =>
            incoming.Enqueue(new NetIncomingMessage { Type = NetworkMessageType.Connect, SenderConnection = client });

        public void Receive(FakeClient client, byte[] data) =>
            incoming.Enqueue(new NetIncomingMessage { Type = NetworkMessageType.Data, SenderConnection = client, message = data, messageLength = data.Length });

        public override NetIncomingMessage ReadMessage() => incoming.TryDequeue(out var message) ? message : null!;
        public override string LocalEndpoint => "127.0.0.1";
        public override void SetIpAndPort(string ip, int port) { }
        public override void Start() { }
        public override void Dispose() { }
    }
}
