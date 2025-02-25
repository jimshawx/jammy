namespace Jammy.AmigaTypes;

public struct opSet
{
	public ULONG MethodID { get; set; }
	public TagItemPtr ops_AttrList { get; set; }
	public GadgetInfoPtr ops_GInfo { get; set; }
}

public struct opUpdate
{
	public ULONG MethodID { get; set; }
	public TagItemPtr opu_AttrList { get; set; }
	public GadgetInfoPtr opu_GInfo { get; set; }
	public ULONG opu_Flags { get; set; }
}

public struct opGet
{
	public ULONG MethodID { get; set; }
	public ULONG opg_AttrID { get; set; }
	public ULONGPtr opg_Storage { get; set; }
}

public struct opAddTail
{
	public ULONG MethodID { get; set; }
	public ListPtr opat_List { get; set; }
}

public struct opMember
{
	public ULONG MethodID { get; set; }
	public ObjectPtr opam_Object { get; set; }
}

