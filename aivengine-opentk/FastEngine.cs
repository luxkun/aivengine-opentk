using System;
using Aiv.Engine;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using System.IO;
using System.Threading;

namespace Aiv.Engine
{
	public class FastEngine : Engine
	{
		public GameWindow window;
		private int texture;
		private Rectangle textureRect;
		private AudioContext audioContext;


		public FastEngine (string windowName, int width, int height, int fps)
		{
			
			window = new GameWindow (width, height, OpenTK.Graphics.GraphicsMode.Default, windowName);

			// call it AFTER GameWindow initialization to avoid problems with Windows.Forms
			this.Initialize (windowName, width, height, fps);

			// create a new texture
			texture = GL.GenTexture ();
			// use the texure (as a 2d one)
			GL.BindTexture (TextureTarget.Texture2D, texture);
			GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			this.textureRect = new Rectangle (0, 0, width, height);

			GL.Enable (EnableCap.Texture2D);

			window.Resize += this.Game_Resize;
			window.RenderFrame += this.Game_RenderFrame;

			// initialize OpenAL context (No ! do it lazily)
			// audioContext = new AudioContext ();

		}

		public override void Run ()
		{
			this.isGameRunning = true;
			this.window.Run (this.fps);
		}

		private void Game_RenderFrame (object sender, FrameEventArgs e)
		{

			if (!this.isGameRunning)
				this.window.Exit ();

			this.GameUpdate (this.ticks);

			// upload texture
			System.Drawing.Imaging.BitmapData data = this.workingBitmap.LockBits (this.textureRect, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			GL.TexImage2D (TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, this.width, this.height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
			this.workingBitmap.UnlockBits (data);

			GL.Clear (ClearBufferMask.ColorBufferBit);

			// projection matrix
			GL.MatrixMode (MatrixMode.Projection);
			GL.LoadIdentity ();
			GL.Ortho (-1.0, 1.0, -1.0, 1.0, 0.0, 4.0);

			// models
			GL.MatrixMode (MatrixMode.Modelview);
			GL.LoadIdentity ();

			GL.Begin (PrimitiveType.Quads);

			GL.TexCoord2 (0.0f, 1.0f);
			GL.Vertex2 (-1f, -1f);
			GL.TexCoord2 (1.0f, 1.0f);
			GL.Vertex2 (1f, -1f);
			GL.TexCoord2 (1.0f, 0.0f);
			GL.Vertex2 (1f, 1f);
			GL.TexCoord2 (0.0f, 0.0f);
			GL.Vertex2 (-1f, 1f);

			GL.End ();

			this.window.SwapBuffers ();
		}

		private void Game_Resize (object sender, EventArgs e)
		{
			GL.Viewport (this.window.ClientRectangle);
		}

		public override bool IsKeyDown (int key)
		{
			OpenTK.Input.KeyboardState state = OpenTK.Input.Keyboard.GetState ();
			return state.IsKeyDown ((OpenTK.Input.Key)key);
		}

		private void PlaySoundThread (string assetName, bool loop)
		{
			string fileName = this.GetAsset (assetName).fileName;

			int channels, bits_per_sample, sample_rate;
			byte[] data = Utils.LoadWave (fileName, out channels, out bits_per_sample, out sample_rate);

			int buffer = AL.GenBuffer ();
			int source = AL.GenSource ();
			AL.BufferData (buffer, Utils.WaveFormat (channels, bits_per_sample), data, data.Length, sample_rate);

			AL.Source (source, ALSourcei.Buffer, buffer);
			AL.Source (source, ALSourceb.Looping, loop);


			AL.SourcePlay (source);

			int state;

			do {
				Thread.Sleep (300);
				AL.GetSource (source, ALGetSourcei.SourceState, out state);
			} while ((ALSourceState)state == ALSourceState.Playing);

			AL.SourceStop (source);
			AL.DeleteSource (source);
			AL.DeleteBuffer (buffer);
		}

		public override void PlaySound (string assetName)
		{
			if (audioContext == null)
				audioContext = new AudioContext ();
			Thread soundThread = new Thread (() => this.PlaySoundThread (assetName, false));
			soundThread.Start ();


		}

		public override void PlaySoundLoop (string assetName)
		{
			if (audioContext == null)
				audioContext = new AudioContext ();
			Thread soundThread = new Thread (() => this.PlaySoundThread (assetName, true));
			soundThread.Start ();
		}
	}
}

