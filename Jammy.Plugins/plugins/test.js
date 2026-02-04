function update()
{
	imgui.Begin("My JS Window", 64);

	if (imgui.Button("Stop"))
		jammy.Stop();

	if (imgui.Button("Go"))
		jammy.Go();

	if (imgui.Button("Step"))
		jammy.Step();

	if (imgui.Button("Step Out"))
		jammy.StepOut();

	jammy.LockEmulation();
	var libs = jammy.GetLibraries();
	jammy.UnlockEmulation();

	imgui.Text(libs);

	//for (var i = 0; i < libs.Items.Count(); i++)
	//{
	//	console.log("Library: " + libs.Items[i]);
	//}


	imgui.End();
}