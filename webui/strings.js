$(() =>
{
	$('#strings').on("click", function()
	{
		$.ajax({
			url: "http://localhost:8080/v1/jammy/debugger/memory",
			type: 'GET',
			dataType: 'json',
			success: function(res)
			{
				$('#stringtext').text(GetStrings(res.Contents, 4));
			},
			error: function(xhr, status, error)
			{
				console.log('' + xhr + ' ' + status + ' ' + error);
			}
		});
	});

	$('.action').on("click", function()
	{
		var action = $(this).data('action');
		$.ajax({
			url: "http://localhost:8080/v1/jammy/debugger/emuControl?_1=0",
			type: 'POST',
			data: action,
			contentType: "text/plain",
			error: function(xhr, status, error)
			{
				console.log('' + xhr + ' ' + status + ' ' + error);
			}
		});
	});

});

var charScore = 
[
	2,//space
	-1,//!
	-1,//"
	-1,//#
	-1,//$
	-3,//%
	-3,//&
	-1,//'
	-2,//(
	-2,//)
	-3,//*
	-3,//+
	-2,//,
	-3,//-
	-1,//.
	-1,//forward slash
	1, 1, 1, 1, 1, 1, 1, 1, 1, 1,//0-9
	-1,//:
	-3,//;
	-3,//<
	-3,//=
	-3,//>
	-1,//?
	-2,//@
	2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,//A-Z
	-3,//[
	-2,//backslash
	-3,//]
	-3,//^
	-3,//_
	-3,//`
	2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,//a-z
	-3,//{
	-3,//|
	-3,//},
	-3,//~
];

function Filter(s)
{
	function isAsciiLetter(c)
	{
		return (c >= 65 && c <= 90) || (c >= 97 && c <= 122);
	}

	//return true if the string is to be filtered out

	//Nu is a really common misinterpretation of a CPU instruction

	//if it's prefixed by anything other than ' ', it's unlikely to be a string
	var nu = s.indexOf("Nu");
	if (nu > 0 && s[nu - 1] != ' ') return true;

	//if it's at the start but not followed by a letter, it's unlikely to be a string
	if (nu == 0 && s.length > 2 && !isAsciiLetter(s[nu + 2])) return true;

	//if it's whitespace, it's not a string
	if (s.trim().length === 0) return true;

	//not filtered
	return false;
}

function CharScore(b)
{
	var c = b;
	c -= 32;
	if (c < 0 || c >= charScore.length) return 0;
	return charScore[c];
}

function IsString(b)
{
	return b >= 32 && b < 128;
}

function GetStrings(ram, minW)
{
	var startI;
	var sb = []
	var currentScore = 0;

	for (k = 0; k < ram.length; k++)
	{
		startI = -1;
		var mem = ram[k].Memory;
		mem = base64ToCharArray(mem);
		/*
		for (i = 0; i < mem.length; i++)
		{
			var isPrint = IsString(mem[i]);
			if (isPrint && startI == -1)
			{
				startI = i;
			}
			else if (!isPrint && startI != -1)
			{
				var len = i - startI;
				if (len >= minW)
				{
					sb.push(slice(mem, startI, len));
				}
				startI = -1;
			}
		}
		*/
		for (i = 0; i <= mem.length; i++)
		{
			//force a terminating null at the end of the buffer
			var score = i == mem.length ? 0 : CharScore(mem[i]);
			if (score != 0 && startI == -1)
			{
				startI = i;
				currentScore = score;
			}
			else if (score == 0 && startI != -1)
			{
				var len = i - startI;
				if (len >= minW && currentScore >= 0)
				{
					//var s = slice(mem, startI, len);
					var s = String.fromCharCode(...slice(mem, startI, len));
					if (!Filter(s))
						sb.push(s);
				}
				startI = -1;
			}
			else
			{
				currentScore += score;
			}
		}
	}
	//return sb.map(x => { return String.fromCharCode(...x); }).join("\n");
	return sb.join("\n");
}

function base64ToCharArray(base64)
{
	return Uint8Array.fromBase64(base64);
}

function slice(arr, offset, length)
{
	ll = arr.Length;
	return {
		length,
		offset,
		ll,
		[Symbol.iterator]()
		{
			let i = 0;
			return {
				next: () => ({
					value: arr[offset + i],
					done: i++ >= length
				})
			};
		}
	};
}
