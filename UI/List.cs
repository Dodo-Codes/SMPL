﻿using SMPL.Tools;
using SMPL.Graphics;
using SMPL.UI;
using System.Collections.Generic;
using System.Collections;
using System;
using Newtonsoft.Json;
using SFML.Window;

namespace SMPL.UI
{
	/// <summary>
	/// Inherit chain: <see cref="List"/> : <see cref="ScrollBar"/> : <see cref="Slider"/> : <see cref="ProgressBar"/> : <see cref="Sprite"/> :
	/// <see cref="Visual"/> : <see cref="Object"/> <br></br><br></br>
	/// This class takes control of multiple equally sized <see cref="Button"/>s by adding, removing, positioning and scrolling.
	/// </summary>
	public class List : ScrollBar
	{
		private float spacing = 10;
		private int scrollIndex;
		private Hitbox hitbox = new();
		private Button clicked;

		[JsonIgnore]
		public List<Button> Buttons { get; private set; } = new();
		public int VisibleButtonCountMax { get; set; } = 5;
		public int VisibleButtonCountCurrent => Math.Min(Buttons.Count, VisibleButtonCountMax);
		public bool IsHovered
		{
			get
			{
				if (IsHidden || IsDisabled || Buttons.Count == 0)
					return false;

				var topLeft = ScrollUp.CornerA.PointMoveAtAngle(180, ScrollUp.Size.X + ButtonWidth, false);
				var bottomLeft = ScrollDown.CornerB.PointMoveAtAngle(180, ScrollDown.Size.X + ButtonWidth, false);

				hitbox.Lines.Clear();
				hitbox.Lines.Add(new(topLeft, ScrollUp.CornerA));
				hitbox.Lines.Add(new(ScrollUp.CornerA, ScrollDown.CornerB));
				hitbox.Lines.Add(new(ScrollDown.CornerB, bottomLeft));
				hitbox.Lines.Add(new(bottomLeft, topLeft));

				return hitbox.ConvexContains(Scene.MouseCursorPosition);
			}
		}
		public float ButtonWidth { get; set; } = 400;
		public float ButtonHeight
		{
			get
			{
				var max = Math.Max(VisibleButtonCountMax, 1);
				return (LengthMax + ScrollDown.Size.X * 2) / max - (ButtonSpacing - (ButtonSpacing / max));
			}
		}
		public float ButtonSpacing
		{
			get => spacing;
			set => spacing = value.Limit(0, LengthMax);
		}
		public int ScrollIndex => scrollIndex;

		public List()
			=> Value = 0;

		protected virtual void OnUnfocus() { }
		protected virtual void OnButtonClick(Button button) { }
		public override void Draw(Camera camera = null)
		{
			OriginUnit = new(0, 0.5f);

			if (IsHidden)
				return;

			VisibleButtonCountMax = Math.Max(VisibleButtonCountMax, 1);

			ScrollValue = 1;
			RangeA = 0;
			RangeB = MathF.Max((Buttons.Count - VisibleButtonCountMax).Limit(0, Buttons.Count - 1), RangeA);

			scrollIndex = (int)Value;

			var nothingToScroll = RangeA == RangeB;
			ScrollUp.IsHidden = nothingToScroll;
			ScrollUp.IsDisabled = nothingToScroll;
			ScrollDown.IsHidden = nothingToScroll;
			ScrollDown.IsDisabled = nothingToScroll;

			base.Draw(camera);

			for (int i = scrollIndex; i < Buttons.Count; i++)
			{
				var btn = Buttons[i];
				btn.Parent = this;
				if (i >= scrollIndex && i < scrollIndex + VisibleButtonCountMax)
				{
					var x = ButtonHeight * 0.5f - ScrollUp.Size.X + ((ButtonHeight + ButtonSpacing) * (i - scrollIndex));
					var y = -Size.Y * OriginUnit.Y + ButtonWidth * 0.5f + Size.Y;

					btn.Size = new(ButtonWidth, ButtonHeight);
					btn.OriginUnit = new(0.5f);
					btn.LocalPosition = new(x, y);
					btn.SetDefaultHitbox();
					btn.Hitbox.TransformLocalLines(btn);
					btn.Draw(camera);
				}
				else
					btn.Hitbox.Lines.Clear();
			}
			IsReceivingInput = IsHovered;

			Update();
		}

		internal void Update()
		{
			var left = Mouse.IsButtonPressed(Mouse.Button.Left);

			if (left.Once($"click-list-{GetHashCode()}"))
			{
				clicked = null;
				var first = ScrollIndex.Limit(0, Buttons.Count);
				var last = (ScrollIndex + VisibleButtonCountMax).Limit(0, Buttons.Count);
				for (int i = first; i < last; i++)
					if (Buttons[i].Hitbox.IsHovered)
						clicked = Buttons[i];

				if (IsHovered == false)
					OnUnfocus();
			}
			if ((left == false).Once($"release-list-{GetHashCode()}") && clicked != null && clicked.Hitbox.IsHovered)
			{
				clicked.Hover();
				OnButtonClick(clicked);
			}
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			hitbox = null;
			clicked = null;
			for (int i = 0; i < Buttons.Count; i++)
				Buttons[i].Destroy();
			Buttons = null;
		}
	}
}
