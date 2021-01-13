namespace RunAmiga
{
	public static class Interrupt
	{
		public const uint NMI = 15;
		public const uint INTEN = 14;
		public const uint EXTER = 13;
		public const uint DSKSYNC = 12;
		public const uint RBF = 11;
		public const uint AUD1 = 10;
		public const uint AUD3 = 9;
		public const uint AUD0 = 8;
		public const uint AUD2 = 7;
		public const uint BLIT = 6;
		public const uint VERTB = 5;
		public const uint COPPER = 4;
		public const uint PORTS = 3;
		public const uint TBE = 2;
		public const uint DSKBLK = 1;
		public const uint SOFTINT = 0;

		public static uint[] priority = new uint[]{ 1,1,1,2,3,3,3,4,4,4,4,5,5,6,6,7};

		public static uint CPUPriority(uint interrupt)
		{
			return priority[interrupt];
		}
	}
}
