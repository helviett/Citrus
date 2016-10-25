﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Lime;

namespace Tangerine.Core
{
	public interface IDocumentView
	{
		void Detach();
		void Attach();
	}

	/// <summary>
	/// Document updater maintains a document in the consistent state. Update() is being invoked after every executed operation.
	/// </summary>
	public interface IDocumentUpdater
	{
		void Update();
	}

	public sealed class Document
	{
		public enum CloseAction
		{
			Cancel,
			SaveChanges,
			DiscardChanges
		}

		private readonly Dictionary<Uid, Row> RowCache = new Dictionary<Uid, Row>();

		public static event Action<Document> AttachingViews;
		public static event Func<Document, CloseAction> Closing;

		public const string SceneFileExtension = "scene";

		public static Document Current { get; private set; }

		public string Path { get; private set; }
		public readonly DocumentHistory History;
		public bool IsModified => History.IsDocumentModified;
		/// <summary>
		/// Gets the root node for the current document.
		/// </summary>
		public Node RootNode { get; private set; }
		/// <summary>
		/// Gets or sets the current container widget.
		/// </summary>
		public Node Container { get; set; }
		/// <summary>
		/// Gets or sets the scene we are navigated from. Need for getting back into the main scene from the external one.
		/// </summary>
		public string SceneNavigatedFrom { get; set; }
		/// <summary>
		/// The list of rows, currently displayed on the timeline.
		/// </summary>
		public readonly List<Row> Rows = new List<Row>();
		/// <summary>
		/// The list of selected rows, currently displayed on the timeline.
		/// </summary>
		public readonly VersionedCollection<Row> SelectedRows = new VersionedCollection<Row>();
		/// <summary>
		/// The list of views (timeline, inspector, ...)
		/// </summary>
		public readonly List<IDocumentView> Views = new List<IDocumentView>();

		public static readonly List<Func<IDocumentUpdater>> UpdaterBuilders = new List<Func<IDocumentUpdater>>();
		readonly List<IDocumentUpdater> updaters = new List<IDocumentUpdater>();

		public int AnimationFrame
		{
			get { return Container.AnimationFrame; }
			set { Container.AnimationFrame = value; }
		}

		public string AnimationId { get; set; }

		public Document()
		{
			History = new DocumentHistory();
			History.Changed += Update;
			History.Changed += Application.InvalidateWindows;
		}

		public Document(string path) : this()
		{
			Path = path;
			using (Theme.Push(DefaultTheme.Instance)) {
				RootNode = new Frame(path);
			}
			Container = RootNode;
		}

		public void MakeCurrent()
		{
			SetCurrent(this);
		}

		public static void SetCurrent(Document doc)
		{
			if (Current != null) {
				Current.DetachViews();
			}
			Current = doc;
			if (doc != null) {
				doc.AttachViews();
			}
		}

		void AttachViews()
		{
			AttachingViews?.Invoke(this);
			foreach (var i in Current.Views) {
				i.Attach();
			}
			if (updaters.Count == 0) {
				updaters.AddRange(UpdaterBuilders.Select(i => i()));
			}
		}

		void DetachViews()
		{
			foreach (var i in Current.Views) {
				i.Detach();
			}
		}

		public bool Close()
		{
			if (!IsModified) {
				return true;
			}
			if (Closing != null) {
				var r = Closing(this);
				if (r == CloseAction.Cancel) {
					return false;
				}
				if (r == CloseAction.SaveChanges) {
					Save();
				}
			} else {
				Save();
			}
			return true;
		}

		public void Save()
		{
			var bd = ((UnpackedAssetsBundle)AssetsBundle.Instance).BaseDirectory;
			var absPath = System.IO.Path.ChangeExtension(System.IO.Path.Combine(bd, Path), ".scene");
			using (var stream = new FileStream(absPath, FileMode.Create)) {
				var serializer = new Orange.HotSceneExporter.Serializer();
				var node = RootNode.Clone();
				foreach (var n in node.Descendants) {
					n.AnimationFrame = 0;
				}
				foreach (var n in node.Descendants.Where(i => i.ContentsPath != null)) {
					n.Nodes.Clear();
					n.Markers.Clear();
				}
				Serialization.WriteObject(Path, stream, node, serializer);
			}
		}

		/// <summary>
		/// Restores the document consistency.
		/// </summary>
		public void Update()
		{
			foreach (var i in updaters) {
				i.Update();
			}
		}

		public IEnumerable<Node> SelectedNodes()
		{
			return SelectedRows.Select(i => i.Components.Get<Components.NodeRow>()?.Node).Where(n => n != null);
		}

		public Row GetRowById(Uid uid)
		{
			Row row;
			if (!RowCache.TryGetValue(uid, out row)) {
				row = new Row(uid);
				RowCache.Add(uid, row);
			}
			return row;
		}
	}
}
