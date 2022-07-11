# Fork 


### Merges:

* This fork merges Recieve/Send timeouts (https://github.com/cyilcode/SteamQueryNet/pull/9)

* Also merges a bunch of other minor changes (https://github.com/cyilcode/SteamQueryNet/pull/18), including:

    * Adding support for cancellation tokens

    * Moving to .NET 6


### Other changes made:

* Fixed all of the unit tests to work properly

* Support for A2S_Info Challenges (GetServerInfo) (https://developer.valvesoftware.com/wiki/Server_queries#A2S_SERVERQUERY_GETCHALLENGE)

* Fixed some issues with A2S_Player Challenges (GetPlayers) sometimes returning a corrupted player response, where it would return a list of 200-300 players with random properties if the challenge response failed

* Added integration tests against real servers

* Added overloads, making cancellation tokens unnecessary

* Default timeout is 2000 instead of 0 (and instantly failing)


### Removals:

* Removed Ping Property from ServerInfo (unnecessary and sync)

* Removed The Ship Game Info / Player Details





# SteamQueryNet


SteamQueryNet is a C# wrapper for [Steam Server Queries](https://developer.valvesoftware.com/wiki/Server_queries) UDP protocol. It is;

* Light
* Dependency free


# How to install ?

Check out [SteamQueryNet](https://www.nuget.org/packages/SteamQueryNet/) on nuget.

# How to use ?

SteamQueryNet comes with a single object that gives you access to all API's of the [Steam protocol](https://developer.valvesoftware.com/wiki/Server_queries) which are;

* Server information (server name, capacity etc).
* Concurrent players.
* Server rules (friendlyfire, roundttime etc). **Warning: currently does not work due to a protocol issue on steam server query API. Use could make use of ServerInfo.tags if the server admins are kind enough to put rules as tags in the field.**

## Creating an instance
To make use of the API's listed above, an instance of `ServerQuery` should be created.

```csharp
IServerQuery serverQuery = new ServerQuery();
serverQuery.Connect(host, port);
```

or you can use string resolvers like below:

```csharp
    string myHostAndPort = "127.0.0.1:27015";
    // or
    myHostAndPort = "127.0.0.1,27015";
    // or
    myHostAndPort = "localhost:27015";
    // or
    myHostAndPort = "localhost,27015";
    // or
    myHostAndPort = "steam://connect/127.0.0.1:27015";
    // or
    myHostAndPort = "steam://connect/localhost:27015";

    IServerQuery serverQuery = new ServerQuery(myHostAndPort);
```

## Providing Custom UDPClient

You can provide custom UDP clients by implementing `IUdpClient` in `SteamQueryNet.Interfaces` namespace.

See the example below:
```csharp
public class MyAmazingUdpClient : IUdpClient
    {
        public bool IsConnected { get; }

        public void Close()
        {
            // client implementation
        }

        public void Connect(IPEndPoint remoteIpEndpoint)
        {
            // client implementation
        }

        public void Dispose()
        {
            // client implementation
        }

        public Task<UdpReceiveResult> ReceiveAsync()
        {
            // client implementation
        }

        public Task<int> SendAsync(byte[] datagram, int bytes)
        {
            // client implementation
        }
    }

    // Usage
    IPEndpoint remoteIpEndpoint = new IPEndPoint(IPAddress.Parse(remoteServerIp), remoteServerPort);

    IUdpClient myUdpClient = new MyAmazingUdpClient();
    IServerQuery serverQuery = new ServerQuery(myUdpClient, remoteIpEndpoint);
```

once its created functions below returns informations desired,

[ServerInfo](https://github.com/cyilcode/SteamQueryNet/blob/master/SteamQueryNet/SteamQueryNet/Models/ServerInfo.cs)
```csharp
ServerInfo serverInfo = serverQuery.GetServerInfo();
```

[Players](https://github.com/cyilcode/SteamQueryNet/blob/master/SteamQueryNet/SteamQueryNet/Models/Player.cs)
```csharp
List<Player> players = serverQuery.GetPlayers();
```

[Rules](https://github.com/cyilcode/SteamQueryNet/blob/master/SteamQueryNet/SteamQueryNet/Models/Rule.cs)
```csharp
List<Rule> rules = serverQuery.GetRules();
```

