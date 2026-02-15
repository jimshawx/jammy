$(document).ready(function()
{
	$('.s').click(function()
	{
		$.ajax({
			url: "http://localhost:8080/jammy/debugger/memory",
			type: 'GET',
			dataType: 'json',
			success: function(res)
			{
				$('.u').text(GetStrings(res.Contents));
			},
			error: function(xhr, status, error)
			{
				console.log('' + xhr + ' ' + status + ' ' + error);
			}
		});
	});
});

function IsString(b)
{
	return b >= 32 && b < 128;
}

var minW = 4;

function GetStrings(ram)
{
	var startI;
	var sb = []
	for (k = 0; k < ram.length; k++)
	{
		startI = -1;
		var mem = ram[k].Memory;
		mem = base64ToCharArray(mem);
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
	}
	return sb.map(x => { return String.fromCharCode(...x); }).join("\n");
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
