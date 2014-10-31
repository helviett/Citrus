﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lime
{
	public interface ICaretParams
	{
		Widget CaretWidget { get; set; }
		float BlinkInterval { get; set; }
	}

	public class CaretParams : ICaretParams
	{
		public Widget CaretWidget { get; set; }
		public float BlinkInterval { get; set; }

		public CaretParams()
		{
			BlinkInterval = 0.5f;
		}
	}

	public class VertiсalLineCaret : Polyline
	{
		public VertiсalLineCaret(SimpleText text) : base(2.0f)
		{
			Points.Add(Vector2.Zero);
			Points.Add(Vector2.Down * text.FontHeight);
			Color = text.Color;
		}
	}

	public class CaretDisplay
	{
		private Widget container;
		private ICaretPosition caretPos;
		private ICaretParams caretParams;

		public CaretDisplay(Widget container, ICaretPosition caretPos, ICaretParams caretParams)
		{
			this.container = container;
			this.caretPos = caretPos;
			this.caretParams = caretParams;
			container.AddNode(caretParams.CaretWidget);
			container.Tasks.Add(CaretDisplayTask());
		}

		private IEnumerator<object> CaretDisplayTask()
		{
			var w = caretParams.CaretWidget;
			var time = 0f;
			bool blinkOn = true;
			while (true) {
				time += container.Tasks.Delta;
				if (time > caretParams.BlinkInterval && caretParams.BlinkInterval > 0f) {
					blinkOn = !blinkOn;
					time = 0f;
				}
				var newPos = caretPos.GetWorldPosition();
				if (!w.Position.Equals(newPos)) {
					w.Position = newPos;
					blinkOn = true;
					time = 0f;
				}
				w.Visible = caretPos.IsVisible && blinkOn;
				yield return 0;
			}
		}
	}

	public interface IEditorParams
	{
		float KeyPressInterval { get; set; }
	}

	public class EditorParams : IEditorParams
	{
		public float KeyPressInterval { get; set; }

		public EditorParams()
		{
			KeyPressInterval = 0.05f;
		}
	}

	public class Editor
	{
		private Widget container;
		private IKeyboardInputProcessor textInputProcessor;
		private IText text;
		private ICaretPosition caretPos;
		private IEditorParams editorParams;

		public Editor(Widget container, ICaretPosition caretPos, IEditorParams editorParams)
		{
			this.container = container;
			this.textInputProcessor = (IKeyboardInputProcessor)container;
			text = (IText)container;
			this.caretPos = caretPos;
			this.editorParams = editorParams;
			container.Tasks.Add(FocusTask());
			container.Tasks.Add(HandleKeyboardTask());
		}

		private IEnumerator<object> FocusTask()
		{
			var world = World.Instance;
			while (true) {
				if (Input.WasMouseReleased() && container.IsMouseOver()) {
					world.ActiveTextWidget = textInputProcessor;
					world.IsActiveTextWidgetUpdated = true;
				}
				caretPos.IsVisible = world.ActiveTextWidget == textInputProcessor;
				if (caretPos.IsVisible)
					world.IsActiveTextWidgetUpdated = true;
				yield return 0;
			}
		}

		private void HandleCursorKeys()
		{
			if (Input.IsKeyPressed(Key.Left))
				caretPos.Pos--;
			if (Input.IsKeyPressed(Key.Right))
				caretPos.Pos++;
			if (Input.IsKeyPressed(Key.Up))
				caretPos.Line--;
			if (Input.IsKeyPressed(Key.Down))
				caretPos.Line++;
		}

		private IEnumerator<object> HandleKeyboardTask()
		{
			var world = World.Instance;
			while (true) {
				if (caretPos.IsVisible) {
					HandleCursorKeys();
					yield return editorParams.KeyPressInterval;
				}
				else {
					yield return 0;
				}
			}
		}
	}
}
