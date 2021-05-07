namespace Jammy.Types
{
	public class Label
	{
		public Label( uint address, string name)
		{
			Name = name;
			Address = address;
		}

		public Label()
		{
		}

		public string Name { get; set; }
		public uint Address { get; set; }
	}
}
