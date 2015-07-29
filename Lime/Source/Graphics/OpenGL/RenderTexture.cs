#if OPENGL
using System;
using System.Diagnostics;
#if iOS || ANDROID
using OpenTK.Graphics.ES20;
#else
using OpenTK.Graphics.OpenGL;
#endif
using System.Collections.Generic;

namespace Lime
{
	public enum RenderTextureFormat
	{
		RGBA8,
		RGB565
	}

	public class RenderTexture : CommonTexture, ITexture, IGLObject
	{
		private uint handle;
		private uint framebuffer;
		private readonly Size size = new Size(0, 0);
		private readonly Rectangle uvRect;
		private static readonly Stack<uint> framebufferStack = new Stack<uint>();

		public RenderTextureFormat Format { get; private set; }

		public RenderTexture(int width, int height, RenderTextureFormat format = RenderTextureFormat.RGBA8)
		{
			Format = format;
			size.Width = width;
			size.Height = height;
			uvRect = new Rectangle(0, 0, 1, 1);
			GLObjectRegistry.Instance.Add(this);
		}

		private void CreateTexture()
		{
			if (!Application.IsMainThread) {
				throw new Lime.Exception("Attempt to create a RenderTexture not from the main thread");
			}
			var t = new uint[1];
			GL.GenFramebuffers(1, t);
			framebuffer = t[0];
			GL.GenTextures(1, t);
			handle = t[0];
			PlatformRenderer.PushTexture(handle, 0);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)All.ClampToEdge);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)All.ClampToEdge);
			int bpp;
			if (Format == RenderTextureFormat.RGBA8) {
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, size.Width, size.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)null);
				bpp = 4;
			} else {
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, size.Width, size.Height, 0, PixelFormat.Rgb, PixelType.UnsignedShort565, (IntPtr)null);
				bpp = 2;
			}
			MemoryUsed = SurfaceSize.Width * SurfaceSize.Height * bpp;
			uint oldFramebuffer = PlatformRenderer.CurrentFramebuffer;
			PlatformRenderer.BindFramebuffer(framebuffer);
			PlatformRenderer.CheckErrors();
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, handle, 0);
			if ((int)GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != (int)FramebufferErrorCode.FramebufferComplete)
				throw new Exception("Failed to create render texture. Framebuffer is incomplete.");
			Renderer.ClearRenderTarget(0, 0, 0, 0);
			PlatformRenderer.PopTexture(0);
			PlatformRenderer.BindFramebuffer(oldFramebuffer);
			PlatformRenderer.CheckErrors();
		}

		public Size ImageSize {
			get { return size; }
		}

		public Size SurfaceSize {
			get { return size; }
		}		
		
		public Rectangle AtlasUVRect {
			get { return uvRect; }
		}

		public ITexture AlphaTexture { get { return null; } }

		public void TransformUVCoordinatesToAtlasSpace(ref Vector2 uv) {}

		public void Discard() 
		{
			MemoryUsed = 0;
			if (framebuffer != 0) {
				var capturedFramebuffer = framebuffer;
				Application.InvokeOnMainThread(() => {
					GL.DeleteFramebuffers(1, new uint[] { capturedFramebuffer });
					PlatformRenderer.CheckErrors();
				});
				framebuffer = 0;
			}
			if (handle != 0) {
				var capturedHandle = handle;
				Application.InvokeOnMainThread(() => {
					GL.DeleteTextures(1, new uint[] { capturedHandle });
					PlatformRenderer.CheckErrors();
				});
				handle = 0;
			}
		}
		
		public override void Dispose()
		{
			Discard();
			base.Dispose();
		}

		~RenderTexture()
		{
			Dispose();
		}

		public uint GetHandle()
		{
			if (handle == 0) {
				CreateTexture();
			}
			return handle;
		}

		public bool IsStubTexture { get { return false; } }

		public string SerializationPath {
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}

		public void SetAsRenderTarget()
		{
			Renderer.Flush();
			GetHandle();
			uint currentFramebuffer = (uint)PlatformRenderer.CurrentFramebuffer;
			PlatformRenderer.BindFramebuffer(framebuffer);
			framebufferStack.Push(currentFramebuffer);
		}

		public void RestoreRenderTarget()
		{
			Renderer.Flush();
			uint prevFramebuffer = framebufferStack.Pop();
			PlatformRenderer.BindFramebuffer(prevFramebuffer);
		}

		public bool IsTransparentPixel(int x, int y)
		{
			return false;
		}
	}
}
#endif