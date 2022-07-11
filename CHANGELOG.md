# Change Log for SteamQueryNet 1.1.0 (7-10-2022)


### Merges:

* This fork merges Recieve/Send timeouts (https://github.com/cyilcode/SteamQueryNet/pull/9)

* Also merges a bunch of other minor changes (https://github.com/cyilcode/SteamQueryNet/pull/18), including:

    * Adding support for cancellation tokens

    * Moving to .NET 6


### This fork takes those two PRs, along with some other changes I made:

* Fixed all of the unit tests to work properly

* Support for A2S_Info Challenges (https://developer.valvesoftware.com/wiki/Server_queries#A2S_SERVERQUERY_GETCHALLENGE)

* Added integration tests against real servers

* Added overloads, making cancellation tokens unnecessary

* Default send/recv timeout is 2000 instead of 0 (and instantly failing)


### Removals:

* Removed Ping (unnecessary and sync)

* Removed The Ship Game Info / Player Details


# Changelog for SteamQueryNet v1.0.6

### 1. Enhancements

* ServerInfo model now reduces the `ServerInfo.Bots` from the `ServerInfo.Players` property. So that `ServerInfo.Players` reflects real player count only. This was an issue because some server flag their bots as real players.

* Added a new constructor to `ServerQuery` to allow users to be able to bind their own local IPEndpoint.

* Added a new constructor to `ServerQuery` to allow users to be able to provide hostnames and ports in one single string like
    
    ```
    string myHostAndPort = "127.0.0.1:27015";
    // or
    string myHostAndPort = "localhost:27015";
    // or
    string myHostAndPort = "steam://connect/127.0.0.1:27015";
    // or
    string myHostAndPort = "steam://connect/localhost:27015";
    ```
* Implemented new tests for ip, hostname and port validation.

### 2. Bug fixes

* Fixed a bug where player information was not gathered correctly by the `ServerQuery.GetPlayers()` function.

* Fixed a bug where player count was not gathered by the `ServerQuery.GetServerInfo()` function.

### 3. Soft-deprecations (no warnings emitted)

* Removed `sealed` modifiers from all `SteamQueryNet.Models` namespace.

* `IServerQuery` moved into `SteamQueryNet.Interfaces` namespace.

### 4. Hard-deprecations

* `ServerQuery` constructor parameter `int port` now changed to `ushort` to remove all integer range checks since the UDP port is already literally an `ushort`.

* Removed port range tests.

* `IServerQuery` moved into `SteamQueryNet.Interfaces` namespace.
