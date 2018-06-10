﻿using System;
using System.Linq;
using System.Collections.Generic;
using Lime;
using Tangerine.Core;
using Tangerine.UI.SceneView.WidgetTransforms;

namespace Tangerine.UI.SceneView
{
	public class ResizeWidgetsProcessor : ITaskProvider
	{
		SceneView sv => SceneView.Instance;

		public IEnumerator<object> Task()
		{
			while (true) {
				Quadrangle hull;
				Vector2 pivot;
				var widgets = Document.Current.SelectedNodes().Editable().OfType<Widget>();
				if (Utils.CalcHullAndPivot(widgets, sv.Scene, out hull, out pivot)) {
					for (int i = 0; i < 4; i++) {
						var a = hull[i];
						if (sv.HitTestResizeControlPoint(a)) {
							Utils.ChangeCursorIfDefault(MouseCursor.SizeNS);
							if (sv.Input.ConsumeKeyPress(Key.Mouse0)) {
								yield return Resize(hull, i * 2, pivot);
							}
						}
						var b = hull[(i + 1) % 4];
						if (sv.HitTestResizeControlPoint((a + b) / 2)) {
							var cursor = (b.X - a.X).Abs() > (b.Y - a.Y).Abs() ? MouseCursor.SizeNS : MouseCursor.SizeWE;
							Utils.ChangeCursorIfDefault(cursor);
							if (sv.Input.ConsumeKeyPress(Key.Mouse0)) {
								yield return Resize(hull, i * 2 + 1, pivot);
							}
						}
					}
				}
				yield return null;
			}
		}

		IEnumerator<object> Resize(Quadrangle hull, int controlPointIndex, Vector2 pivot)
		{
			var cursor = WidgetContext.Current.MouseCursor;
			using (Document.Current.History.BeginTransaction()) {
				var widgets = Document.Current.SelectedNodes().Editable().OfType<Widget>().ToList();
				var mouseStartPos = sv.MousePosition;

				while (sv.Input.IsMousePressed()) {
					Document.Current.History.RollbackTransaction();

					Utils.ChangeCursorIfDefault(cursor);
					var proportional = sv.Input.IsKeyPressed(Key.Shift);

					RescaleWidgets(widgets.Count <= 1,
						sv.Input.IsKeyPressed(Key.Control)
							? (widgets.Count <= 1 ? (Vector2?) null : pivot)
							: hull[LookupPivotIndex[controlPointIndex] / 2],
						widgets,
						controlPointIndex,
						sv.MousePosition,
						mouseStartPos,
						proportional,
						!sv.Input.IsKeyPressed(Key.Control)
					);

					yield return null;
				}
				sv.Input.ConsumeKey(Key.Mouse0);
				Document.Current.History.CommitTransaction();
			}
		}

		private static readonly int[] LookupPivotIndex = {
			4, 5, 6, 7, 0, 1, 2, 3
		};

		private static readonly bool[][] LookupInvolvedAxes = {
			new[] {true, true},
			new[] {false, true},
			new[] {true, true},
			new[] {true, false},
			new[] {true, true},
			new[] {false, true},
			new[] {true, true},
			new[] {true, false},
		};

		void RescaleWidgets(bool hullInFirstWidgetSpace, Vector2? pivotPoint, List<Widget> widgets, int controlPointIndex,
			Vector2 curMousePos, Vector2 prevMousePos, bool proportional, bool convertScaleToSize)
		{
			WidgetTransformsHelper.ApplyTransformationToWidgetsGroupObb(
				sv.Scene,
				widgets, pivotPoint, hullInFirstWidgetSpace, curMousePos, prevMousePos,
				convertScaleToSize,
				(originalVectorInObbSpace, deformedVectorInObbSpace) => {
					Vector2d deformationScaleInObbSpace =
						new Vector2d(
							Math.Abs(originalVectorInObbSpace.X) < Mathf.ZeroTolerance
								? 1
								: deformedVectorInObbSpace.X / originalVectorInObbSpace.X,
							Math.Abs(originalVectorInObbSpace.Y) < Mathf.ZeroTolerance
								? 1
								: deformedVectorInObbSpace.Y / originalVectorInObbSpace.Y
						);

					if (!LookupInvolvedAxes[controlPointIndex][0]) {
						deformationScaleInObbSpace.X = proportional ? deformationScaleInObbSpace.Y : 1;
					} else if (!LookupInvolvedAxes[controlPointIndex][1]) {
						deformationScaleInObbSpace.Y = proportional ? deformationScaleInObbSpace.X : 1;
					} else if (proportional) {
						deformationScaleInObbSpace.X = (deformationScaleInObbSpace.X + deformationScaleInObbSpace.Y) / 2;
						deformationScaleInObbSpace.Y = deformationScaleInObbSpace.X;
					}

					return new Transform2d(Vector2d.Zero, deformationScaleInObbSpace, 0); 
				}
			);
		}

	}
}
