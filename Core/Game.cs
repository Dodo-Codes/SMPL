﻿using System;
using System.Numerics;
using SFML.Graphics;
using SFML.Window;

namespace SMPL
{
	/// <summary>
	/// All the core systems are handled here (the <see cref="Game"/> states, the <see cref="Window"/>, the game loop etc).
	/// </summary>
	public static class Game
	{
		/// <summary>
		/// The raw <see cref="RenderWindow"/> instance. Useful for input events, drawing, ingame settings etc.
		/// </summary>
		public static RenderWindow Window { get; private set; }

		/// <summary>
		/// The cursor's position relative to the <see cref="Window"/>.<br></br>
		/// - Example: [0, 0] is the top left corner of the <see cref="Window"/> and [<see cref="Window.Size"/>] is the bottom right corner
		/// </summary>
		public static Vector2 MouseCursorPosition
		{
			get { var p = Mouse.GetPosition(Window); return new(p.X, p.Y); }
         set { Mouse.SetPosition(new((int)value.X, (int)value.Y), Window); }
		}

		/// <summary>
		/// Boots up the <see cref="Game"/> systems that initialize the <see cref="Window"/> alongside other things.
		/// The <paramref name="startingScene"/> is set as <see cref="Scene.CurrentScene"/> and the optional <paramref name="loadingScene"/> as
		/// <see cref="Scene.LoadingScene"/>.
		/// </summary>
		public static void Start(Scene startingScene, Scene loadingScene = null, uint mainCameraWidth = 1920, uint mainCameraHeight = 1080)
		{
			if (startingScene == null || Window != null)
				return;

			InitWindow();

			Scene.Init(startingScene, loadingScene);
			Scene.MainCamera = new(mainCameraWidth, mainCameraHeight);

			while (Window.IsOpen)
			{
				Window.DispatchEvents();
				Window.Clear();
				Scene.MainCamera.Fill(Color.Black);

				Time.Update();
				Scene.UpdateCurrentScene();
				Camera.DrawMainCameraToWindow();
				Window.Display();
			}

			void InitWindow()
			{
				Window = new(new(1280, 720), "SMPL Game");
				Window.Clear();
				Window.Display();
				Window.Closed += OnClose;
				Window.SetFramerateLimit(120);

				var view = Window.GetView();
				view.Center = new();
				Window.SetView(view);
			}
		}
		/// <summary>
		/// Notifies the <see cref="Scene.CurrentScene"/> with <see cref="Scene.OnGameStop"/> then closes out the <see cref="Window"/>
		/// and shuts down everything.
		/// </summary>
		public static void Stop()
		{
			Scene.CurrentScene?.GameStop();
			Window.Close();
		}

		private static void Main() { }
		private static void OnClose(object sender, EventArgs e) => Stop();
	}
}