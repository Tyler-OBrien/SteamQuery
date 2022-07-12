using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SteamQueryNet.Interfaces;
using SteamQueryNet.Models;
using SteamQueryNet.Services;
using SteamQueryNet.Utils;

[assembly: InternalsVisibleTo("SteamQueryNet.Tests")]

namespace SteamQueryNet;
#nullable enable
public class ServerQuery : IServerQuery, IDisposable
{

    private ushort m_port;
    private IPEndPoint m_remoteIpEndpoint;

    /// <summary>
    ///     Creates a new instance of ServerQuery with given UDPClient and remote endpoint.
    /// </summary>
    /// <param name="udpClient">UdpClient to communicate.</param>
    /// <param name="remoteEndpoint">Remote server endpoint.</param>
    public ServerQuery(IUdpClient udpClient, IPEndPoint remoteEndpoint)
    {
        UdpClient = udpClient;
        m_remoteIpEndpoint = remoteEndpoint;
    }

    /// <summary>
    ///     Creates a new instance of ServerQuery without UDP socket connection.
    /// </summary>
    public ServerQuery()
    {
    }

    internal virtual IUdpClient UdpClient { get; private set; }

    /// <summary>
    ///     Reflects the udp client connection state.
    /// </summary>
    public bool IsConnected => UdpClient.IsConnected;

    /// <summary>
    ///     Amount of time in milliseconds to terminate send operation if the server won't respond.
    /// </summary>
    public int SendTimeout { get; set; }

    /// <summary>
    ///     Amount of time in milliseconds to terminate receive operation if the server won't respond.
    /// </summary>
    public int ReceiveTimeout { get; set; }

    /// <summary>
    ///     Disposes the object and its disposables.
    /// </summary>
    public void Dispose()
    {
        UdpClient.Close();
        UdpClient.Dispose();
    }

    /// <summary>
    ///     Creates a new ServerQuery instance for Steam Server Query Operations.
    /// </summary>
    /// <param name="serverAddress">IPAddress or HostName of the server that queries will be sent.</param>
    /// <param name="port">Port of the server that queries will be sent.</param>
    public IServerQuery Connect(string serverAddress, ushort port)
    {
        PrepareAndConnect(serverAddress, port);
        return this;
    }

    /// <summary>
    ///     Creates a new ServerQuery instance for Steam Server Query Operations.
    /// </summary>
    /// <param name="serverAddressAndPort">IPAddress or HostName of the server and port separated by a colon(:) or a comma(,).</param>
    public IServerQuery Connect(string serverAddressAndPort)
    {
        var (serverAddress, port) = Helpers.ResolveIPAndPortFromString(serverAddressAndPort);
        PrepareAndConnect(serverAddress, port);
        return this;
    }

    /// <summary>
    ///     Creates a new instance of ServerQuery with the given Local IPEndpoint.
    /// </summary>
    /// <param name="customLocalIPEndpoint">Desired local IPEndpoint to bound.</param>
    /// <param name="serverAddressAndPort">IPAddress or HostName of the server and port separated by a colon(:) or a comma(,).</param>
    public IServerQuery Connect(IPEndPoint customLocalIPEndpoint, string serverAddressAndPort)
    {
        UdpClient = new UdpWrapper(customLocalIPEndpoint, SendTimeout, ReceiveTimeout);
        var (serverAddress, port) = Helpers.ResolveIPAndPortFromString(serverAddressAndPort);
        PrepareAndConnect(serverAddress, port);
        return this;
    }

    /// <summary>
    ///     Creates a new instance of ServerQuery with the given Local IPEndpoint.
    /// </summary>
    /// <param name="customLocalIPEndpoint">Desired local IPEndpoint to bound.</param>
    /// <param name="serverAddress">IPAddress or HostName of the server that queries will be sent.</param>
    /// <param name="port">Port of the server that queries will be sent.</param>
    public IServerQuery Connect(IPEndPoint customLocalIPEndpoint, string serverAddress, ushort port)
    {
        UdpClient = new UdpWrapper(customLocalIPEndpoint, SendTimeout, ReceiveTimeout);
        PrepareAndConnect(serverAddress, port);
        return this;
    }


    /// <inheritdoc />
    public Task<ServerInfo?> GetServerInfoAsync()
    {
        return GetServerInfoAsync(CancellationToken.None);
    }

    /// <inheritdoc />
    /// <inheritdoc />
    public async Task<ServerInfo?> GetServerInfoAsync(CancellationToken token)
    {
        var sInfo = new ServerInfo();


        var response = await SendRequestAsync(RequestHelpers.PrepareAS2_INFO_Request(), token);
        var tryGetHeader = response.Skip(4).First();
        if (tryGetHeader != PacketHeaders.A2S_INFO_S2C_CHALLENGE && tryGetHeader != PacketHeaders.A2S_INFO_RESPONSE)
        {
#if DEBUG
            Console.WriteLine(
                $"[Warning] GetServerInfoAsync returned {tryGetHeader} header after first request for challenge, instead of 0x41 Challenge Response or 0x49 Normal Response, for {m_remoteIpEndpoint.ToString()}");
#endif
            return null;
        }

        if (tryGetHeader == PacketHeaders.A2S_INFO_S2C_CHALLENGE)
        {
            var challenge = response.Skip(DataResolutionUtils.RESPONSE_CODE_INDEX).ToArray();
            // Now we got the challenge! Send it back!
            response = await SendRequestAsync(RequestHelpers.PrepareAS2_INFO_Request(challenge), token);


            tryGetHeader = response.Skip(4).First();
        }


        if (tryGetHeader != PacketHeaders.A2S_INFO_RESPONSE)
        {
#if DEBUG
            Console.WriteLine(
                $"[Warning] GetServerInfoAsync returned {tryGetHeader} header after challenge, instead of 0x49/I valid response, for {m_remoteIpEndpoint.ToString()}.");
#endif
            return null;
        }


        if (response.Length > 0) DataResolutionUtils.ExtractData(sInfo, response, nameof(sInfo.EDF), true);

        return sInfo;
    }

    /// <inheritdoc />
    public ServerInfo? GetServerInfo()
    {
        var task = GetServerInfoAsync(CancellationToken.None);
        if (task.IsCompleted == false)
            task.RunSynchronously();
        return task.Result;
        // return Helpers.RunSync(GetServerInfoAsync);
    }



    /// <inheritdoc />
    public Task<List<Player>?> GetPlayersAsync()
    {
        return GetPlayersAsync(CancellationToken.None);
    }


    /// <inheritdoc />
    public async Task<List<Player>?> GetPlayersAsync(CancellationToken cancellationToken)
    {
        var response = await SendRequestAsync(
            RequestHelpers.PrepareAS2_GENERIC_Request(PacketHeaders.A2S_PLAYER, -1),
            cancellationToken);

        var tryGetHeader = response.Skip(4).First();

        if (tryGetHeader != PacketHeaders.A2S_PLAYER_S2C_CHALLENGE)
        {
#if DEBUG
            Console.WriteLine(
                $"[Warning] GetPlayersAsync returned {tryGetHeader} header after first request for challenge, instead of 0x41 Challenge Response, for {this.m_remoteIpEndpoint.ToString()}");
#endif
            return null;
        }

        var challenge = BitConverter.ToInt32(
            response.Skip(DataResolutionUtils.RESPONSE_CODE_INDEX).Take(sizeof(int)).ToArray(),
            0);

        response = await SendRequestAsync(
            RequestHelpers.PrepareAS2_GENERIC_Request(PacketHeaders.A2S_PLAYER, challenge),
            cancellationToken);


        tryGetHeader = response.Skip(4).First();


        if (tryGetHeader != PacketHeaders.A2S_PLAYER_RESPONSE)
        {
#if DEBUG
            Console.WriteLine(
                $"[Warning] GetPlayersAsync returned {tryGetHeader} header after challenge, instead of 0x44/D valid response, for {this.m_remoteIpEndpoint.ToString()}.");
#endif
            return null;
        }

        if (response.Length > 0)
            return DataResolutionUtils.ExtractPlayersData<Player>(response);
        throw new InvalidOperationException("Server did not response the query");
    }

    /// <inheritdoc />
    public List<Player>? GetPlayers()
    {
        var task = GetPlayersAsync();
        if (task.IsCompleted == false)
            task.RunSynchronously();
        return task.Result;
        // return Helpers.RunSync(GetPlayersAsync);
    }


    /// <inheritdoc />
    public Task<List<Rule>?> GetRulesAsync()
    {
        return GetRulesAsync(CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<List<Rule>?> GetRulesAsync(CancellationToken cancellationToken)
    {

        var response = await SendRequestAsync(
            RequestHelpers.PrepareAS2_GENERIC_Request(PacketHeaders.A2S_RULES, -1),
            cancellationToken);

        var tryGetHeader = response.Skip(4).First();

        if (tryGetHeader != PacketHeaders.A2S_RULES_S2C_CHALLENGE)
        {
#if DEBUG
            Console.WriteLine(
                $"[Warning] GetRulesAsync returned {tryGetHeader} header after first request for challenge, instead of 0x41 Challenge Response, for {this.m_remoteIpEndpoint.ToString()}");
#endif
            return null;
        }

        var challenge = BitConverter.ToInt32(
            response.Skip(DataResolutionUtils.RESPONSE_CODE_INDEX).Take(sizeof(int)).ToArray(),
            0);

        response = await SendRequestAsync(
            RequestHelpers.PrepareAS2_GENERIC_Request(PacketHeaders.A2S_RULES, challenge),
            cancellationToken);


        tryGetHeader = response.Skip(4).First();


        if (tryGetHeader != PacketHeaders.A2S_RULES_RESPONSE)
        {
#if DEBUG
            Console.WriteLine(
                $"[Warning] GetRulesAsync returned {tryGetHeader} header after challenge, instead of 0x45/E valid response, for {this.m_remoteIpEndpoint.ToString()}.");
#endif
            return null;
        }

        if (response.Length > 0)
            return DataResolutionUtils.ExtractRulesData<Rule>(response);
        throw new InvalidOperationException("Server did not response the query");
    }

    /// <inheritdoc />
    public List<Rule>? GetRules()
    {
        var task = GetRulesAsync(CancellationToken.None);
        if (task.IsCompleted == false)
            task.RunSynchronously();
        return task.Result;
        // return Helpers.RunSync(GetRulesAsync);
    }

    private void PrepareAndConnect(string serverAddress, ushort port)
    {
        m_port = port;

        // Try to parse the serverAddress as IP first
        if (IPAddress.TryParse(serverAddress, out var parsedIpAddress))
            // Yep its an IP.
            m_remoteIpEndpoint = new IPEndPoint(parsedIpAddress, m_port);
        else
            // Nope it might be a hostname.
            try
            {
                var addressList = Dns.GetHostAddresses(serverAddress);
                if (addressList.Length > 0)
                    // We get the first address.
                    m_remoteIpEndpoint = new IPEndPoint(addressList[0], m_port);
                else
                    throw new ArgumentException($"Invalid host address {serverAddress}");
            }
            catch (SocketException ex)
            {
                throw new ArgumentException("Could not reach the hostname.", ex);
            }

        UdpClient ??= new UdpWrapper(new IPEndPoint(IPAddress.Any, 0), SendTimeout, ReceiveTimeout);
        UdpClient.Connect(m_remoteIpEndpoint);
    }

    private async Task<byte[]> SendRequestAsync(byte[] request, CancellationToken cancellationToken)
    {
        await UdpClient.SendAsync(request, cancellationToken);
        var result = await UdpClient.ReceiveAsync(cancellationToken);
        return result.Buffer;
    }
}