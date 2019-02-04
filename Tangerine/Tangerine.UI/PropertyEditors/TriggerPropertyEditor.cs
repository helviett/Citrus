using System.Collections.Generic;
using System.Linq;
using Lime;
using Tangerine.Core;

namespace Tangerine.UI
{
	public class TriggerPropertyEditor : CommonPropertyEditor<string>
	{
		private ComboBox comboBox;
		public delegate void FillValuesDelegate(IEnumerable<object> objects, ComboBox comboBox);

		public TriggerPropertyEditor(IPropertyEditorParams editorParams) : base(editorParams)
		{
			comboBox = new ThemedComboBox { LayoutCell = new LayoutCell(Alignment.Center) };
			EditorContainer.AddNode(comboBox);
			EditorContainer.AddNode(Spacer.HStretch());
			comboBox.Changed += ComboBox_Changed;
			Invalidate();
			comboBox.AddChangeWatcher(CoalescedPropertyValue(), v => comboBox.Text = v.IsDefined ? v.Value : ManyValuesText);
		}


		public void Invalidate()
		{
			comboBox.Items.Clear();
			foreach (var obj in EditorParams.Objects) {
				var node = (Node)obj;
				foreach (var a in node.Animations) {
					foreach (var m in a.Markers.Where(i => i.Action != MarkerAction.Jump && !string.IsNullOrEmpty(i.Id))) {
						var id = a.Id != null ? m.Id + '@' + a.Id : m.Id;
						if (!comboBox.Items.Any(i => i.Text == id)) {
							comboBox.Items.Add(new DropDownList.Item(id));
						}
					}
				}
			}
		}

		void ComboBox_Changed(DropDownList.ChangedEventArgs args)
		{
			if (!args.ChangedByUser)
				return;
			var newTrigger = (string)args.Value;
			var currentTriggers = CoalescedPropertyValue().GetValue();
			if (string.IsNullOrWhiteSpace(currentTriggers.Value) || args.Index < 0) {
				// Keep existing and remove absent triggers after hand input.
				var availableTriggers = new HashSet<string>(comboBox.Items.Select(item => item.Text));
				var setTrigger = string.Join(
					",",
					newTrigger.
						Split(',').
						Select(el => el.Trim()).
						Where(el => availableTriggers.Contains(el)).
						Distinct(new TriggerStringComparer())
				);
				if (setTrigger.Length == 0) {
					comboBox.Text = currentTriggers.IsDefined ? currentTriggers.Value : ManyValuesText;
					return;
				}
				SetProperty(setTrigger);
				if (setTrigger != newTrigger) {
					comboBox.Text = setTrigger;
				}
				return;
			}
			var triggers = new List<string>();
			var added = false;
			SplitTrigger(newTrigger, out _, out var newAnimation);
			foreach (var trigger in currentTriggers.Value.Split(',').Select(i => i.Trim())) {
				SplitTrigger(trigger, out _, out var animation);
				if (animation == newAnimation) {
					if (!added) {
						added = true;
						triggers.Add(newTrigger);
					}
				} else {
					triggers.Add(trigger);
				}
			}
			if (!added) {
				triggers.Add(newTrigger);
			}
			var newValue = string.Join(",", triggers);
			SetProperty(newValue);
			comboBox.Text = newValue;
		}

		protected static void SplitTrigger(string trigger, out string markerId, out string animationId)
		{
			if (!trigger.Contains('@')) {
				markerId = trigger;
				animationId = null;
			} else {
				var t = trigger.Split('@');
				markerId = t[0];
				animationId = t[1];
			}
		}

		private class TriggerStringComparer : IEqualityComparer<string>
		{
			public bool Equals(string x, string y)
			{
				SplitTrigger(x, out _, out var xAnimation);
				SplitTrigger(y, out _, out var yAnimation);
				return xAnimation == yAnimation;
			}

			public int GetHashCode(string obj)
			{
				SplitTrigger(obj, out _, out var animation);
				return animation == null ? 0 : animation.GetHashCode();
			}
		}
	}
}
