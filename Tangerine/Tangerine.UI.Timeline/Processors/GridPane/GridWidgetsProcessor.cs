﻿using System;
using System.Collections.Generic;
using Tangerine.Core;

namespace Tangerine.UI.Timeline
{
	public class GridWidgetsProcessor : Core.IProcessor
	{
		Timeline timeline => Timeline.Instance;

		public IEnumerator<object> Loop()
		{
			while (true) {
				if (!AreWidgetsValid()) {
					ResetWidgets();
				}
				AdjustWidths();
				yield return null;
			}
		}

		void AdjustWidths()
		{
			foreach (var row in Document.Current.Rows) {
				var gw = row.Components.Get<Components.IGridWidget>();
				gw.Widget.MinWidth = Timeline.Instance.ColumnCount * TimelineMetrics.ColWidth;
			}
		}

		void ResetWidgets()
		{
			var content = timeline.Grid.ContentWidget;
			content.Nodes.Clear();
			foreach (var row in Document.Current.Rows) {
				content.AddNode(row.Components.Get<Components.IGridWidget>().Widget);
			}
		}

		bool AreWidgetsValid()
		{
			var content = timeline.Grid.ContentWidget;
			if (Document.Current.Rows.Count != content.Nodes.Count) {
				return false;
			}
			foreach (var row in Document.Current.Rows) {
				if (row.Components.Get<Components.IGridWidget>().Widget != content.Nodes[row.Index]) {
					return false;
				}
			}
			return true;
		}
	}
}

