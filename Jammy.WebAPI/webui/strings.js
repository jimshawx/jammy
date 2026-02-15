$(document).ready(function () {
	$(".t").click(function () {
		$("h2").text("Hello from jQuery!");
	});

	$('.s').click(function () {
		$.ajax({
			url: "http://localhost:8080/jammy/debugger/memory",
			type: 'GET',
			dataType: 'json',
			success: function (res) {

				//console.log(res);
				//var q = JSON.parse(res);
				$('.u').text(GetStrings(res.Contents));
			},
			error: function (xhr, status, error) {
				console.log('' + xhr + ' ' + status + ' ' + error);
			}
		});
	});
});

function GetStrings(ram) {

	var startI;
	var sb = []
	for (k = 0; k < ram.length; k++) {
		startI = -1;
		var mem = ram[k].Memory;
		for (i = 0; i < ram[k].length; i++) {
			var isPrint = IsString(mem[i]);
			if (isPrint && startI == -1) {
				startI = i;
			}
			else if (!isPrint && startI != -1) {
				var len = i - startI;
				if (len >= minW) {
					sb.push(slice(mem, startI, len));
				}
				startI = -1;
			}
		}
	}
	return sb.map(x => { return String.fromCharCode(...x); });//.join("\n");
}

function slice(arr, offset, length) {
	ll = arr.Length;
	return {
		length,
		offset,
		ll,
		[Symbol.iterator]() {
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
