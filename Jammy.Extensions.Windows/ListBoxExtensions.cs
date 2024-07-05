/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Extensions.Windows
{
	public static class ListBoxExtensions
	{
		public static void SizeListBox(this ListBox listBox, int columns)
		{
			if (listBox.Items.Count == 0) return;

			using (var g = listBox.CreateGraphics())
			{
				g.PageUnit = GraphicsUnit.Pixel;
				int paddingX = listBox.Height - listBox.ClientSize.Height;
				int paddingY = listBox.Width - listBox.ClientSize.Width;
				int itemsPerColumn = (listBox.Items.Count + columns - 1) / columns;
				var sizes = listBox.Items.Cast<string>().Select(x => g.MeasureString(x, listBox.Font)).ToList();
				float w = sizes.Max(x => x.Width);
				//float h = sizes.Take(itemsPerColumn).Sum(x => x.Height);
				listBox.ColumnWidth = (int)Math.Ceiling(w);
				listBox.Width = paddingX + listBox.ColumnWidth * columns;
				listBox.Height = (int)(sizes[0].Height * itemsPerColumn);
			}
		}
	}
}
