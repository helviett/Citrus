﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lime.PopupMenu
{
	public class Menu : ICollection<MenuItem>
	{
		RoundedRectangle rectangle = new RoundedRectangle();

		public event Action Hidden;
		public Frame Frame = new Frame() { Tag = "$PopupMenu.cs" };
		private List<MenuItem> items = new List<MenuItem>();
		private int layer;
		private int maxHeight;
		private int itemWidth;

		public Menu(int layer = Widget.MaxLayer, int itemWidth = 350, int maxHeight = 768)
		{
			this.layer = layer;
			this.maxHeight = maxHeight;
			this.itemWidth = itemWidth;
			Frame.AddNode(rectangle);
			Frame.Updating += Frame_Updating;
		}

		public void Show()
		{
			World.Instance.PushNode(Frame);
			Frame.CenterOnParent();
			Frame.Layer = layer;
			Frame.Input.CaptureAll();
		}

		public void Hide()
		{
			Frame.Unlink();
			if (Hidden != null) {
				Hidden();
			}
		}

		void Frame_Updating(float delta)
		{
			if (Input.WasMousePressed()) {
				if (!Frame.HitTest(Input.MousePosition)) {
					Hide();
				}
			}
			float totalItemsHeigth = MenuItem.Height * Count;
			int columnsCount = Math.Max((totalItemsHeigth / maxHeight).Ceiling(), 1);
			int itemsPerColumn = ((float)Count / columnsCount).Ceiling();
			Frame.Height = MenuItem.Height * itemsPerColumn + MenuItem.Height;
			Frame.Width = itemWidth * columnsCount;
			UpdateBackground();
			UpdateItems(itemsPerColumn);
		}

		private void UpdateItems(int itemsPerColumn)
		{
			int i = 0;
			foreach (var item in items) {
				int column = i / itemsPerColumn;
				int row = i % itemsPerColumn;
				item.Frame.X = column * itemWidth;
				item.Frame.Y = row * MenuItem.Height + MenuItem.Height / 2;
				item.Frame.Width = itemWidth;
				item.Frame.Height = MenuItem.Height;
				i++;
			}
		}

		private void UpdateBackground()
		{
			rectangle.CornerRadius = MenuItem.Height / 3;
			rectangle.Size = Frame.Size;
		}

		#region ICollection<MenuItem>

		public void Add(MenuItem item)
		{
			item.Menu = this;
			items.Add(item);
			Frame.Nodes.Push(item.Frame);
		}

		public void Clear()
		{
			foreach (var item in items.ToArray()) {
				Remove(item);
			}
		}

		public bool Contains(MenuItem item)
		{
			return items.Contains(item);
		}

		public void CopyTo(MenuItem[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}

		public bool Remove(MenuItem item)
		{
			Frame.Nodes.Remove(item.Frame);
			return items.Remove(item);
		}

		public IEnumerator<MenuItem> GetEnumerator()
		{
			return items.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return items.GetEnumerator();
		}

		public int Count { get { return items.Count; } }

		public bool IsReadOnly { get { return false; } }
		#endregion
	}
}
