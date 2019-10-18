using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lime;
using Tangerine.Core;
using Tangerine.Core.Operations;

namespace Tangerine.UI.FilesDropHandler
{
	public class ImagesDropHandler : IFilesDropHandler
	{
		private readonly List<Type> imageTypes = new List<Type> {
			typeof(Image), typeof(DistortionMesh), typeof(NineGrid),
			typeof(TiledImage), typeof(ParticleModifier),
		};

		public List<string> Extensions { get; } = new List<string> { ".png" };

		public void Handle(IEnumerable<string> files, IFilesDropCallbacks callbacks, out IEnumerable<string> handledFiles)
		{
			handledFiles = files.Where(f => Extensions.Contains(Path.GetExtension(f)));
			if (handledFiles.Any()) {
				CreateContextMenu(handledFiles, callbacks);
			}
		}

		private void CreateContextMenu(IEnumerable<string> files, IFilesDropCallbacks callbacks)
		{
			var menu = new Menu();
			foreach (var imageType in imageTypes) {
				if (NodeCompositionValidator.Validate(Document.Current.Container.GetType(), imageType)) {
					menu.Add(new Command($"Create {imageType.Name}",
						() => CreateImageTypeInstance(imageType, files, callbacks)));
				}
			}
			menu.Popup();
		}


		private void CreateImageTypeInstance(Type type, IEnumerable<string> files, IFilesDropCallbacks callbacks)
		{
			using (Document.Current.History.BeginTransaction()) {
				foreach (var file in files) {
					if (
						!Utils.ExtractAssetPathOrShowAlert(file, out var assetPath, out var assetType) ||
						!Utils.AssertCurrentDocument(assetPath, assetType)
					) {
						continue;
					}
					var args = new FilesDropManager.NodeCreatingEventArgs(assetPath, assetType);
					callbacks.NodeCreating?.Invoke(args);
					if (args.Cancel) {
						continue;
					}
					var node = CreateNode.Perform(type);
					var texture = new SerializableTexture(assetPath);
					var nodeSize = (Vector2)texture.ImageSize;
					var nodeId = Path.GetFileNameWithoutExtension(assetPath);
					if (node is Widget) {
						SetProperty.Perform(node, nameof(Widget.Texture), texture);
						SetProperty.Perform(node, nameof(Widget.Pivot), Vector2.Half);
						SetProperty.Perform(node, nameof(Widget.Size), nodeSize);
						SetProperty.Perform(node, nameof(Widget.Id), nodeId);
					} else if (node is ParticleModifier) {
						SetProperty.Perform(node, nameof(ParticleModifier.Texture), texture);
						SetProperty.Perform(node, nameof(ParticleModifier.Size), nodeSize);
						SetProperty.Perform(node, nameof(ParticleModifier.Id), nodeId);
					}
					callbacks.NodeCreated?.Invoke(node);
				}
				Document.Current.History.CommitTransaction();
			}
		}
	}
}
