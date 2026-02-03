function update()
{
	imgui.Begin("My JS Window", 64);

	if (imgui.Button("Stop"))
		jammy.Stop();

	if (imgui.Button("Go"))
		jammy.Go();

	imgui.End();
}