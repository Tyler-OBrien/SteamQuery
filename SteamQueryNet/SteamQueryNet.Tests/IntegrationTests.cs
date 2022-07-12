using System.Threading.Tasks;
using xRetry;
using Xunit;

namespace SteamQueryNet.Tests;

public class IntegrationTests
{
    /*
* We keep this test here just to test a trusted server
* So, this is more like an integration test than an unit test.
* These tests are flaky, but represent real servers and the whole flow
*/

    [RetryTheory(3, 200)]

    // Randomly taken from popular servers of different games
    [InlineData("135.125.189.170:27015")]
    [InlineData("216.52.148.47:27015")]
    [InlineData("66.55.142.18:27066")]
    [InlineData("109.205.180.203:33915")]
    [InlineData("66.23.205.195:27086")]
    public async Task GetServerInfo(string trustedServer)
    {
        using (var sq = new ServerQuery())
        {
            sq.ReceiveTimeout = 10000;
            sq.SendTimeout = 10000;
            sq.Connect(trustedServer);


            // Make sure that the server is still alive.
            Assert.True(sq.IsConnected);
            var getRules = await sq.GetRulesAsync();
            var getPlayers = await sq.GetPlayersAsync();
            var getServerInfo = await sq.GetServerInfoAsync();
            Assert.NotNull(getServerInfo);
            Assert.NotNull(getPlayers);
            Assert.NotNull(getRules);
            // Hacky, but there was issues with player deseralization where the GetPlayersAsync method would return a ton of fake players
            if (getServerInfo.MaxPlayers * 1.25 < getPlayers.Count)
                Assert.False(getServerInfo.MaxPlayers < getPlayers.Count,
                    "There should not be more players then there is space for");

            Assert.True(!string.IsNullOrWhiteSpace(getServerInfo.Name));
            Assert.NotNull(getServerInfo.ToString());
        }
    }
}