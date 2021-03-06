namespace SMPL
{
	public static partial class Thing
	{
		public static Color AmbientColor { get; set; } = new Color(50, 50, 50);
	}

	internal class LightInstance : ThingInstance
	{
		public Color Color { get; set; } = new Color(255, 255, 255, 50);
		#region Backend
		internal static readonly Vector2[] positions = new Vector2[50];
		internal static readonly Color[] colors = new Color[50];
		internal static readonly float[] scales = new float[50];

		internal static List<LightInstance> lights = new();

		internal const float SIZE = 100f;
		internal const float HALF_SIZE = SIZE / 2f;

		[JsonConstructor]
		internal LightInstance()
		{
			TryInit();
		}
		internal LightInstance(string uid) : base(uid)
		{
			TryInit();
		}
		private void TryInit()
		{
			if(TryCreationError() == false)
				lights.Add(this);
		}
		private bool TryCreationError()
		{
			if(lights.Count == 50)
			{
				Console.LogError(1, $"Cannot create {nameof(LightInstance)}{{{UID}}}. Only up to 50 lights are allowed.");
				return true;
			}
			return false;
		}

		internal void UpdateGlobalArrays()
		{
			var i = lights.IndexOf(this);
			positions[i] = Position;
			colors[i] = Color;
			scales[i] = Scale;
		}
		internal static void Update(VisualInstance visual, RenderTarget renderTarget)
		{
			for(int i = 0; i < lights.Count; i++)
				lights[i].UpdateGlobalArrays();

			var positionsInShader = new Vector2[positions.Length];
			for(int j = 0; j < positionsInShader.Length; j++)
			{
				var p = renderTarget.MapCoordsToPixel(positions[j].ToSFML(), renderTarget.GetView()) - new Vector2i(0, (int)(renderTarget.Size.Y));
				positionsInShader[j] = new Vector2(p.X, p.Y);
			}

			visual.SetEffectColor("AmbientColor", Thing.AmbientColor);
			visual.SetShaderColorArray("Colors", colors);
			visual.SetShaderVector2Array("Positions", positionsInShader);
			visual.SetShaderFloatArray("Scales", scales);
		}

		internal override void OnDestroy()
		{
			base.OnDestroy();

			var i = lights.IndexOf(this);
			positions[i] = default;
			colors[i] = default;
			scales[i] = default;

			lights.Remove(this);
		}
		internal override Hitbox GetBoundingBox()
		{
			var hitbox = new Hitbox(
				new(-HALF_SIZE, -HALF_SIZE),
				new(HALF_SIZE, -HALF_SIZE),
				new(HALF_SIZE, HALF_SIZE),
				new(-HALF_SIZE, HALF_SIZE),
				new(-HALF_SIZE, -HALF_SIZE));
			return hitbox;
		}
		#endregion
	}
}
