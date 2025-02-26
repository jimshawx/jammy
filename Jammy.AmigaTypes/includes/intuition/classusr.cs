namespace Jammy.AmigaTypes;

public class opSet
{
	public ULONG MethodID { get; set; }
	public TagItemPtr ops_AttrList { get; set; }
	public GadgetInfoPtr ops_GInfo { get; set; }
}

public class opUpdate
{
	public ULONG MethodID { get; set; }
	public TagItemPtr opu_AttrList { get; set; }
	public GadgetInfoPtr opu_GInfo { get; set; }
	public ULONG opu_Flags { get; set; }
}

public class opGet
{
	public ULONG MethodID { get; set; }
	public ULONG opg_AttrID { get; set; }
	public ULONGPtr opg_Storage { get; set; }
}

public class opAddTail
{
	public ULONG MethodID { get; set; }
	public ListPtr opat_List { get; set; }
}

public class opMember
{
	public ULONG MethodID { get; set; }
	public ObjectPtr opam_Object { get; set; }
}

