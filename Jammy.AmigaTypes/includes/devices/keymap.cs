namespace Jammy.AmigaTypes;

public class KeyMap
{
	public UBYTEPtr km_LoKeyMapTypes { get; set; }
	public ULONGPtr km_LoKeyMap { get; set; }
	public UBYTEPtr km_LoCapsable { get; set; }
	public UBYTEPtr km_LoRepeatable { get; set; }
	public UBYTEPtr km_HiKeyMapTypes { get; set; }
	public ULONGPtr km_HiKeyMap { get; set; }
	public UBYTEPtr km_HiCapsable { get; set; }
	public UBYTEPtr km_HiRepeatable { get; set; }
}

public class KeyMapNode
{
	public Node kn_Node { get; set; }
	public KeyMap kn_KeyMap { get; set; }
}

public class KeyMapResource
{
	public Node kr_Node { get; set; }
	public List kr_List { get; set; }
}

