using ImGuiNET;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using System;
using System.Runtime.InteropServices;
using System.Threading;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Plugins.Renderer
{
	public interface IPluginRenderer
	{
		void Render(SKCanvas canvas, ImDrawDataPtr drawData);
		IDisposable Lock();
	}

	public class ImGuiSkiaRenderer : IPluginRenderer, IDisposable
	{
		private readonly SKImage fontTexture;
		private readonly GCHandle handle;
		private readonly SKShader fontImage;
		private readonly IntPtr imguiContext;

		public ImGuiSkiaRenderer(float scale, ILogger logger)
		{
			imguiContext = ImGui.CreateContext();
			using var imgui = Lock();

			ImGui.StyleColorsLight();
			var io = ImGui.GetIO();
			io.Fonts.AddFontDefault();
			io.Fonts.Build();
			io.Fonts.GetTexDataAsAlpha8(out IntPtr pixels, out int width, out int height, out _);
			io.FontGlobalScale = scale;

			if (pixels == IntPtr.Zero || width <= 0 || height <= 0)
				throw new NotImplementedException("ImGui doesn't support a font on this platform");

			int rowBytes = width ;
			int length = rowBytes * height;

			// Copy ImGui alpha into managed buffer (orig)
			var orig = new byte[length];
			Marshal.Copy(pixels, orig, 0, length);

			byte[] conv = orig;

			//pin converted buffer and create SKBitmap -> SKImage (raster-backed)
			handle = GCHandle.Alloc(conv, GCHandleType.Pinned);
			IntPtr ptr = handle.AddrOfPinnedObject();
			var info = new SKImageInfo(width, height, SKColorType.Alpha8, SKAlphaType.Unpremul);

			fontTexture = SKImage.FromPixels(info, ptr, rowBytes);

			io.Fonts.SetTexID((IntPtr)0x12341);

			var localMatrix = SKMatrix.CreateScale(1.0f / width, 1.0f / height);
			fontImage = SKShader.CreateImage(
							fontTexture,
							SKShaderTileMode.Clamp,
							SKShaderTileMode.Clamp,
							localMatrix);
		}

		public void Dispose()
		{
			using var imgui = Lock();

			fontImage?.Dispose();
			fontTexture?.Dispose();
			if (handle.IsAllocated)
				handle.Free();
			if (imguiContext != IntPtr.Zero)
				ImGui.DestroyContext(imguiContext);
		}

		private static readonly SemaphoreSlim renderSemaphore = new SemaphoreSlim(1, 1);
		public IDisposable Lock()
		{
			var locked = new RenderLock(renderSemaphore);
			ImGui.SetCurrentContext(imguiContext);
			return locked;
		}

		public void Render(SKCanvas canvas, ImDrawDataPtr drawData)
		{
			if (drawData.CmdListsCount == 0)
				return;

			canvas.Save();

			var scale = drawData.FramebufferScale;
			canvas.Scale(scale.X, scale.Y);
			canvas.Translate(-drawData.DisplayPos.X, -drawData.DisplayPos.Y);

			for (int n = 0; n < drawData.CmdListsCount; n++)
			{
				ImDrawListPtr cmdList = drawData.CmdLists[n];

				for (int cmd_i = 0; cmd_i < cmdList.CmdBuffer.Size; cmd_i++)
				{
					ImDrawCmdPtr cmd = cmdList.CmdBuffer[cmd_i];

					// Clip rect
					var clip = SKRect.Create(
						cmd.ClipRect.X,
						cmd.ClipRect.Y,
						cmd.ClipRect.Z - cmd.ClipRect.X,
						cmd.ClipRect.W - cmd.ClipRect.Y
					);
					//var debug = new SKPaint { Color = SKColors.Red, Style = SKPaintStyle.Stroke, StrokeWidth = 3 };
					//canvas.DrawRect(clip, debug);

					canvas.Save();
					canvas.ClipRect(clip);

					// Pick texture
					SKImage img = null;
					if (cmd.TextureId == (IntPtr)0x12341)
						img = fontTexture;

					// Build shader
					var paint = new SKPaint();
					paint.Color = SKColors.White;
					if (img != null)
					{
						paint.Shader = fontImage;
					}
					else
					{
						paint.Shader = null;
					}

					int elemCount = (int)cmd.ElemCount;
					var positions = new SKPoint[elemCount];
					var texcoords = new SKPoint[elemCount];
					var colors = new SKColor[elemCount];
					var indices = new ushort[elemCount];

					var vtxBuffer = cmdList.VtxBuffer;
					var idxBuffer = cmdList.IdxBuffer;

					for (int i = 0; i < elemCount; i++)
					{
						int idx = idxBuffer[(int)cmd.IdxOffset + i];
						var v = vtxBuffer[idx];

						positions[i] = new SKPoint(v.pos.X, v.pos.Y);

						if (img != null)
						{
							texcoords[i] = new SKPoint(
								v.uv.X,
								v.uv.Y);

							//if these don't get jittered, some renderers optimize away the triangle
							//when all three uvs are identical (Skia, Windows)
							if ((i % 3) == 0) texcoords[i].X += 0.0001f;
							if ((i % 3) == 1) texcoords[i].Y += 0.0001f;
						}
						else
						{
							texcoords[i] = new SKPoint(0, 0);
						}

						uint c = v.col;
						byte r = (byte)((c >> 0) & 0xFF);
						byte g = (byte)((c >> 8) & 0xFF);
						byte b = (byte)((c >> 16) & 0xFF);
						byte a = (byte)((c >> 24) & 0xFF);
						colors[i] = new SKColor(r, g, b, a);

						indices[i] = (ushort)i;
					}

					var verts = SKVertices.CreateCopy(
						SKVertexMode.Triangles,
						positions,
						texcoords,
						colors,
						indices
					);

					canvas.DrawVertices(verts, SKBlendMode.Modulate, paint);

					canvas.Restore();
				}
			}

			canvas.Restore();
		}
	}

	public sealed class RenderLock : IDisposable
	{
		private readonly SemaphoreSlim sem;
		
		internal RenderLock(SemaphoreSlim sem)
		{
			this.sem = sem;
			sem.Wait();
		}

		public void Dispose() { sem.Release(); }
	}

	/*
			// 1. Load the SkSL shader
	string sksl = @"
	uniform shader tex;

	half4 main(float2 uv, half4 color) {
		return tex.eval(uv) * color;
	}
	";

	// 2. Compile the shader
	var effect = SKRuntimeEffect.Create(sksl, out var errorText);
	if (effect == null)
		throw new Exception("Shader compile error: " + errorText);

	// 3. Create the shader with your texture
	SKShader shader = effect.ToShader(new SKRuntimeEffectUniforms(),
		new SKRuntimeEffectChildren
		{
			["tex"] = yourTextureShader  // usually SKShader.CreateBitmap(...)
		});

	// 4. Use it in a paint
	using var paint = new SKPaint
	{
		Shader = shader,
		IsAntialias = false,
		FilterQuality = SKFilterQuality.None
	};

	// 5. Draw your ImGui mesh
	canvas.DrawVertices(vertices, SKBlendMode.SrcOver, paint);
	*/

}