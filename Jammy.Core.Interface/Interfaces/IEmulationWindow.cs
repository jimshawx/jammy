using Jammy.Core.Types.Types;
using System;

namespace Jammy.Core.Interface.Interfaces
{
	public interface IEmulationWindow : IInputOutput
	{
		void SetPicture(int screenWidth, int screenHeight);
		void Blit(int[] screen);
		void SetKeyHandlers(Action<int> addKeyDown, Action<int> addKeyUp);
		bool IsActive();
		int[] GetFramebuffer();
	}
}
