namespace SteamQueryNet.Models;

internal sealed class PacketHeaders
{
    public const byte A2S_INFO = 0x54;

    public const byte A2S_PLAYER = 0x55;

    public const byte A2S_RULES = 0x56;

    public const byte A2S_INFO_S2C_CHALLENGE = 0x41; // A

    public const byte A2S_PLAYER_S2C_CHALLENGE = 0x41; // A


    public const byte A2S_INFO_RESPONSE = 0x49; // I

    public const byte A2S_PLAYER_RESPONSE = 0x44; // D
}