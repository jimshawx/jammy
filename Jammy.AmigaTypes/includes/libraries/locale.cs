namespace Jammy.AmigaTypes;

public class LocaleBase
{
	public Library lb_LibNode { get; set; }
	public BOOL lb_SysPatches { get; set; }
}

public class Locale
{
	public STRPTR loc_LocaleName { get; set; }
	public STRPTR loc_LanguageName { get; set; }
	[AmigaArraySize(10)]
	public STRPTR[] loc_PrefLanguages { get; set; }
	public ULONG loc_Flags { get; set; }
	public ULONG loc_CodeSet { get; set; }
	public ULONG loc_CountryCode { get; set; }
	public ULONG loc_TelephoneCode { get; set; }
	public LONG loc_GMTOffset { get; set; }
	public UBYTE loc_MeasuringSystem { get; set; }
	public UBYTE loc_CalendarType { get; set; }
	[AmigaArraySize(2)]
	public UBYTE[] loc_Reserved0 { get; set; }
	public STRPTR loc_DateTimeFormat { get; set; }
	public STRPTR loc_DateFormat { get; set; }
	public STRPTR loc_TimeFormat { get; set; }
	public STRPTR loc_ShortDateTimeFormat { get; set; }
	public STRPTR loc_ShortDateFormat { get; set; }
	public STRPTR loc_ShortTimeFormat { get; set; }
	public STRPTR loc_DecimalPoint { get; set; }
	public STRPTR loc_GroupSeparator { get; set; }
	public STRPTR loc_FracGroupSeparator { get; set; }
	public UBYTEPtr loc_Grouping { get; set; }
	public UBYTEPtr loc_FracGrouping { get; set; }
	public STRPTR loc_MonDecimalPoint { get; set; }
	public STRPTR loc_MonGroupSeparator { get; set; }
	public STRPTR loc_MonFracGroupSeparator { get; set; }
	public UBYTEPtr loc_MonGrouping { get; set; }
	public UBYTEPtr loc_MonFracGrouping { get; set; }
	public UBYTE loc_MonFracDigits { get; set; }
	public UBYTE loc_MonIntFracDigits { get; set; }
	[AmigaArraySize(2)]
	public UBYTE[] loc_Reserved1 { get; set; }
	public STRPTR loc_MonCS { get; set; }
	public STRPTR loc_MonSmallCS { get; set; }
	public STRPTR loc_MonIntCS { get; set; }
	public STRPTR loc_MonPositiveSign { get; set; }
	public UBYTE loc_MonPositiveSpaceSep { get; set; }
	public UBYTE loc_MonPositiveSignPos { get; set; }
	public UBYTE loc_MonPositiveCSPos { get; set; }
	public UBYTE loc_Reserved2 { get; set; }
	public STRPTR loc_MonNegativeSign { get; set; }
	public UBYTE loc_MonNegativeSpaceSep { get; set; }
	public UBYTE loc_MonNegativeSignPos { get; set; }
	public UBYTE loc_MonNegativeCSPos { get; set; }
	public UBYTE loc_Reserved3 { get; set; }
}

public class Catalog
{
	public Node cat_Link { get; set; }
	public UWORD cat_Pad { get; set; }
	public STRPTR cat_Language { get; set; }
	public ULONG cat_CodeSet { get; set; }
	public UWORD cat_Version { get; set; }
	public UWORD cat_Revision { get; set; }
}

