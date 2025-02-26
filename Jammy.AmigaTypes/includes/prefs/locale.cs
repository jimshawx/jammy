namespace Jammy.AmigaTypes;

public class CountryPrefs
{
	[AmigaArraySize(4)]
	public ULONG[] cp_Reserved { get; set; }
	public ULONG cp_CountryCode { get; set; }
	public ULONG cp_TelephoneCode { get; set; }
	public UBYTE cp_MeasuringSystem { get; set; }
	[AmigaArraySize(80)]
	public char[] cp_DateTimeFormat { get; set; }
	[AmigaArraySize(40)]
	public char[] cp_DateFormat { get; set; }
	[AmigaArraySize(40)]
	public char[] cp_TimeFormat { get; set; }
	[AmigaArraySize(80)]
	public char[] cp_ShortDateTimeFormat { get; set; }
	[AmigaArraySize(40)]
	public char[] cp_ShortDateFormat { get; set; }
	[AmigaArraySize(40)]
	public char[] cp_ShortTimeFormat { get; set; }
	[AmigaArraySize(10)]
	public char[] cp_DecimalPoint { get; set; }
	[AmigaArraySize(10)]
	public char[] cp_GroupSeparator { get; set; }
	[AmigaArraySize(10)]
	public char[] cp_FracGroupSeparator { get; set; }
	[AmigaArraySize(10)]
	public UBYTE[] cp_Grouping { get; set; }
	[AmigaArraySize(10)]
	public UBYTE[] cp_FracGrouping { get; set; }
	[AmigaArraySize(10)]
	public char[] cp_MonDecimalPoint { get; set; }
	[AmigaArraySize(10)]
	public char[] cp_MonGroupSeparator { get; set; }
	[AmigaArraySize(10)]
	public char[] cp_MonFracGroupSeparator { get; set; }
	[AmigaArraySize(10)]
	public UBYTE[] cp_MonGrouping { get; set; }
	[AmigaArraySize(10)]
	public UBYTE[] cp_MonFracGrouping { get; set; }
	public UBYTE cp_MonFracDigits { get; set; }
	public UBYTE cp_MonIntFracDigits { get; set; }
	[AmigaArraySize(10)]
	public char[] cp_MonCS { get; set; }
	[AmigaArraySize(10)]
	public char[] cp_MonSmallCS { get; set; }
	[AmigaArraySize(10)]
	public char[] cp_MonIntCS { get; set; }
	[AmigaArraySize(10)]
	public char[] cp_MonPositiveSign { get; set; }
	public UBYTE cp_MonPositiveSpaceSep { get; set; }
	public UBYTE cp_MonPositiveSignPos { get; set; }
	public UBYTE cp_MonPositiveCSPos { get; set; }
	[AmigaArraySize(10)]
	public char[] cp_MonNegativeSign { get; set; }
	public UBYTE cp_MonNegativeSpaceSep { get; set; }
	public UBYTE cp_MonNegativeSignPos { get; set; }
	public UBYTE cp_MonNegativeCSPos { get; set; }
	public UBYTE cp_CalendarType { get; set; }
}

public class LocalePrefs
{
	[AmigaArraySize(4)]
	public ULONG[] lp_Reserved { get; set; }
	[AmigaArraySize(32)]
	public char[] lp_CountryName { get; set; }
	[AmigaArraySize((10)*(30))]
	public char[] lp_PreferredLanguages { get; set; }
	public LONG lp_GMTOffset { get; set; }
	public ULONG lp_Flags { get; set; }
	public CountryPrefs lp_CountryData { get; set; }
}

