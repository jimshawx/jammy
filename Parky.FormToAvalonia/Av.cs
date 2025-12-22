using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Simple;
using AvaloniaTypes;
using ReactiveUI.Avalonia;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using System.Xml.Serialization;
using Application = Avalonia.Application;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Parky.FormToAvalonia
{
	public static class Av
	{
		private static Form ff;
		public static void Convert(Form f)
		{
			ff = f;
			AvUI.Run(Array.Empty<string>());
		}

		private readonly static List<string> methods = new List<string>();

		private static string ConvertW(int w) { return ((int)(w / 2.5)).ToString(); }
		private static string ConvertH(int h) { return ((int)(h / 2.5)).ToString(); }
		private static string ConvertT(int t) { return ((int)(t / 2.5)).ToString(); }
		private static string ConvertL(int l) { return ((int)(l / 2.5)).ToString(); }

		public static Avalonia.Controls.Window AvUiWindow()
		{
			methods.Clear();
			var rootWindow = new AvaloniaTypes.Window();
			rootWindow.Content = new AvaloniaTypes.Canvas { Width = ConvertW(ff.Width), Height = ConvertH(ff.Height) };
			rootWindow.Title = ff.Text;
			Debug.WriteLine("Begin Conversion");
			Walk(ff, rootWindow.Content);

			try
			{ 
				var serializer = new XmlSerializer(typeof(AvaloniaTypes.Window));
				using var writer = new StringWriter();
				serializer.Serialize(writer, rootWindow);
				var avaloniaXml = writer.ToString();
				Debug.WriteLine(avaloniaXml);
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Conversion Failed!");
				Debug.WriteLine(ex.ToString());
			}

			foreach (var m in methods)
				Debug.WriteLine($"private void {m}(object sender, RoutedEventArgs e) {{}}");	

			return new Avalonia.Controls.Window();
		}
	
		private static void Walk(System.Windows.Forms.Control w, AvaloniaTypes.Control a)
		{
			foreach (var c in w.Controls)
			{
				var cc = (System.Windows.Forms.Control)c;
				AvaloniaTypes.Control ac = null;
				
				switch (cc)
				{
					case System.Windows.Forms.Button:
						ac = new AvaloniaTypes.Button { Content = cc.Text };
						string ce = ShowEventHandlers(cc, "s_clickEvent");
						if (!string.IsNullOrEmpty(ce))
						{
							methods.Add(ce);
							((AvaloniaTypes.Button)ac).Click = ce;
						}
						break;
					case System.Windows.Forms.TextBox:
						ac = new AvaloniaTypes.TextBox { Text = cc.Text };
						var tb = (System.Windows.Forms.TextBox)cc;
						if (tb.Multiline)
							((AvaloniaTypes.TextBox)ac).AcceptsReturn = "True";
						break;
					case System.Windows.Forms.Label:
						ac = new AvaloniaTypes.Label { Content = cc.Text };
						break;
					case System.Windows.Forms.Panel:
						ac = new AvaloniaTypes.Canvas();
						break;
					case System.Windows.Forms.RadioButton:
						ac = new AvaloniaTypes.RadioButton();

						var rb = (AvaloniaTypes.RadioButton)ac;

						string sc = ShowEventHandlers(cc, "s_checkedChangedEvent");
						if (!string.IsNullOrEmpty(sc))
						{ 
							methods.Add(sc);
							rb.IsCheckChanged = sc;
						}
						break;
					case System.Windows.Forms.ListBox:
						ac = new AvaloniaTypes.ListBox();
						break;

					case System.Windows.Forms.RichTextBox:
						ac = new AvaloniaTypes.TextBox();
						break;

					case System.Windows.Forms.ComboBox:
						ac = new AvaloniaTypes.ComboBox();
						var bc = (AvaloniaTypes.ComboBox)ac;
						var combo = (System.Windows.Forms.ComboBox)cc;

						string sd = ShowEventHandlers(cc, "s_selectedIndexChangedEvent");
						if (!string.IsNullOrEmpty(sd))
						{
							methods.Add(sd);
							bc.SelectionChanged = sd;
						}
						foreach (var item in combo.Items)
							bc.Items.Add(item.ToString());
						break;

					case System.Windows.Forms.TabControl:
						ac = new AvaloniaTypes.TabControl();
						((AvaloniaTypes.Canvas)a).Children.Add(ac);
						if (a is AvaloniaTypes.Canvas)
						{
							ac.Left = cc.Left.ToString();
							ac.Top = cc.Top.ToString();
						}

						// set common properties
						ac.Width =  ConvertW(cc.Width);
						ac.Height = ConvertH(cc.Height);
						ac.Name = cc.Name;
						ac.Margin = $"{0} {0} {0} {0}";

						var tabControl = (AvaloniaTypes.TabControl)ac;
						var tab = (System.Windows.Forms.TabControl)cc;
						foreach (var page in tab.TabPages.Cast<System.Windows.Forms.Control>())
						{
							var ti = new AvaloniaTypes.TabItem { Header = page.Text };
							var p = new AvaloniaTypes.Canvas();
							p.Width = ConvertW(page.Width);
							p.Height = ConvertH(page.Height);
							p.Left = ConvertL(page.Left);
							p.Top = ConvertT(page.Top);
							ti.Content = p;
							tabControl.Items.Add(ti);
							Walk(page, p);
						}
						continue;

					case System.Windows.Forms.SplitContainer:
						var split = (System.Windows.Forms.SplitContainer)cc;
						ac = new AvaloniaTypes.Grid { Width = ConvertW(cc.Width), Height = ConvertH(cc.Height) };
						((AvaloniaTypes.Canvas)a).Children.Add(ac);
						if (a is AvaloniaTypes.Canvas)
						{
							ac.Left = ConvertL(cc.Left);
							ac.Top = ConvertT(cc.Top);
						}

						// set common properties
						ac.Width = ConvertW(cc.Width);
						ac.Height = ConvertH(cc.Height);
						ac.Name = cc.Name;
						ac.Margin = $"{0} {0} {0} {0}";

						var grid = (AvaloniaTypes.Grid)ac;
						var left = new AvaloniaTypes.Canvas();
						left.Width = ConvertW(cc.Width);
						left.Height = ConvertH(cc.Height);
						var right = new AvaloniaTypes.Canvas();
						right.Width = ConvertW(cc.Width);
						right.Height = ConvertH(cc.Height);
						var splitter = new AvaloniaTypes.GridSplitter();
						if (split.Orientation == Orientation.Vertical)
						{ 
							grid.ColumnDefinitions = "*,Auto,*";
							splitter.ResizeDirection = "Columns";
							splitter.Width = "10";
							left.Width = ConvertW(split.Panel1.Width);
							left.GridColumn = "0";
							splitter.GridColumn = "1";
							right.Width = ConvertW(split.Panel2.Width);
							right.GridColumn = "2";
						}
						else
						{ 
							grid.RowDefinitions = "*,Auto,*";
							splitter.ResizeDirection = "Rows";
							splitter.Height = "10";
							left.Height = ConvertH(split.Panel1.Height);
							left.GridRow = "0";
							splitter.GridRow = "1";
							right.Height = ConvertH(split.Panel2.Height);
							right.GridRow = "2";
						}

						grid.Children.Add(left);
						grid.Children.Add(splitter);
						grid.Children.Add(right);

						Walk(split.Panel1, left);
						Walk(split.Panel2, right);
						continue;

					case System.Windows.Forms.PictureBox:
						ac = new AvaloniaTypes.Canvas { Width = ConvertW(cc.Width), Height = ConvertH(cc.Height) };
						break;
					default:
						Debug.WriteLine($"Need to convert Windows type {cc.GetType()}");
						ac = new AvaloniaTypes.ContentControl();
						break;
				}

				// set common properties
				ac.Width = ConvertW(cc.Width);
				ac.Height = ConvertH(cc.Height);
				ac.Name = cc.Name;
				ac.Margin = $"{0} {0} {0} {0}";

				// add to parent
				if (a is AvaloniaTypes.Canvas panel)
				{
					ac.Left = ConvertL(cc.Left);
					ac.Top = ConvertT(cc.Top);
					panel.Children.Add(ac);
				}
				else
				{ 
					Debug.WriteLine($"Parent isn't a Canvas! {a.GetType()}");
				}
				// recurse
				Walk(cc, ac);
			}
		}

		private static string ShowEventHandlers(System.Windows.Forms.Control control, string eventFieldName)
		{
			try
			{
				// Get the type of Control
				Type type = typeof(System.Windows.Forms.Control);

				// Get the private static field 'EventClick' (or other event key)
				var allFields = type.GetFields(BindingFlags.Static | BindingFlags.NonPublic);
				FieldInfo eventKeyField = type.GetField(eventFieldName, BindingFlags.Static | BindingFlags.NonPublic);
				if (eventKeyField == null)
				{
					Debug.WriteLine($"Event key '{eventFieldName}' not found.");
					return "";
				}

				object eventKey = eventKeyField.GetValue(null);

				// Get the 'Events' property (EventHandlerList)
				PropertyInfo eventsProp = type.GetProperty("Events", BindingFlags.Instance | BindingFlags.NonPublic);
				EventHandlerList eventHandlerList = (EventHandlerList)eventsProp.GetValue(control);

				// Get the delegate for this event
				Delegate handlers = eventHandlerList[eventKey];
				if (handlers == null)
				{
					Debug.WriteLine("No handlers attached.");
					return "";
				}

				// List all subscribed methods
				foreach (Delegate d in handlers.GetInvocationList())
				{
					//Debug.WriteLine($"Handler method: {d.Method.Name}, Declaring type: {d.Method.DeclaringType}");
					return d.Method.Name;
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error inspecting event handlers: {ex.Message}");
			}
			return "";
		}
	}

	public static class AvUI
	{
		public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>() // `App` is child of `Application`
		.UsePlatformDetect()
		//.LogToTrace(LogEventLevel.Verbose)
		.UseReactiveUI();

		public static void Run(string[] args)
		{
			BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
		}
	}

	public class App : Application
	{
		public override void Initialize()
		{
			Styles.Add(new SimpleTheme());
		}

		public override void OnFrameworkInitializationCompleted()
		{
			if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
				desktop.MainWindow = Av.AvUiWindow();
			base.OnFrameworkInitializationCompleted();
		}
	}
}
