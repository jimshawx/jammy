namespace Jammy.Core.Floppy.DMS;

public static partial class xDMS
{

	/* Functions return codes */
	public const int NO_PROBLEM = 0;
	public const int FILE_END = 1;
	public const int ERR_NOMEMORY = 2;
	public const int ERR_CANTOPENIN = 3;
	public const int ERR_CANTOPENOUT = 4;
	public const int ERR_NOTDMS = 5;
	public const int ERR_SREAD = 6;
	public const int ERR_HCRC = 7;
	public const int ERR_NOTTRACK = 8;
	public const int ERR_BIGTRACK = 9;
	public const int ERR_THCRC = 10;
	public const int ERR_TDCRC = 11;
	public const int ERR_CSUM = 12;
	public const int ERR_CANTWRITE = 13;
	public const int ERR_BADDECR = 14;
	public const int ERR_UNKNMODE = 15;
	public const int ERR_NOPASSWD = 16;
	public const int ERR_BADPASSWD = 17;
	public const int ERR_FMS = 18;
	public const int ERR_GZIP = 19;
	public const int ERR_READDISK = 20;


	/* Command to execute */
	public const int CMD_VIEW = 1;
	public const int CMD_VIEWFULL = 2;
	public const int CMD_SHOWDIZ = 3;
	public const int CMD_SHOWBANNER = 4;
	public const int CMD_TEST = 5;
	public const int CMD_UNPACK = 6;
	public const int CMD_UNPKGZ = 7;
	public const int CMD_EXTRACT = 8;


	public const int OPT_VERBOSE = 1;
	public const int OPT_QUIET = 2;
}
