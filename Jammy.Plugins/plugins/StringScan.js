var strings = [];
var minW = 4;

function init() {
	//strings = GetStrings();
}

function update() {
	imgui.Begin("Strings", 64);
	if (imgui.Button("Scan")) {
		strings = GetStrings();
	}

	//imgui.TextUnformatted(strings);
	showLargeTextWindow(strings);
	imgui.End();
}

function showLargeTextWindow(logLines) {
	//imgui.Begin("Large Text Viewer");




	// Scrollable child region with horizontal scrollbar enabled
	imgui.BeginChild(
		"StringsX",
		new Vec2(0,0),
		//{ x: 0, y: 0 }, // Fill available space
		//true,                 // Border
		0,
		2048//imgui.WindowFlags.HorizontalScrollbar
	);

	// Ensure no wrapping so horizontal scroll works
	imgui.PushTextWrapPos(3.4e38);//imgui.FLT_MAX);

	// Use ListClipper to only render visible lines
	const clipper = new imgui.ListClipper();
	clipper.Begin(logLines.length);

	while (clipper.Step()) {
		for (let i = clipper.DisplayStart; i < clipper.DisplayEnd; i++) {
			imgui.TextUnformatted(logLines[i]);
		}
	}
	clipper.End();

	imgui.PopTextWrapPos();
	imgui.EndChild();
	//imgui.End();
}

function IsString(b) {
	return b >= 32 && b < 128;
}

function GetStrings() {
	jammy.LockEmulation();
	ram = jammy.GetMemoryContent().Contents;
	jammy.UnlockEmulation();

	var startI;
	var sb = []
	for (k = 0; k < ram.Count; k++)
	{
		startI = -1;
		var mem = ram[k].Memory;
		for (i = 0; i < ram[k].Length; i++) {
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
