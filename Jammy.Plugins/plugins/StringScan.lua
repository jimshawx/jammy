strings = {}
minW = 4

function init()
    -- strings = GetStrings()
	for k,v in pairs(_G) do
    print(k .. " " .. type(v))
end

end

function update()
    ImGui.Begin("Strings", 64)

    if ImGui.Button("Scan") then
        strings = GetStrings()
    end

    showLargeTextWindow(strings)

    ImGui.End()
end

function showLargeTextWindow(logLines)
    -- Scrollable child region with horizontal scrollbar
    ImGui.BeginChild(
        "StringsX",
        Vec2(0, 0),
        false,
        2048 -- ImGuiWindowFlags.HorizontalScrollbar
    )

    -- Disable wrapping
    ImGui.PushTextWrapPos(3.4e38)

    -- ListClipper
    local clipper = ImGuiListClipper()
    clipper:Begin(#logLines)

    while clipper:Step() do
        for i = clipper.DisplayStart, clipper.DisplayEnd - 1 do
            ImGui.TextUnformatted(logLines[i + 1]) -- Lua is 1‑indexed
        end
    end

    clipper:End()

    ImGui.PopTextWrapPos()
    ImGui.EndChild()
end

function IsString(b)
    return b >= 32 and b < 128
end

function GetStrings()
    jammy.LockEmulation()
    local ram = jammy.GetMemoryContent().Contents
    jammy.UnlockEmulation()

    local results = {}

    for k = 1, ram.Count do
        local block = ram[k]
        local mem = block.Memory
        local startI = -1

        for i = 0, block.Length - 1 do
            local isPrint = IsString(mem[i])

            if isPrint and startI == -1 then
                startI = i
            elseif (not isPrint) and startI ~= -1 then
                local len = i - startI
                if len >= minW then
                    table.insert(results, slice(mem, startI, len))
                end
                startI = -1
            end
        end
    end

    -- Convert byte arrays to Lua strings
    local out = {}
    for i, arr in ipairs(results) do
        local chars = {}
        for b in arr do
            table.insert(chars, string.char(b))
        end
        out[i] = table.concat(chars)
    end

    return out
end

function slice(arr, offset, length)
    local i = 0

    return function()
        if i < length then
            local v = arr[offset + i]
            i = i + 1
            return v
        end
    end
end
