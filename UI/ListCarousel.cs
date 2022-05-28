﻿using Newtonsoft.Json;
using SFML.Window;
using SMPL.Graphics;
using SMPL.Tools;
using System.Numerics;

namespace SMPL.UI
{
	public class ListCarousel : List
	{
		[JsonIgnore]
		public Button Previous { get; private set; }
		[JsonIgnore]
		public Button Next { get; private set; }

		public new bool IsHovered
		{
			get
			{
				if (IsHidden || IsDisabled)
					return false;

				var hitbox = new Hitbox(
					Previous.CornerA,
					Next.CornerB,
					Next.CornerC,
					Previous.CornerD,
					Previous.CornerA);

				return hitbox.ConvexContains(Scene.MouseCursorPosition);
			}
		}
		public bool SelectionIsRepeating { get; set; }
		public int SelectionIndex
		{
			get => (int)Value;
			set => Value = value.Limit(0, Buttons.Count, SelectionIsRepeating ? Extensions.Limitation.Overflow : Extensions.Limitation.ClosestBound);
		}
		[JsonIgnore]
		public Button Selection => Buttons.Count == 0 ? null : Buttons[SelectionIndex];

		public ListCarousel(Button previous = null, Button next = null)
		{
			VisibleButtonCountMax = 7;
			SelectionIsRepeating = true;

			Angle = 0;
			previous ??= new();
			next ??= new();

			Previous = previous;
			Next = next;

			Previous.Clicked += OnPrevious;
			Next.Clicked += OnNext;

			Previous.Held += OnPreviousHold;
			Next.Held += OnNextHold;
		}

		private void OnPreviousHold() => OnScrollDown();
		private void OnNextHold() => OnScrollUp();
		private void OnPrevious() => OnScrollDown();
		private void OnNext() => OnScrollUp();

		protected override void OnScrollUp()
		{
			if (IsDisabled)
				return;

			SelectionIndex++;
		}
		protected override void OnScrollDown()
		{
			if (IsDisabled)
				return;

			SelectionIndex--;
		}

		public override void Draw(Camera camera = null)
		{
			ScrollValue = 1;
			RangeA = 0;
			RangeB = Buttons.Count - 1;

			Previous.Parent = this;
			Next.Parent = this;

			Previous.Size = new Vector2(ButtonHeight, ButtonHeight) * Scale;
			Next.Size = new Vector2(ButtonHeight, ButtonHeight) * Scale;

			Previous.LocalPosition = new(-ButtonWidth * 0.5f - ButtonHeight * 0.5f, 0);
			Next.LocalPosition = new(ButtonWidth * 0.5f + ButtonHeight * 0.5f, 0);

			TryScaleQuickText(Previous);
			TryScaleQuickText(Next);

			Previous.Draw(camera);
			Next.Draw(camera);

			if (Selection == null)
				return;

			Selection.Parent = this;
			Selection.Size = new Vector2(ButtonWidth, ButtonHeight) * Scale;
			Selection.LocalPosition = new();
			Selection.Angle = Angle;

			TryScaleQuickText(Selection);

			Selection.Draw(camera);

			void TryScaleQuickText(Button button)
			{
				if (button is TextButton tb && tb.QuickText != null)
					tb.QuickText.Scale = Scale;
			}
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			Previous.Clicked -= OnPrevious;
			Next.Clicked -= OnNext;
			Previous.Held -= OnPreviousHold;
			Next.Held -= OnNextHold;

			Previous.Destroy();
			Next.Destroy();

			Previous = null;
			Next = null;
		}
	}
}
