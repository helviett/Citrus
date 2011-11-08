using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ProtoBuf;

namespace Lime
{
	[ProtoContract]
	public class PersistentFont
	{
		public Font Instance { get; private set; }

		[ProtoMember (1)]
		public string Name
		{
			get	{
				return name;
			}
			set	{
				name = value;
				Instance = FontPool.Instance [Name];
			}
		}

		private string name;

		public PersistentFont ()
		{
			Name = "";
		}

		public PersistentFont (string name)
		{
			Name = name;
		}
	}

	public class FontPool
	{
		public Font Null = new Font ();
		private Dictionary<string, Font> fonts = new Dictionary<string, Font> ();

		static readonly FontPool instance = new FontPool ();
		public static FontPool Instance { get { return instance; } }

		private FontPool () { }

		public Font this [string name]
		{
			get	{
				if (string.IsNullOrEmpty (name))
					name = "Default";
				Font font;
				if (fonts.TryGetValue (name, out font))
					return font;
				string path = "Fonts/" + name + ".fnt";
				if (!AssetsBundle.Instance.FileExists (path))
					return null;
				font = Serialization.ReadObject<Font> (path);
				fonts [name] = font;
				return font;
			}
		}
	}
}