using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SteamQueryNet.Interfaces;

namespace SteamQueryNet.Services;

internal sealed class UdpWrapper : IUdpClient
{
    private readonly int m_receiveTimeout;
    private readonly int m_sendTimeout;
    private readonly UdpClient m_udpClient;

    public UdpWrapper(IPEndPoint localIpEndPoint, int sendTimeout, int receiveTimeout)
    {
        m_udpClient = new UdpClient(localIpEndPoint);
        m_sendTimeout = sendTimeout;
        m_receiveTimeout = receiveTimeout;
        if (m_receiveTimeout <= 0)
            m_receiveTimeout = 2000;

        if (m_sendTimeout <= 0)
            m_sendTimeout = 2000;
    }

    public bool IsConnected => m_udpClient.Client.Connected;

    public void Close()
    {
        m_udpClient.Close();
    }

    public void Connect(IPEndPoint remoteIpEndpoint)
    {
        m_udpClient.Connect(remoteIpEndpoint);
    }

    public void Dispose()
    {
        m_udpClient.Dispose();
    }

    public Task<UdpReceiveResult> ReceiveAsync()
    {
        return ReceiveAsync(CancellationToken.None);
    }


    public async Task<UdpReceiveResult> ReceiveAsync(CancellationToken cancellationToken)
    {
        var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        source.CancelAfter(m_receiveTimeout);

        try
        {
            return await m_udpClient.ReceiveAsync(source.Token);
        }
        catch (OperationCanceledException)
        {
            if (cancellationToken.IsCancellationRequested)
                throw;
            throw new TimeoutException();
        }
    }

    public Task<int> SendAsync(byte[] datagram)
    {
        return SendAsync(datagram, CancellationToken.None);
    }


    public async Task<int> SendAsync(byte[] datagram, CancellationToken cancellationToken)
    {
        if (cancellationToken == CancellationToken.None)
            cancellationToken = new CancellationToken(false);

        var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        source.CancelAfter(m_receiveTimeout);

        try
        {
            return await m_udpClient.SendAsync(datagram, source.Token);
        }
        catch (OperationCanceledException)
        {
            if (cancellationToken.IsCancellationRequested)
                throw;
            throw new TimeoutException();
        }
    }
}