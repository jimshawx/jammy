function update()
	imgui.Begin("My Lua Window", 64)

	if imgui.Button("Step") then
		jammy.Step()
	end

	if imgui.Button("Step Out") then
		jammy.StepOut()
	end

	if imgui.Button("Stop") then
		jammy.Stop()
	end

	if imgui.Button("Go") then
		jammy.Go()
	end

	imgui.End()

	--local x = jammy.GetRegs();
	--print(string.format("PC: %X", x.PC));

end