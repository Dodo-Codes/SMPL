﻿using SFML.Graphics;
using SFML.Window;

namespace SMPL
{
	/// <summary>
	/// Inherit chain: <see cref="Slider"/> : <see cref="ProgressBar"/> : <see cref="Sprite"/> : <see cref="Visual"/> : <see cref="Object"/><br></br><br></br>
	/// An interactable graphic that indicatates some value in a range.
	/// </summary>
	public class Slider : ProgressBar
	{
		private bool isClicked;

		/// <summary>
		/// The length of the <see cref="Slider"/> in range [0 - 1] with 1 being the <see cref="Sprite.Size"/>.
		/// </summary>
		public float LengthUnit { get; set; } = 0.2f;
		/// <summary>
		/// The length of the <see cref="Slider"/> in the world.
		/// </summary>
		public float Length
		{
			get => LengthUnit.Map(0, 1, 0, LengthMax);
			set => LengthUnit = value.Map(0, LengthMax, 0, 1);
		}

		/// <summary>
		/// Draws the <see cref="Slider"/> on the <see cref="Visual.DrawTarget"/> according
		/// to all the required <see cref="Object"/>, <see cref="Visual"/>, <see cref="Sprite"/>, <see cref="ProgressBar"/> and <see cref="Slider"/> parameters.
		/// </summary>
		public override void Draw()
		{
			Update();

			if (IsHidden)
				return;

			DrawTarget ??= Scene.MainCamera;

			var w = Texture == null ? 0 : Texture.Size.X;
			var h = Texture == null ? 0 : Texture.Size.Y;
			var w0 = w * TexCoordsUnitA.X;
			var ww = w * TexCoordsUnitB.X;
			var h0 = h * TexCoordsUnitA.Y;
			var hh = h * TexCoordsUnitB.Y;

			var tl = CornerA.PointPercentTowardPoint(CornerB, new(ProgressUnit * 100 - LengthUnit * 50));
			var tr = CornerA.PointPercentTowardPoint(CornerB, new(ProgressUnit * 100 + LengthUnit * 50));
			var bl = CornerD.PointPercentTowardPoint(CornerC, new(ProgressUnit * 100 - LengthUnit * 50));
			var br = CornerD.PointPercentTowardPoint(CornerC, new(ProgressUnit * 100 + LengthUnit * 50));

			var verts = new Vertex[]
			{
				new(tl.ToSFML(), Color, new(w0, h0)),
				new(tr.ToSFML(), Color, new(ww, h0)),
				new(br.ToSFML(), Color, new(ww, hh)),
				new(bl.ToSFML(), Color, new(w0, hh)),
			};

			DrawTarget.renderTexture.Draw(verts, PrimitiveType.Quads, new(BlendMode, Transform.Identity, Texture, Shader));
		}

		private void Update()
		{
			Size = new(LengthMax * Scale, Size.Y);

			SetDefaultHitbox();
			Hitbox.TransformLocalLines(this);

			var left = Mouse.IsButtonPressed(Mouse.Button.Left);
			if (left.Once($"slider-click-{GetHashCode()}") && Hitbox.ConvexContains(Scene.MouseCursorPosition))
				isClicked = true;

			if (left == false)
				isClicked = false;

			if (isClicked)
			{
				var closest = new Line(CornerA, CornerB).GetClosestPoint(Scene.MouseCursorPosition);
				var dist = CornerA.DistanceBetweenPoints(CornerB);
				var value = CornerA.DistanceBetweenPoints(closest).Map(0, dist, RangeA, RangeB);
				var sz = Size;
				var txB = TexCoordsUnitB;

				Value = value;
				Size = sz;
				TexCoordsUnitB = txB;
			}
		}
	}
}