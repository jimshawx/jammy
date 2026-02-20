var strings = [];
var minW = 4;

function init() {
	//strings = GetStrings();
	console.log("init js");

	//jammy.AddBreakpoint(0xFC0F90, 0, 0, 2, fn);
	//jammy.AddBreakpoint(0xFC0F90, 0, 0, 2);
	jammy.AddBreakpoint(0xFC0F90, 0, 0, 2, breakpointFn);
	strings = GetStrings();
	console.log("#strings " + strings.length);
	//for (var i = 0; i < strings.length; i++)
	//	console.log(strings[i]);
	console.log(1, 2, 3, 4, 5);
	console.log();
	console.assert(1, "hello0");
	console.assert([], "hello1a");
	console.assert({}, "hello1b");
	console.assert({a:2}, "hello1c");
	console.assert([3], "hello1d");
	x = 2;
	console.assert(x, "hello2");
	console.assert(null, "hello4");
	console.assert(0, "hello5");
	console.assert(undefined, "hello3");
	console.assert(NaN, "hello6");

	console.log("This is the outer level");
	console.group();
	console.log("Level 2");
	console.group();
	console.log("Level 3");
	console.warn("More of level 3");
	console.groupEnd();
	console.log("Back to level 2");
	console.groupEnd();
	console.log("Back to the outer level");

	console.log("Hello world!");
	console.group("myLabel");
	console.log("Hello again, this time inside a group, with a label!");
	console.groupEnd();
	console.log("and we are back.");
}

function breakpointFn(bp)
{
	console.log('hit ' + bp.Address);
	return true;
}

function update() {
	//console.log("update js");
}

function IsString(b) {
	return b >= 32 && b < 128;
}

function GetStrings() {
	//jammy.LockEmulation();
	ram = jammy.GetMemoryContent();
	//jammy.UnlockEmulation();

	//for (i = 0; i < ram.Contents.Count; i++)
	//	ram.Contents[i].Memory = [i,2,3,4];
	//console.dirxml(ram);

	ram = ram.Contents;

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
