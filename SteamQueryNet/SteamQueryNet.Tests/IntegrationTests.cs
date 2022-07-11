using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xRetry;
using Xunit;

namespace SteamQueryNet.Tests
{
    public class IntegrationTests
    {
        /*
 * We keep this test here just to test a trusted server
 * So, this is more like an integration test than an unit test.
 * These tests are flaky, but represent real servers and the whole flow
 */

        [RetryTheory(maxRetries: 3, delayBetweenRetriesMs: 200)]

        // Randomly taken from popular servers of different games
        [InlineData("54.37.111.217:27015")]
        [InlineData("74.91.115.81:27015")]
        [InlineData("135.125.189.170:27015")]
        [InlineData("142.44.169.172:2303")]
        [InlineData("216.52.148.47:27015")]
        [InlineData("66.55.142.18:27066")]
        [InlineData("109.205.180.203:33915")]
        [InlineData("64.74.163.82:30265")]
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
                var getServerInfo = await sq.GetServerInfoAsync();
                var getPlayers = await sq.GetPlayersAsync();
                Assert.NotNull(getServerInfo);
                Assert.NotNull(getPlayers);
                // Hacky, but there was issues with player deseralization where the GetPlayersAsync method would return a ton of fake players
                if (getServerInfo.MaxPlayers * 1.25 < getPlayers.Count)
                {
                    Assert.False(getServerInfo.MaxPlayers < getPlayers.Count, "There should not be more players then there is space for");
                }

                Assert.True(!string.IsNullOrWhiteSpace(getServerInfo.Name));
                Assert.NotNull(getServerInfo.ToString());
            }
        }

    }
}
