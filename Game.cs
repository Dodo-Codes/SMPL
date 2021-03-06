global using System;
global using System.Collections.Concurrent;
global using System.Collections.Generic;
global using System.Collections.ObjectModel;
global using System.Diagnostics;
global using System.IO;
global using System.Linq;
global using System.Numerics;
global using System.Reflection;
global using System.Runtime.CompilerServices;
global using System.Runtime.InteropServices;
global using System.Runtime.Serialization;
global using System.Threading;
global using System.Windows.Forms;

global using Fasterflect;

global using Newtonsoft.Json;
global using Newtonsoft.Json.Serialization;

global using SFML.Audio;
global using SFML.Graphics;
global using SFML.Graphics.Glsl;
global using SFML.System;
global using SFML.Window;

global using SMPL.Prefabs;
global using SMPL.Tools;
global using SMPL.UI;

global using Console = SMPL.Tools.Console;
global using Time = SMPL.Tools.Time;

namespace SMPL
{
	public static class Game
	{
		public enum WindowState { Windowed, Borderless, Fullscreen }
		public static Settings Settings => settings;

		public static RenderWindow Window { get; internal set; }
		public static Vector2 MouseCursorPosition
		{
			get { var p = Mouse.GetPosition(Window); return new(p.X, p.Y); }
			set { Mouse.SetPosition(new((int)value.X, (int)value.Y), Window); }
		}

		public static void Start(string sceneName, params string[] initialAssetPaths)
		{
			if(string.IsNullOrWhiteSpace(sceneName))
				return;

			Scene.CurrentScene = new(sceneName, initialAssetPaths);
			Start();
		}
		public static void Load(string scenePath)
		{
			Scene.Load(scenePath);
			Start();
		}
		public static void UpdateEngine(RenderTarget renderTarget)
		{
			Window.DispatchEvents();
			Scene.MainCamera.RenderTexture.Clear(Color.Black);
			Time.Update();

			var visuals = VisualInstance.visuals.Reverse();
			var cameras = CameraInstance.cameras;

			for(int i = 0; i < cameras.Count; i++)
				if(cameras[i].UID != Scene.MAIN_CAMERA_UID)
					cameras[i].RenderTexture.Clear(Color.Transparent);

			foreach(var kvp in visuals)
				for(int i = 0; i < kvp.Value.Count; i++)
				{
					var camUIDs = Thing.GetUIDsByTag(kvp.Value[i].CameraTag);
					for(int j = 0; j < camUIDs.Count; j++)
					{
						var cam = ThingInstance.Get<CameraInstance>(camUIDs[j]);
						if(cam != null)
							kvp.Value[i].Draw(cam.RenderTexture);
					}
				}


			for(int i = 0; i < cameras.Count; i++)
				if(cameras[i].UID != Scene.MAIN_CAMERA_UID)
					cameras[i].RenderTexture.Display();

			foreach(var kvp in visuals)
				for(int i = 0; i < kvp.Value.Count; i++)
					kvp.Value[i].Draw(renderTarget);

			Scene.UpdateCurrentScene();
			AudioInstance.Update();
			CameraInstance.DrawMainCameraToWindow();
		}
		public static void Stop()
		{
			Settings.Save();
			Event.GameStop();
			Window.Close();
		}

		public static void OpenWebPage(string url)
		{
			try { Process.Start(url); }
			catch
			{
				if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					url = url.Replace("&", "^&");
					Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
				}
				else if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
					Process.Start("xdg-open", url);
				else if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
					Process.Start("open", url);
				else
					Console.LogError(1, $"Could not load URL '{url}'.");
			}
		}
		public static void CenterWindow()
		{
			var sz = Settings.ScreenResolution;
			Window.Size = new((uint)(sz.X / 1.5f), (uint)(sz.Y / 1.5f));
			Window.Position = new Vector2i((int)(sz.X / 2f), (int)(sz.Y / 2f)) - new Vector2i((int)(Window.Size.X / 2), (int)(Window.Size.Y / 2));
		}

		#region Backend
		internal static Settings settings = new();
		internal static Styles currWindowStyle;
		private static void Main() { }
		private static void Start()
		{
			if(Window != null)
				return;

			Settings.Load();
			InitWindow(Settings.WindowState, Settings.Resolution);

			if(Thing.Exists(Scene.MAIN_CAMERA_UID) == false)
				Thing.CreateCamera(Scene.MAIN_CAMERA_UID, Settings.ScreenResolution);

			Scene.assetsLoading = new(Scene.ThreadLoadAssets) { IsBackground = true, Name = "AssetsLoading" };
			Scene.assetsLoading.Start();

			while(Window.IsOpen)
				UpdateEngine(Scene.MainCamera.RenderTexture);
		}
		private static void OnClose(object sender, EventArgs e) => Stop();

		internal static void InitWindow(WindowState windowState, Vector2 resolution)
		{
			if(Window != null)
			{
				Window.Closed -= OnClose;
				Window.Dispose();
			}

			currWindowStyle = windowState.ToWindowStyles();
			Window = new(new((uint)resolution.X, (uint)resolution.Y), "SMPL Game", currWindowStyle) { Position = new() };
			Window.Clear();
			Window.Display();
			Window.Closed += OnClose;
			Window.SetFramerateLimit(120);
			Window.SetVerticalSyncEnabled(Settings.IsVSyncEnabled);

			var view = Window.GetView();
			view.Center = new();
			Window.SetView(view);

			if(windowState == WindowState.Windowed)
				CenterWindow();
		}
		internal static WindowState ToWindowStates(this Styles style)
		{
			return style switch
			{
				Styles.Fullscreen => WindowState.Fullscreen,
				Styles.None => WindowState.Borderless,
				_ => WindowState.Windowed,
			};
		}
		internal static Styles ToWindowStyles(this WindowState windowStates)
		{
			return windowStates switch
			{
				WindowState.Borderless => Styles.None,
				WindowState.Fullscreen => Styles.Fullscreen,
				_ => Styles.Default,
			};
		}
		#endregion
	}
}
