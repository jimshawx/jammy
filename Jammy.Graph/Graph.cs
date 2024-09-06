using Jammy.Core.Interface.Interfaces;
using Jammy.Debugger.Types;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Jammy.Graph
{
	using GVC_t = IntPtr;
	using Agraph_t = IntPtr;
	using Agdesc_t = uint;
	using Agnode_t = IntPtr;
	using Agedge_t = IntPtr;
	using Agsym_t = IntPtr;
	using graph_t = IntPtr;
	using HBRUSH = IntPtr;

	public interface IGraph
	{
		void GraphBranches(PC_TRACE trace);
	}

	//public struct Agdesc_s
	//{
	//  /* graph descriptor */
	//	unsigned directed:1;    /* if edges are asymmetric */
	//	unsigned strict:1;      /* if multi-edges forbidden */
	//	unsigned no_loop:1;     /* if no loops */
	//	unsigned maingraph:1;   /* if this is the top level graph */
	//	unsigned flatlock:1;    /* if sets are flattened into lists in cdt */
	//	unsigned no_write:1;    /* if a temporary subgraph */
	//	unsigned has_attrs:1;   /* if string attr tables should be initialized */
	//	unsigned has_cmpnd:1;   /* if may contain collapsed nodes */
	//};

	public class Graph : IGraph
	{
		[DllImport("graphviz/x64/gvc.dll")]
		private extern static GVC_t gvContext();

		[DllImport("graphviz/x64/gvc.dll")]
		private extern static IntPtr gvcVersion(GVC_t gvc);

		[DllImport("graphviz/x64/gvc.dll")]
		private extern static IntPtr gvcBuildDate(GVC_t gvc);

		[DllImport("graphviz/x64/gvc.dll")]
		private extern static IntPtr gvcInfo(GVC_t gvc);

		[DllImport("graphviz/x64/gvc.dll")]
		private extern static void gvFreeContext(GVC_t gvc);

		[DllImport("graphviz/x64/gvc.dll")]
		private extern static int gvLayout(GVC_t gvc, graph_t g, string engine);

		[DllImport("graphviz/x64/gvc.dll")]
		private extern static int gvRenderFilename(GVC_t gvc, graph_t g, string format, string filename);

		[DllImport("graphviz/x64/gvc.dll")]
		private extern static int gvRenderData(GVC_t gvc, graph_t g, string format, out IntPtr result, out uint length);

		[DllImport("graphviz/x64/gvc.dll")]
		private extern static void gvFreeRenderData(IntPtr data);

		[DllImport("graphviz/x64/gvc.dll")]
		private extern static int gvFreeLayout(GVC_t gvc, graph_t g);

		[DllImport("graphviz/x64/gvc.dll")]
		private extern static void attach_attrs(GVC_t gvc);

		[DllImport("graphviz/x64/cgraph.dll")]
		private extern static Agraph_t agopen(string name, Agdesc_t desc, IntPtr disc);

		[DllImport("graphviz/x64/cgraph.dll")]
		private extern static Agnode_t agnode(Agraph_t g, string name, int createflag);

		[DllImport("graphviz/x64/cgraph.dll")]
		private extern static Agedge_t agedge(Agraph_t g, Agnode_t t, Agnode_t h, string name, int createflag);

		[DllImport("graphviz/x64/cgraph.dll")]
		private extern static Agsym_t agattr(Agraph_t g, int kind, string name, string value);

		[DllImport("graphviz/x64/cgraph.dll")]
		private extern static int agxset(IntPtr obj, Agsym_t sym, string value);

		[DllImport("graphviz/x64/cgraph.dll")]
		private extern static IntPtr agxget(IntPtr obj, Agsym_t sym);

		[DllImport("graphviz/x64/cgraph.dll")]
		private extern static int agclose(Agraph_t g);

		private const int AGRAPH = 0;
		private const int AGNODE = 1;
		private const int AGOUTEDGE = 2;
		private const int AGINEDGE = 3;
		private const int AGEDGE = AGOUTEDGE;

		private const int TRUE = 1;

		private static int RGB(int r, int g, int b) { return (b << 16) | (g << 8) | r; }

		private readonly ILogger logger;
		private readonly Jammy.Disassembler.Disassembler disassembler;
		private readonly IDebugMemoryMapper memory;

		public Graph(IMemoryMapper memory, ILogger<Graph> logger)
		{
			this.logger = logger;
			this.memory = (IDebugMemoryMapper)memory;
			disassembler = new Jammy.Disassembler.Disassembler();

			var gvc = gvContext();

			logger.LogTrace(Marshal.PtrToStringAnsi(gvcVersion(gvc)));
			logger.LogTrace(Marshal.PtrToStringAnsi(gvcBuildDate(gvc)));

			var info = gvcInfo(gvc);
			for (int i = 0; i < 3; i++)
			{
				var p = Marshal.ReadIntPtr(info, i * IntPtr.Size);
				var t = Marshal.PtrToStringAnsi(p);
				logger.LogTrace($"{t}");
			}

			gvFreeContext(gvc);

			//Test();
		}

		private void Test()
		{
			//Agdesc_t strictdirected = { 1, 1, 0, 1 };
			uint strictdirected = 0b1011;

			Agraph_t g = agopen("G", strictdirected, IntPtr.Zero);

			Agnode_t n0, n1, n2, n3, n4;
			n0 = agnode(g, "n0", TRUE);
			n1 = agnode(g, "n1", TRUE);
			n2 = agnode(g, "n2", TRUE);
			n3 = agnode(g, "n3", TRUE);
			n4 = agnode(g, "n4", TRUE);

			Agedge_t e0, e1, e2, e3, e4, e5;
			e0 = agedge(g, n0, n1, "e0", TRUE);
			e1 = agedge(g, n1, n2, "e1", TRUE);
			e2 = agedge(g, n2, n3, "e2", TRUE);
			e3 = agedge(g, n2, n4, "e3", TRUE);
			e4 = agedge(g, n4, n0, "e3", TRUE);
			e5 = agedge(g, n1, n3, "e5", TRUE);

			agattr(g, AGRAPH, "splines", "polyline");
			agattr(g, AGNODE, "shape", "box");

			Agsym_t w, h;
			w = agattr(g, AGNODE, "width", "1.0");
			h = agattr(g, AGNODE, "height", "1.0");
			agxset(n4, w, "0.2");
			agxset(n4, h, "0.3");

			GVC_t gvc;
			gvc = gvContext();

			int err;
			err = gvLayout(gvc, g, "dot");
			err = gvRenderFilename(gvc, g, "dot", "gv.txt");
			err = gvRenderFilename(gvc, g, "png", "gv.png");

			IntPtr graph;
			uint length;
			err = gvRenderData(gvc, g, "dot", out graph, out length);
			if (graph != IntPtr.Zero)
			{
				logger.LogTrace(Marshal.PtrToStringAnsi(graph));
				gvFreeRenderData(graph);
			}
			gvFreeLayout(gvc, g);
			gvFreeContext(gvc);
			agclose(g);
		}

		private double atof(string c)
		{
			return double.Parse(c);
		}

		private int ptsToDev(string c)
		{
			return (int)Math.Ceiling(window_dpi / 72.0 * atof(c) * output_scale);
		}

		private int insToDev(string c)
		{
			return (int)Math.Ceiling(window_dpi * atof(c) * output_scale);
		}

		private double devToIns(int d)
		{
			return d / window_dpi;
		}

		private Form CreateGraphWindow(uint pc, Font font)
		{
			var f = new Form { Width = 100, Height = 100, Font = font, Name = "Graph", Text = $"Graph @{pc:X8}", 
				ControlBox = true, FormBorderStyle = FormBorderStyle.SizableToolWindow, MinimizeBox = true, MaximizeBox = true };
			f.Controls.Add(new PictureBox { Dock = DockStyle.Fill });
			return f;
		}

		private byte[] peek_20(uint pc)
		{
			var b = new byte[20];
			for (uint p = 0; p < 20; p++)
				b[p] = memory.UnsafeRead8(pc + p);
			return b;
		}

		private string disassemble(uint pc, ulong end, out int n_lines, int max_lines)
		{
			n_lines = 0;
			var sb = new StringBuilder();
			do
			{
				var sz = disassembler.Disassemble(pc, peek_20(pc));
				pc += (uint)sz.Bytes.Length;
				sb.AppendLine(sz.ToString());
				n_lines++;
			} while (pc < end && (max_lines == -1 || n_lines < max_lines));

			if (n_lines == max_lines)
			{
				sb.AppendLine("...");
				n_lines++;
			}
			return sb.ToString();
		}

		private Control CreateNodeTextBox(BRANCH_NODE node, Font font, Form window, out int n_lines)
		{
			n_lines = 1;
			uint pc = node.start;

			var edit = new TextBox();
			edit.Multiline = true;
			edit.ReadOnly = true;

			edit.Text = pc_to_label(pc, 0, 0, 0) + "\r\n" + disassemble(pc, node.end, out n_lines, 4);
			edit.Width = 100;
			edit.Height = 30;
			edit.Font = font;

			string tipText = pc_to_label(pc, 0, 0, 0) + "\r\n" + disassemble(pc, node.end, out var _, -1);

			ToolTip tt = null;
			edit.MouseHover += ((object sender, EventArgs e) =>
			{
				tt = new ToolTip();
				tt.OwnerDraw = true;
				tt.Popup += (object sender, PopupEventArgs e) =>
				{ 
					e.ToolTipSize = TextRenderer.MeasureText(tt.GetToolTip(e.AssociatedControl), font);
				};
				tt.Draw += (object sender, DrawToolTipEventArgs e) =>
				{
					e.DrawBackground();
					e.DrawBorder();
					e.Graphics.DrawString(tipText, font, new SolidBrush(Color.Black), 0.0f, 0.0f);
				};
				tt.Show(tipText, edit, 0);
			});

			edit.MouseLeave += ((object sender, EventArgs e) =>
			{
				if (tt != null)
					tt.Dispose();
			});

			return edit;
		}

		private string[] tok(string s)
		{
			return s.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
					.Concat([null])
					.ToArray();
		}

		private string pc_to_label(uint pc, int _1, int _2, int _3)
		{
			return $"L{pc:X8}:";
		}

		private const float window_dpi = 96.0f;
		private float input_scale = 1.0f;
		private float output_scale = 1.0f;

		private const int MARGIN_X = 10;
		private const int MARGIN_Y = 10;

		public void GraphBranches(PC_TRACE trace)
		{
			var ss = new SemaphoreSlim(1);
			ss.Wait();
			var t = new Thread(() =>
			{
				var window = CreateBranchNodes(trace);
				window.FormClosing += (object? sender, FormClosingEventArgs e) =>
					{
						var controls = new List<Control>(window.Controls.Cast<Control>());
						window.Controls.Clear();
						var p = controls.OfType<PictureBox>().Single();
						p.Image.Dispose();
						p.Dispose();
						foreach (var v in controls)
							v.Dispose();
					};
				ss.Release();
				window.Show();
				Application.Run(window);
			});
			t.SetApartmentState(ApartmentState.STA);
			t.Start();
			ss.Wait();
		}

		private Form CreateBranchNodes(PC_TRACE trace)
		{
			logger.LogTrace("graphing...");

			output_scale = 1.0f;
			input_scale = 0.8f;

			var branchNodes = trace.nodes.OrderBy(x => x.start).ToList();

			//struct Agdesc_s strictdirected = { 1, 1, 0, 1 };
			uint strictdirected = 0b1011;
			Agraph_t g = agopen("G", strictdirected, IntPtr.Zero);

			//graph attributes
			Agsym_t bb = agattr(g, AGRAPH, "bb", "0,0,0,0");
			agattr(g, AGRAPH, "splines", "polyline");
			agattr(g, AGNODE, "shape", "box");
			//agattr(g, AGNODE, "overlap", "false");

			//node attrbiutes
			Agsym_t w = agattr(g, AGNODE, "width", "1.0");
			Agsym_t h = agattr(g, AGNODE, "height", "1.0");
			Agsym_t id = agattr(g, AGNODE, "id", "-1");
			Agsym_t pos = agattr(g, AGNODE, "pos", "0,0");

			//edge attributes
			Agsym_t epos = agattr(g, AGEDGE, "pos", "0,0");

			var nodes = new List<Agnode_t>();

			int nodeId = 0;
			foreach (var branchNode in branchNodes)
			{
				branchNode.agnode = agnode(g, $"{pc_to_label(branchNode.start, 0, 0, 0)}", TRUE);
				agxset(branchNode.agnode, id, $"{nodeId++}");
				nodes.Add(branchNode.agnode);
			}

			var font = new Font("Consolas", 6.0f, FontStyle.Regular, GraphicsUnit.Point);

			var size = new Size();
			var window = CreateGraphWindow(trace.Start, font);
			window.Show();
			window.Invoke(() =>
			{
				var gf = Graphics.FromHwnd(window.Handle);
				string test = "01234567801234567801234678012345678901234\r\n0\r\n";
				var dim = gf.MeasureString(test, window.Font);
				size.Width = (int)dim.Width;
				size.Height = (int)(dim.Height / 2.0f);
				gf.Dispose();
			});

			logger.LogTrace("creating nodes and edges...");

			//width and height are in inches (72 points), everything else is in points
			//node position is the middle of the box
			//need to convert points to device pixels (72 points is usually 96dp)

			var edges = new List<Agedge_t>();
			var textBoxes = new List<Control>();
			foreach (var branchNode in branchNodes)
			{
				int n_lines;
				var textBox = CreateNodeTextBox(branchNode, font, window, out n_lines);
				textBoxes.Add(textBox);
				window.Controls.Add(textBox);

				double sx = devToIns(size.Width);
				double sy = devToIns(size.Height * n_lines);

				agxset(branchNode.agnode, w, $"{sx:F4}");
				agxset(branchNode.agnode, h, $"{sy:F4}");

				if (branchNode.nottaken != null)
					edges.Add(agedge(g, branchNode.agnode, branchNode.nottaken.agnode, "", TRUE));
				if (branchNode.taken != null)
					edges.Add(agedge(g, branchNode.agnode, branchNode.taken.agnode, "", TRUE));
			}

			logger.LogTrace("layout...");

			GVC_t gvc = gvContext();

			string[] algs = ["dot", "neato", "fdp", "sfdp", "twopi", "circo", "patchwork", "osage"];
			const int alg_no = 0;
			int err = gvLayout(gvc, g, algs[alg_no]);
			Debug.Assert(err == 0);

			//debug
			/*
			err = gvRenderFilename(gvc, g, "dot", $"gv{sorter[0].start:X8}.txt");
			err = gvRenderFilename(gvc, g, "png", $"gv{sorter[0].start:X8}.png");

			IntPtr graph;
			uint length;
			err = gvRenderData(gvc, g, "dot", out graph, out length);
			if (graph != IntPtr.Zero)
			{
				logger.LogTrace(Marshal.PtrToStringAnsi(graph));
				gvFreeRenderData(graph);
			}
			*/
			// end debug

			//if we don't render anything, need to run this to populate all the graph data
			attach_attrs(g);

			gvFreeLayout(gvc, g);

			logger.LogTrace("rendering...\n");

			string[] bbox = tok(Marshal.PtrToStringAnsi(agxget(g, bb)));
			int boxw = ptsToDev(bbox[2]);
			int boxh = ptsToDev(bbox[3]);

			//scale to 1024x1024 max
			output_scale = MathF.Min(1.0f, 1024.0f / MathF.Max(boxw, boxh));
			logger.LogTrace($"output scale {output_scale}\n");
			boxw = (int)((float)boxw * output_scale);
			boxh = (int)((float)boxh * output_scale);

			int fboxh = boxh;

			boxw += 2 * MARGIN_X;
			boxh += 2 * MARGIN_Y;

			window.ClientSize = new Size(boxw, boxh);
			window.MaximumSize = window.Size;

			var bitmap = new Bitmap(boxw, boxh, PixelFormat.Format32bppRgb);
			var gfx = Graphics.FromImage(bitmap);
			
			var pen = new Pen(Color.Black);
			var brush = new SolidBrush(Color.DarkRed);
			gfx.FillRectangle(new SolidBrush(Color.White), 0, 0, bitmap.Width, bitmap.Height);

			gfx.TranslateTransform(MARGIN_X, MARGIN_Y);

			foreach ((var branchNode, var textBox) in branchNodes.Zip(textBoxes))
			{
				Agnode_t n = branchNode.agnode;
				int width = insToDev(Marshal.PtrToStringAnsi(agxget(n, w)));
				int height = insToDev(Marshal.PtrToStringAnsi(agxget(n, h)));

				string[] ppos = tok(Marshal.PtrToStringAnsi(agxget(n, pos)));
				int x = ptsToDev(ppos[0]);
				int y = ptsToDev(ppos[1]);

				x -= width / 2;
				y -= height / 2;

				gfx.FillRectangle(brush, x, y, width, height);

				textBox.ClientSize = new Size(width, height);
				textBox.Location = new Point(x+MARGIN_X, y+MARGIN_Y);
			}

			foreach (var edge in edges)
			{
				string[] pp = tok(Marshal.PtrToStringAnsi(agxget(edge, epos)));
				int x, y;
				if (pp[3] == null) continue;

				x = ptsToDev(pp[3]);
				y = ptsToDev(pp[4]);
				var currPos = new Point(x, y);
				int j = 5;
				while (pp[j] != null)
				{
					x = ptsToDev(pp[j++]);
					y = ptsToDev(pp[j++]);
					var nextPos = new Point(x, y);
					gfx.DrawLine(pen, currPos, nextPos);
					currPos = nextPos;
				}
				int ex = ptsToDev(pp[1]);
				int ey = ptsToDev(pp[2]);

				var endPos = new PointF(ex, ey);
				gfx.DrawLine(pen, currPos, endPos);

				//arrowhead
				float dx = (float)(ex - x);
				float dy = (float)(ey - y);
				float a = MathF.Atan2(dy, dx);
				float arrowlen = MathF.Max(2.0f, 7.0f * output_scale);
				float ax, ay;

				ax = ex + arrowlen * MathF.Cos(a + MathF.PI * 1.1f);
				ay = ey + arrowlen * MathF.Sin(a + MathF.PI * 1.1f);
				gfx.DrawLine(pen, endPos.X, endPos.Y, ax, ay);

				ax = ex + arrowlen * MathF.Cos(a - MathF.PI * 1.1f);
				ay = ey + arrowlen * MathF.Sin(a - MathF.PI * 1.1f);
				gfx.DrawLine(pen, endPos.X, endPos.Y, ax, ay);
			}

			var picture = window.Controls.OfType<PictureBox>().Single();
			picture.Image = bitmap;
			picture.Refresh();
			picture.SendToBack();

			gfx.Dispose();

			gvFreeContext(gvc);

			agclose(g);

			logger.LogTrace("done\n");

			return window;
		}
	}
}
