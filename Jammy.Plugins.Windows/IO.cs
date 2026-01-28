using System.Windows.Forms;
using ImGuiNET;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Plugins.Windows
{
	public class ImGuiInput
	{
		public static void SetImGuiInput(Control control)
		{
			control.MouseMove += (_, e) =>
			{
				ImGui.GetIO().AddMousePosEvent(e.X, e.Y);
			};

			control.MouseDown += (_, e) =>
			{
				if (e.Button == MouseButtons.Left)
					ImGui.GetIO().AddMouseButtonEvent(0, true);
				if (e.Button == MouseButtons.Right)
					ImGui.GetIO().AddMouseButtonEvent(1, true);
				if (e.Button == MouseButtons.Middle)
					ImGui.GetIO().AddMouseButtonEvent(2, true);
			};

			control.MouseUp += (_, e) =>
			{
				if (e.Button == MouseButtons.Left)
					ImGui.GetIO().AddMouseButtonEvent(0, false);
				if (e.Button == MouseButtons.Right)
					ImGui.GetIO().AddMouseButtonEvent(1, false);
				if (e.Button == MouseButtons.Middle)
					ImGui.GetIO().AddMouseButtonEvent(2, false);
			};

			control.MouseWheel += (_, e) =>
			{
				// WinForms gives wheel in "notches" of 120
				float delta = e.Delta > 0 ? 1f : -1f;
				ImGui.GetIO().AddMouseWheelEvent(0, delta);
			};

			control.KeyDown += (_, e) =>
			{
				var io = ImGui.GetIO();
				io.AddKeyEvent(MapKey(e.KeyCode), true);

				io.AddKeyEvent(ImGuiKey.ModCtrl, e.Control);
				io.AddKeyEvent(ImGuiKey.ModShift, e.Shift);
				io.AddKeyEvent(ImGuiKey.ModAlt, e.Alt);
			};

			control.KeyUp += (_, e) =>
			{
				var io = ImGui.GetIO();
				io.AddKeyEvent(MapKey(e.KeyCode), false);

				io.AddKeyEvent(ImGuiKey.ModCtrl, e.Control);
				io.AddKeyEvent(ImGuiKey.ModShift, e.Shift);
				io.AddKeyEvent(ImGuiKey.ModAlt, e.Alt);
			};

			control.KeyPress += (_, e) =>
			{
				ImGui.GetIO().AddInputCharacter(e.KeyChar);
			};
		}

		private static ImGuiKey MapKey(Keys key)
		{
			return key switch
			{
				Keys.Tab => ImGuiKey.Tab,
				Keys.Left => ImGuiKey.LeftArrow,
				Keys.Right => ImGuiKey.RightArrow,
				Keys.Up => ImGuiKey.UpArrow,
				Keys.Down => ImGuiKey.DownArrow,
				Keys.PageUp => ImGuiKey.PageUp,
				Keys.PageDown => ImGuiKey.PageDown,
				Keys.Home => ImGuiKey.Home,
				Keys.End => ImGuiKey.End,
				Keys.Insert => ImGuiKey.Insert,
				Keys.Delete => ImGuiKey.Delete,
				Keys.Back => ImGuiKey.Backspace,
				Keys.Space => ImGuiKey.Space,
				Keys.Enter => ImGuiKey.Enter,
				Keys.Escape => ImGuiKey.Escape,

				Keys.A => ImGuiKey.A,
				Keys.B => ImGuiKey.B,
				Keys.C => ImGuiKey.C,
				Keys.D => ImGuiKey.D,
				Keys.E => ImGuiKey.E,
				Keys.F => ImGuiKey.F,
				Keys.G => ImGuiKey.G,
				Keys.H => ImGuiKey.H,
				Keys.I => ImGuiKey.I,
				Keys.J => ImGuiKey.J,
				Keys.K => ImGuiKey.K,
				Keys.L => ImGuiKey.L,
				Keys.M => ImGuiKey.M,
				Keys.N => ImGuiKey.N,
				Keys.O => ImGuiKey.O,
				Keys.P => ImGuiKey.P,
				Keys.Q => ImGuiKey.Q,
				Keys.R => ImGuiKey.R,
				Keys.S => ImGuiKey.S,
				Keys.T => ImGuiKey.T,
				Keys.U => ImGuiKey.U,
				Keys.V => ImGuiKey.V,
				Keys.W => ImGuiKey.W,
				Keys.X => ImGuiKey.X,
				Keys.Y => ImGuiKey.Y,
				Keys.Z => ImGuiKey.Z,

				Keys.D0 => ImGuiKey._0,
				Keys.D1 => ImGuiKey._1,
				Keys.D2 => ImGuiKey._2,
				Keys.D3 => ImGuiKey._3,
				Keys.D4 => ImGuiKey._4,
				Keys.D5 => ImGuiKey._5,
				Keys.D6 => ImGuiKey._6,
				Keys.D7 => ImGuiKey._7,
				Keys.D8 => ImGuiKey._8,
				Keys.D9 => ImGuiKey._9,

				_ => ImGuiKey.None
			};
		}
	}
}