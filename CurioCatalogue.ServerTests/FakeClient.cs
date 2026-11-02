using System.Collections.Concurrent;
using System.Net;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.Server;

namespace CurioCatalogue.ServerTests;

/// <summary>
/// A player without a game client: a fake connection that sends the server the packets a client sends when it joins, so
/// the server makes a real <see cref="IServerPlayer"/> through its own join path (events, systems, the entity spawned).
/// Requests to our channel go the same way. What the server sends back is kept in <see cref="Received"/>.
/// Engine 1.22.7, decompiled VintagestoryLib (docs/modding.md, "A player without a client").
/// </summary>
sealed class FakeClient : NetConnection
{
    // Packet_Client ids, as ServerMain.PacketHandlers registers them; Packet_Server 56 is the list of network channels.
    const int Identification = 1, RequestJoin = 11, ClientLoaded = 26, PlayerReady = 29, CustomPacket = 23, PositionsOverTcp = 34;
    const int ChannelsPacket = 56;
    // ServerMain.HandlePlayerIdentification refuses any other network version ("disconnect-wrongversion").
    const string NetworkVersion = "1.22.6";

    static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(30);

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
    /// Joins the server as <paramref name="name"/> and returns when the player is playing. The server must not verify
    /// players with the auth server (the fixture sets <c>VerifyPlayerAuth = false</c>); the whitelist is skipped for a
    /// local address (127.0.0.1).
    /// </summary>
    public static FakeClient Join(GameServerFixture game, string name)
    {
        var fake = new FakeClient(game, FakeSocket.Of(game), name + "-uid");
        fake.socket.Connect(fake);
        // Without it, FinalizePlayerIdentification starts a task that checks 10 s later whether UDP came, and that task throws
        // a NullReferenceException on a thread-pool thread, which kills the test process (seen 02.11.2026). A real client
        // sends it when its UDP does not get through.
        fake.Send(PositionsOverTcp, _ => { });
        // NetConnection.client: the server's ConnectedClient for this connection, set when it takes the connect message.
        fake.Send(Identification, p => p.Identification = new Packet_ClientIdentification
        {
            Playername = name, PlayerUID = fake.PlayerUid, NetworkVersion = NetworkVersion, ShortGameVersion = "",
            MdProtocolVersion = "", MpToken = "", ServerPassword = "", ViewDistance = 128,
        });
        fake.WaitFor(() => fake.client?.Player != null, "the server to identify the player");
        fake.Send(RequestJoin, p => p.RequestJoin = new Packet_ClientRequestJoin { Language = "en" });
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
        // The message id is the type's place in the registration (NetworkChannelBase.RegisterMessageType counts from 0),
        // so ModNetwork.Register's order is the source.
        int messageId = Array.IndexOf(OurMessageTypes, typeof(T));
        if (messageId < 0) throw new ArgumentException($"{typeof(T).Name} is not one of our message types");
        using var data = new MemoryStream();
        ProtoBuf.Serializer.Serialize(data, message);
        Send(CustomPacket, p => p.CustomPacket = new Packet_CustomPacket { ChannelId = OurChannelId(), MessageId = messageId, Data = data.ToArray() });
    }

    // ModNetwork.Register (internal), in its order; the channel is named after the mod id.
    static readonly Type[] OurMessageTypes =
    [
        typeof(MakeInvoiceRequest), typeof(RenameLedgerRequest), typeof(ClearLedgerRequest), typeof(CancelInvoiceRequest),
        typeof(MakeWantListRequest), typeof(MissingItemsRefusal),
    ];

    int OurChannelId()
    {
        // The server tells a joining client the ids of its channels (NetworkAPI.SendChannelsPacket).
        var channels = Received.First(p => p.Id == ChannelsPacket).NetworkChannels;
        int index = Array.IndexOf(channels.GetChannelNames(), CurioCatalogueModSystem.ModId);
        if (index < 0) throw new InvalidOperationException($"the server has no channel {CurioCatalogueModSystem.ModId}");
        return channels.GetChannelIds()[index];
    }

    /// <summary>Waits until <paramref name="done"/> (run on the server's main thread) is true.</summary>
    public void WaitFor(Func<bool> done, string what)
    {
        var until = DateTime.UtcNow + WaitTimeout;
        while (!game.OnMain(done))
        {
            if (DateTime.UtcNow > until) throw new TimeoutException($"waited {WaitTimeout} for {what}");
            Thread.Sleep(20);
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
