using Jammy.NativeOverlay.Overlays;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.NativeOverlay
{
	public interface IOverlayCollection
	{
		void Render();
		void Add(IOverlayRenderer renderer);
		void Remove(IOverlayRenderer renderer);
	}

	public class OverlayCollection : IOverlayCollection
	{
		private readonly List<IOverlayRenderer> overlays = new List<IOverlayRenderer>();

		public OverlayCollection(IEnumerable<IOverlayRenderer> overlays)
		{
			this.overlays.AddRange(overlays);
		}

		public void Render()
		{
			foreach (var overlay in overlays)
				overlay.Render();
		}

		public void Add(IOverlayRenderer renderer)
		{
			overlays.Add(renderer);
		}

		public void Remove(IOverlayRenderer renderer)
		{
			overlays.Remove(renderer);
		}
	}
}
