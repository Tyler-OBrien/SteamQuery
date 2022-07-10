namespace SteamQueryNet.Models
{
	internal sealed class RequestHeaders
	{
		public const byte A2S_INFO = 0x54;

		public const byte A2S_PLAYER = 0x55;

		public const byte A2S_RULES = 0x56;

        public const byte S2C_CHALLENGE = 0x41; // A

        public const byte S2C_Response = 0x49; // I
}
}
