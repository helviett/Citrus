﻿using System;
using System.Linq;
using Lime;
using Tangerine.Core;
using System.Collections.Generic;

namespace Tangerine.UI.Timeline
{
	public static class RowExtensions
	{
		public static Components.IGridWidget GetGridWidget(this Row row) => row.Components.Get<Components.IGridWidget>();
	}

	public class Timeline : IDocumentView
	{
		public static Timeline Instance { get; private set; }
			
		public readonly Toolbar Toolbar = new Toolbar();
		public readonly Rulerbar Ruler = new Rulerbar();
		public readonly OverviewPane Overview = new OverviewPane();
		public readonly GridPane Grid = new GridPane();
		public readonly RollPane Roll = new RollPane();
		public readonly Widget PanelWidget;
		public readonly Widget RootWidget = new Widget();

		public Vector2 ScrollPos;
		public Node Container
		{
			get { return Document.Current.Container; }
			set { Document.Current.Container = value; }
		}
		public int CurrentColumn
		{
			get { return Document.Current.AnimationFrame; }
			set
			{
				if (UserPreferences.Instance.AnimationMode && Document.Current.AnimationFrame != value) {
					SetCurrentFrameRecursive(Document.Current.Container, value);
				} else {
					Document.Current.AnimationFrame = value; 
				}
			}
		}
		public int ColumnCount { get; set; }
		public GridSelection GridSelection = new GridSelection();
		public readonly Entity Globals = new Entity();

		public Timeline(Widget panelWidget)
		{
			PanelWidget = panelWidget;
			CreateProcessors();
			InitializeWidgets();
		}

		public void Attach()
		{
			Instance = this;
			PanelWidget.PushNode(RootWidget);
			RootWidget.SetFocus();
		}

		public void Detach()
		{
			Instance = null;
			RootWidget.Unlink();
		}

		void InitializeWidgets()
		{
			RootWidget.Layout = new StackLayout();
			RootWidget.AddNode(new VSplitter {
				Nodes = {
					Overview.RootWidget,
					new HSplitter {
						Nodes = {
							new Widget {
								Layout = new VBoxLayout(),
								LayoutCell = new LayoutCell { StretchX = 0.33f },
								Nodes = {
									Toolbar.RootWidget,
									Roll.RootWidget,
								}
							},
							new Widget {
								Layout = new VBoxLayout(),
								Nodes = {
									Ruler.RootWidget,
									Grid.RootWidget,
								}
							},
						}
					}
				}
			});
		}

		void CreateProcessors()
		{
			RootWidget.LateTasks.Add(new IProcessor[] {
				new UnselectUnlinkedNodesProcessor(),
				new ColumnCountProcessor(),
				new BuildRowViewsProcessor(),
				new RollWidgetsProcessor(),
				new GridWidgetsProcessor(),
				new OverviewWidgetsProcessor(),
				new OverviewScrollProcessor(),
				new MouseWheelProcessor(),
				new ResizeGridCurveViewProcessor(),
				new GridMouseScrollProcessor(),
				new RollMouseScrollProcessor(),
				new SelectAndDragKeyframesProcessor(),
				new HasKeyframeRespondentProcessor(),
				new DragKeyframesRespondentProcessor(),
				new SelectAndDragRowsProcessor(),
				new RulerMouseScrollProcessor(),
				new ClampScrollPosProcessor(),
				new ClampScrollPosOnContainerChange().GetProcessor(),
				new EditMarkerProcessor(),
				EnsureCurrentColumnVisibleOnContainerChange(),
				new SelectFirstNodeOnDocumentOpeningProcessor()
			});
		}

		class SelectFirstNodeOnDocumentOpeningProcessor : IProcessor
		{
			public IEnumerator<object> Loop()
			{
				if (Document.Current.Container.Nodes.Count > 0) {
					Core.Operations.SelectNode.Perform(Document.Current.Container.Nodes[0]);
				}
				yield return null;
			}
		}

		IProcessor EnsureCurrentColumnVisibleOnContainerChange()
		{
			return new Property<Node>(() => Document.Current.Container).
				DistinctUntilChanged().Consume(_ => EnsureColumnVisible(Document.Current.AnimationFrame));
		}

		void SetCurrentFrameRecursive(Node node, int frame)
		{
			node.AnimationFrame = frame;
			foreach (var child in node.Nodes) {
				SetCurrentFrameRecursive(child, frame);
			}
		}

		public void EnsureColumnVisible(int column)
		{
			if ((column + 1) * TimelineMetrics.ColWidth - ScrollPos.X >= Grid.RootWidget.Width) {
				ScrollPos.X = (column + 1) * TimelineMetrics.ColWidth - Grid.RootWidget.Width;
			}
			if (column * TimelineMetrics.ColWidth < ScrollPos.X) {
				ScrollPos.X = Math.Max(0, column * TimelineMetrics.ColWidth);
			}
		}

		public void EnsureRowVisible(Row row)
		{
			var gw = row.GetGridWidget();
			if (gw.Bottom > ScrollPos.Y + Grid.Size.Y) {
				ScrollPos.Y = gw.Bottom - Grid.Size.Y;
			}
			if (gw.Top < ScrollPos.Y) {
				ScrollPos.Y = Math.Max(0, gw.Top);
			}
		}

		public bool IsColumnVisible(int col)
		{
			var pos = col * TimelineMetrics.ColWidth - ScrollPos.X;
			return pos >= 0 && pos < Grid.Size.X;
		}
		
		public bool IsRowVisible(int row)
		{
			var pos = Document.Current.Rows[row].GetGridWidget().Top - ScrollPos.Y;
			return pos >= 0 && pos < Grid.Size.Y;
		}
	}
}