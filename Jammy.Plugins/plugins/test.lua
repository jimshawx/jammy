function update()
	imgui.Begin("My Lua Window", 64)

	if imgui.Button("Stop") then
		jammy.Stop()
	end

	if imgui.Button("Go") then
		jammy.Go()
	end

	imgui.End()
end
