using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json;
using SteamQueryNet.Interfaces;
using SteamQueryNet.Models;
using SteamQueryNet.Tests.Responses;
using SteamQueryNet.Utils;
using xRetry;
using Xunit;

namespace SteamQueryNet.Tests;

public class ServerQueryTests
{
    private const string IP_ADDRESS = "127.0.0.1";
    private const string HOST_NAME = "localhost";
    private const ushort PORT = 27015;
    private readonly IPEndPoint _localIpEndpoint = new(IPAddress.Parse("127.0.0.1"), 0);
    private byte _packetCount;

    [Theory]
    [InlineData(IP_ADDRESS)]
    [InlineData(HOST_NAME)]
    public void ShouldInitializeWithProperHost(string host)
    {
        using (var sq = new ServerQuery(new Mock<IUdpClient>().Object, It.IsAny<IPEndPoint>()))
        {
            sq.Connect(host, PORT);
        }
    }

    [Theory]
    [InlineData("127.0.0.1:27015")]
    [InlineData("127.0.0.1,27015")]
    [InlineData("localhost:27015")]
    [InlineData("localhost,27015")]
    [InlineData("steam://connect/localhost:27015")]
    [InlineData("steam://connect/127.0.0.1:27015")]
    public void ShouldInitializeWithProperHostAndPort(string ipAndHost)
    {
        using (var sq = new ServerQuery(new Mock<IUdpClient>().Object, It.IsAny<IPEndPoint>()))
        {
            sq.Connect(ipAndHost);
        }
    }

    [Theory]
    [InlineData("invalidHost:-1")]
    [InlineData("invalidHost,-1")]
    [InlineData("invalidHost:65536")]
    [InlineData("invalidHost,65536")]
    [InlineData("256.256.256.256:-1")]
    [InlineData("256.256.256.256,-1")]
    [InlineData("256.256.256.256:65536")]
    [InlineData("256.256.256.256,65536")]
    public void ShouldNotInitializeWithAnInvalidHostAndPort(string invalidHost)
    {
        Assert.Throws<ArgumentException>(() =>
        {
            using (var sq = new ServerQuery(new Mock<IUdpClient>().Object, It.IsAny<IPEndPoint>()))
            {
                sq.Connect(invalidHost);
            }
        });
    }

    [Fact]
    public void GetServerInfo_ShouldPopulateCorrectServerInfo()
    {
        (var responsePacket, var responseObject) = ResponseHelper.GetValidResponse(ResponseHelper.ServerInfo);
        var expectedObject = (ServerInfo)responseObject;

        byte[][] requestPackets = { RequestHelpers.PrepareAS2_INFO_Request() };
        byte[][] responsePackets = { responsePacket };

        var udpClientMock = SetupReceiveResponse(responsePackets);
        SetupRequestCompare(requestPackets, udpClientMock);

        using (var sq = new ServerQuery(udpClientMock.Object, _localIpEndpoint))
        {
            Assert.Equal(JsonConvert.SerializeObject(expectedObject), JsonConvert.SerializeObject(sq.GetServerInfo()));
        }
    }

    [Fact]
    // This had to be modified a bit from the original, because we now pay attention to the packet headers.
    // For future reference:
    // The packets to the server ALWAYS have a request header of 0x55 (U) aka 85 in Decimal
    // The challenge packet from the server has a response header of 0x41 (A) aka 65
    // The A2S_Player packet with all of the player information after confirming your challenge has a response header of 0x44 (D) aka 68 in Decimal
    // 0x55 (85) w/ -1 -> 0x41 (65) w/ Challenge (4 Bytes / Int)
    // 0x55 (85) w/ Challenge (4 Bytes / Int) -> 0x44 (68) w/ Player Info
    public async Task GetPlayers_ShouldPopulateCorrectPlayers()
    {
        (var playersPacket, var responseObject) = ResponseHelper.GetValidResponse(ResponseHelper.GetPlayers);
        var expectedObject = (List<Player>)responseObject;

        var ChallengeSendPacket = RequestHelpers.PrepareAS2_RENEW_CHALLENGE_Request();

        var challengeResponsePacket = ChallengeSendPacket.Take(5).Concat(new byte[]{ 0x22, 0x34, 0x12, 0x9A }).ToArray();

        // Both requests will be executed on A2S_PLayers since thats how you refresh challenges.
        byte[][] requestPackets = { ChallengeSendPacket.ToArray(), challengeResponsePacket.ToArray() };
        requestPackets[0][4] = PacketHeaders.A2S_PLAYER;

        // First response is the Challenge renewal response and the second 
        byte[][] responsePackets = { challengeResponsePacket.ToArray(), playersPacket.ToArray() };
        // Changed from the normal Unit Test, the code expects the right response now for each challenge
        responsePackets[0][4] = PacketHeaders.A2S_PLAYER_S2C_CHALLENGE;

        var udpClientMock = SetupReceiveResponse(responsePackets);
        SetupRequestCompare(requestPackets, udpClientMock);


        using (var sq = new ServerQuery(udpClientMock.Object, _localIpEndpoint))
        {
            Assert.Equal(JsonConvert.SerializeObject(expectedObject),
                JsonConvert.SerializeObject(await sq.GetPlayersAsync()));
        }
    }

    private void SetupRequestCompare(IEnumerable<byte[]> requestPackets, Mock<IUdpClient> udpClientMock)
    {
        udpClientMock
            .Setup(x => x.SendAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Callback<byte[], CancellationToken>((request, _) =>
            {
                Assert.True(TestValidators.CompareBytes(requestPackets.ElementAt(_packetCount), request));
                ++_packetCount;
            });
    }

    private Mock<IUdpClient> SetupReceiveResponse(IEnumerable<byte[]> udpPackets)
    {
        var udpClientMock = new Mock<IUdpClient>();
        var setupSequence = udpClientMock.SetupSequence(x => x.ReceiveAsync(It.IsAny<CancellationToken>()));
        foreach (var packet in udpPackets)
            setupSequence = setupSequence.ReturnsAsync(new UdpReceiveResult(packet, _localIpEndpoint));

        return udpClientMock;
    }
}