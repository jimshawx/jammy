using Jammy.Core.Types.Types;
using System;

namespace Jammy.Core.Interface.Interfaces
{
	public interface IEmulationWindow
	{
		bool IsCaptured { get; }
		void SetPicture(int screenWidth, int screenHeight);
		void Blit(int[] screen);
		Point RecentreMouse();
		void SetKeyHandlers(Action<int> addKeyDown, Action<int> addKeyUp);
		bool IsActive();
	}
}
