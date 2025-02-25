namespace Jammy.AmigaTypes;

public struct VSprite
{
	public VSpritePtr NextVSprite { get; set; }
	public VSpritePtr PrevVSprite { get; set; }
	public VSpritePtr DrawPath { get; set; }
	public VSpritePtr ClearPath { get; set; }
	public WORD OldY { get; set; }
	public WORD OldX { get; set; }
	public WORD Flags { get; set; }
	public WORD Y { get; set; }
	public WORD X { get; set; }
	public WORD Height { get; set; }
	public WORD Width { get; set; }
	public WORD Depth { get; set; }
	public WORD MeMask { get; set; }
	public WORD HitMask { get; set; }
	public WORDPtr ImageData { get; set; }
	public WORDPtr BorderLine { get; set; }
	public WORDPtr CollMask { get; set; }
	public WORDPtr SprColors { get; set; }
	public BobPtr VSBob { get; set; }
	public BYTE PlanePick { get; set; }
	public BYTE PlaneOnOff { get; set; }
	public VUserStuff VUserExt { get; set; }
}

public struct Bob
{
	public WORD Flags { get; set; }
	public WORDPtr SaveBuffer { get; set; }
	public WORDPtr ImageShadow { get; set; }
	public BobPtr Before { get; set; }
	public BobPtr After { get; set; }
	public VSpritePtr BobVSprite { get; set; }
	public AnimCompPtr BobComp { get; set; }
	public DBufPacketPtr DBuffer { get; set; }
	public BUserStuff BUserExt { get; set; }
}

public struct AnimComp
{
	public WORD Flags { get; set; }
	public WORD Timer { get; set; }
	public WORD TimeSet { get; set; }
	public AnimCompPtr NextComp { get; set; }
	public AnimCompPtr PrevComp { get; set; }
	public AnimCompPtr NextSeq { get; set; }
	public AnimCompPtr PrevSeq { get; set; }
	public FunctionPtr AnimCRoutine { get; set; }
	public WORD YTrans { get; set; }
	public WORD XTrans { get; set; }
	public AnimObPtr HeadOb { get; set; }
	public BobPtr AnimBob { get; set; }
}

public struct AnimOb
{
	public AnimObPtr NextOb { get; set; }
	public AnimObPtr PrevOb { get; set; }
	public LONG Clock { get; set; }
	public WORD AnOldY { get; set; }
	public WORD AnOldX { get; set; }
	public WORD AnY { get; set; }
	public WORD AnX { get; set; }
	public WORD YVel { get; set; }
	public WORD XVel { get; set; }
	public WORD YAccel { get; set; }
	public WORD XAccel { get; set; }
	public WORD RingYTrans { get; set; }
	public WORD RingXTrans { get; set; }
	public FunctionPtr AnimORoutine { get; set; }
	public AnimCompPtr HeadComp { get; set; }
	public AUserStuff AUserExt { get; set; }
}

public struct DBufPacket
{
	public WORD BufY { get; set; }
	public WORD BufX { get; set; }
	public VSpritePtr BufPath { get; set; }
	public WORDPtr BufBuffer { get; set; }
}

public struct collTable
{
	[AmigaArraySize(16)]
	public FunctionPtr[] collPtrs { get; set; }
}

