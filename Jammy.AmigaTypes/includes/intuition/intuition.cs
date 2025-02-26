namespace Jammy.AmigaTypes;

public class Menu
{
	public MenuPtr NextMenu { get; set; }
	public WORD LeftEdge { get; set; }
	public WORD TopEdge { get; set; }
	public WORD Width { get; set; }
	public WORD Height { get; set; }
	public UWORD Flags { get; set; }
	public BYTEPtr MenuName { get; set; }
	public MenuItemPtr FirstItem { get; set; }
	public WORD JazzX { get; set; }
	public WORD JazzY { get; set; }
	public WORD BeatX { get; set; }
	public WORD BeatY { get; set; }
}

public class MenuItem
{
	public MenuItemPtr NextItem { get; set; }
	public WORD LeftEdge { get; set; }
	public WORD TopEdge { get; set; }
	public WORD Width { get; set; }
	public WORD Height { get; set; }
	public UWORD Flags { get; set; }
	public LONG MutualExclude { get; set; }
	public APTR ItemFill { get; set; }
	public APTR SelectFill { get; set; }
	public BYTE Command { get; set; }
	public MenuItemPtr SubItem { get; set; }
	public UWORD NextSelect { get; set; }
}

public class Requester
{
	public RequesterPtr OlderRequest { get; set; }
	public WORD LeftEdge { get; set; }
	public WORD TopEdge { get; set; }
	public WORD Width { get; set; }
	public WORD Height { get; set; }
	public WORD RelLeft { get; set; }
	public WORD RelTop { get; set; }
	public GadgetPtr ReqGadget { get; set; }
	public BorderPtr ReqBorder { get; set; }
	public IntuiTextPtr ReqText { get; set; }
	public UWORD Flags { get; set; }
	public UBYTE BackFill { get; set; }
	public LayerPtr ReqLayer { get; set; }
	[AmigaArraySize(32)]
	public UBYTE[] ReqPad1 { get; set; }
	public BitMapPtr ImageBMap { get; set; }
	public WindowPtr RWindow { get; set; }
	public ImagePtr ReqImage { get; set; }
	[AmigaArraySize(32)]
	public UBYTE[] ReqPad2 { get; set; }
}

public class Gadget
{
	public GadgetPtr NextGadget { get; set; }
	public WORD LeftEdge { get; set; }
	public WORD TopEdge { get; set; }
	public WORD Width { get; set; }
	public WORD Height { get; set; }
	public UWORD Flags { get; set; }
	public UWORD Activation { get; set; }
	public UWORD GadgetType { get; set; }
	public APTR GadgetRender { get; set; }
	public APTR SelectRender { get; set; }
	public IntuiTextPtr GadgetText { get; set; }
	public LONG MutualExclude { get; set; }
	public APTR SpecialInfo { get; set; }
	public UWORD GadgetID { get; set; }
	public APTR UserData { get; set; }
}

public class BoolInfo
{
	public UWORD Flags { get; set; }
	public UWORDPtr Mask { get; set; }
	public ULONG Reserved { get; set; }
}

public class PropInfo
{
	public UWORD Flags { get; set; }
	public UWORD HorizPot { get; set; }
	public UWORD VertPot { get; set; }
	public UWORD HorizBody { get; set; }
	public UWORD VertBody { get; set; }
	public UWORD CWidth { get; set; }
	public UWORD CHeight { get; set; }
	public UWORD HPotRes { get; set; }
	public UWORD VPotRes { get; set; }
	public UWORD LeftBorder { get; set; }
	public UWORD TopBorder { get; set; }
}

public class StringInfo
{
	public UBYTEPtr Buffer { get; set; }
	public UBYTEPtr UndoBuffer { get; set; }
	public WORD BufferPos { get; set; }
	public WORD MaxChars { get; set; }
	public WORD DispPos { get; set; }
	public WORD UndoPos { get; set; }
	public WORD NumChars { get; set; }
	public WORD DispCount { get; set; }
	public WORD CLeft { get; set; }
	public WORD CTop { get; set; }
	public StringExtendPtr Extension { get; set; }
	public LONG LongInt { get; set; }
	public KeyMapPtr AltKeyMap { get; set; }
}

public class IntuiText
{
	public UBYTE FrontPen { get; set; }
	public UBYTE BackPen { get; set; }
	public UBYTE DrawMode { get; set; }
	public WORD LeftEdge { get; set; }
	public WORD TopEdge { get; set; }
	public TextAttrPtr ITextFont { get; set; }
	public UBYTEPtr IText { get; set; }
	public IntuiTextPtr NextText { get; set; }
}

public class Border
{
	public WORD LeftEdge { get; set; }
	public WORD TopEdge { get; set; }
	public UBYTE FrontPen { get; set; }
	public UBYTE BackPen { get; set; }
	public UBYTE DrawMode { get; set; }
	public BYTE Count { get; set; }
	public WORDPtr XY { get; set; }
	public BorderPtr NextBorder { get; set; }
}

public class Image
{
	public WORD LeftEdge { get; set; }
	public WORD TopEdge { get; set; }
	public WORD Width { get; set; }
	public WORD Height { get; set; }
	public WORD Depth { get; set; }
	public UWORDPtr ImageData { get; set; }
	public UBYTE PlanePick { get; set; }
	public UBYTE PlaneOnOff { get; set; }
	public ImagePtr NextImage { get; set; }
}

public class IntuiMessage
{
	public Message ExecMessage { get; set; }
	public ULONG Class { get; set; }
	public UWORD Code { get; set; }
	public UWORD Qualifier { get; set; }
	public APTR IAddress { get; set; }
	public WORD MouseX { get; set; }
	public WORD MouseY { get; set; }
	public ULONG Seconds { get; set; }
	public ULONG Micros { get; set; }
	public WindowPtr IDCMPWindow { get; set; }
	public IntuiMessagePtr SpecialLink { get; set; }
}

public class IBox
{
	public WORD Left { get; set; }
	public WORD Top { get; set; }
	public WORD Width { get; set; }
	public WORD Height { get; set; }
}

public class Window
{
	public WindowPtr NextWindow { get; set; }
	public WORD LeftEdge { get; set; }
	public WORD TopEdge { get; set; }
	public WORD Width { get; set; }
	public WORD Height { get; set; }
	public WORD MouseY { get; set; }
	public WORD MouseX { get; set; }
	public WORD MinWidth { get; set; }
	public WORD MinHeight { get; set; }
	public UWORD MaxWidth { get; set; }
	public UWORD MaxHeight { get; set; }
	public ULONG Flags { get; set; }
	public MenuPtr MenuStrip { get; set; }
	public UBYTEPtr Title { get; set; }
	public RequesterPtr FirstRequest { get; set; }
	public RequesterPtr DMRequest { get; set; }
	public WORD ReqCount { get; set; }
	public ScreenPtr WScreen { get; set; }
	public RastPortPtr RPort { get; set; }
	public BYTE BorderLeft { get; set; }
	public BYTE BorderTop { get; set; }
	public BYTE BorderRight { get; set; }
	public BYTE BorderBottom { get; set; }
	public RastPortPtr BorderRPort { get; set; }
	public GadgetPtr FirstGadget { get; set; }
	public WindowPtr Parent { get; set; }
	public WindowPtr Descendant { get; set; }
	public UWORDPtr Pointer { get; set; }
	public BYTE PtrHeight { get; set; }
	public BYTE PtrWidth { get; set; }
	public BYTE XOffset { get; set; }
	public BYTE YOffset { get; set; }
	public ULONG IDCMPFlags { get; set; }
	public MsgPortPtr UserPort { get; set; }
	public MsgPortPtr WindowPort { get; set; }
	public IntuiMessagePtr MessageKey { get; set; }
	public UBYTE DetailPen { get; set; }
	public UBYTE BlockPen { get; set; }
	public ImagePtr CheckMark { get; set; }
	public UBYTEPtr ScreenTitle { get; set; }
	public WORD GZZMouseX { get; set; }
	public WORD GZZMouseY { get; set; }
	public WORD GZZWidth { get; set; }
	public WORD GZZHeight { get; set; }
	public UBYTEPtr ExtData { get; set; }
	public BYTEPtr UserData { get; set; }
	public LayerPtr WLayer { get; set; }
	public TextFontPtr IFont { get; set; }
	public ULONG MoreFlags { get; set; }
}

public class NewWindow
{
	public WORD LeftEdge { get; set; }
	public WORD TopEdge { get; set; }
	public WORD Width { get; set; }
	public WORD Height { get; set; }
	public UBYTE DetailPen { get; set; }
	public UBYTE BlockPen { get; set; }
	public ULONG IDCMPFlags { get; set; }
	public ULONG Flags { get; set; }
	public GadgetPtr FirstGadget { get; set; }
	public ImagePtr CheckMark { get; set; }
	public UBYTEPtr Title { get; set; }
	public ScreenPtr Screen { get; set; }
	public BitMapPtr BitMap { get; set; }
	public WORD MinWidth { get; set; }
	public WORD MinHeight { get; set; }
	public UWORD MaxWidth { get; set; }
	public UWORD MaxHeight { get; set; }
	public UWORD Type { get; set; }
}

public class ExtNewWindow
{
	public WORD LeftEdge { get; set; }
	public WORD TopEdge { get; set; }
	public WORD Width { get; set; }
	public WORD Height { get; set; }
	public UBYTE DetailPen { get; set; }
	public UBYTE BlockPen { get; set; }
	public ULONG IDCMPFlags { get; set; }
	public ULONG Flags { get; set; }
	public GadgetPtr FirstGadget { get; set; }
	public ImagePtr CheckMark { get; set; }
	public UBYTEPtr Title { get; set; }
	public ScreenPtr Screen { get; set; }
	public BitMapPtr BitMap { get; set; }
	public WORD MinWidth { get; set; }
	public WORD MinHeight { get; set; }
	public UWORD MaxWidth { get; set; }
	public UWORD MaxHeight { get; set; }
	public UWORD Type { get; set; }
	public TagItemPtr Extension { get; set; }
}

public class Remember
{
	public RememberPtr NextRemember { get; set; }
	public ULONG RememberSize { get; set; }
	public UBYTEPtr Memory { get; set; }
}

public class ColorSpec
{
	public WORD ColorIndex { get; set; }
	public UWORD Red { get; set; }
	public UWORD Green { get; set; }
	public UWORD Blue { get; set; }
}

public class EasyStruct
{
	public ULONG es_StructSize { get; set; }
	public ULONG es_Flags { get; set; }
	public UBYTEPtr es_Title { get; set; }
	public UBYTEPtr es_TextFormat { get; set; }
	public UBYTEPtr es_GadgetFormat { get; set; }
}

