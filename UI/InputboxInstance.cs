namespace SMPL.UI
{
	internal class InputboxInstance : TextboxInstance
	{
		public static bool IsFocused { get; set; }
		public bool IsDisabled { get; set; }

		public Color CursorColor { get; set; }
		public int CursorPositionIndex
		{
			get => cursorIndex;
			set => cursorIndex = value.Limit(0, Value.Length);
		}

		public string PlaceholderValue { get; set; } = "Type...";
		public Color PlaceholderColor { get; set; } = new(255, 255, 255, 70);

		public void Submit()
		{
			Event.InputboxSubmit(UID);
		}

		#region Backend
		private const float SPEED = 0.5f;
		private readonly Clock cursorBlinkTimer = new(), holdTimer = new(), holdTickTimer = new();
		private bool cursorIsVisible;
		private int cursorIndex;
		private float cursorPosX;

		[JsonConstructor]
		internal InputboxInstance() => Init();
		internal InputboxInstance(string uid, string fontPath) : base(uid, fontPath)
		{
			Init();
		}

		internal override void OnDraw(RenderTarget renderTarget)
		{
			Alignment = Thing.TextboxAlignment.TopLeft;

			GetCamera()?.RenderTexture.Clear(BackgroundColor);

			TryInput();
			TryDrawPlaceholder();
			base.OnDraw(renderTarget);
			TryDrawCursor();

			TryMoveTextWhenCursorOut();

			DrawTextbox(renderTarget);
		}
		internal override void OnDestroy()
		{
			base.OnDestroy();
			cursorBlinkTimer.Dispose();
			holdTimer.Dispose();
			holdTickTimer.Dispose();
			Game.Window.TextEntered -= OnInput;
		}

		private void Init()
		{
			Game.Window.TextEntered += OnInput;
			Value = "";
			CursorColor = Color.White;
			skipParentRender = true;
		}
		private void OnInput(object sender, TextEventArgs e)
		{
			if(IsFocused == false || Keyboard.IsKeyPressed(Keyboard.Key.LControl) || Keyboard.IsKeyPressed(Keyboard.Key.RControl))
				return;

			var keyStr = e.Unicode;
			keyStr = keyStr.Replace('\r', '\n');
			ShowCursor();

			if(keyStr == "\n")
			{
				IsFocused = false;
				Submit();
				return;
			}

			if(keyStr == "\b") // is backspace
			{
				if(CursorPositionIndex == 0)
					return;

				Value = Value.Remove((int)CursorPositionIndex - 1, 1);
				CursorPositionIndex--;
			}
			else
			{
				Value = Value.Insert((int)CursorPositionIndex, keyStr);
				CursorPositionIndex++;
			}
		}
		private void ShowCursor()
		{
			cursorBlinkTimer.Restart();
			cursorIsVisible = true;
		}

		private void TryDrawPlaceholder()
		{
			if(Value.Length != 0)
				return;

			Update();
			textInstance.OutlineThickness = 0;
			textInstance.FillColor = PlaceholderColor;
			textInstance.DisplayedString = "  " + PlaceholderValue;

			GetCamera()?.RenderTexture.Draw(textInstance, new(SFML.Graphics.BlendMode.Alpha));
		}
		private void TryInput()
		{
			if(IsDisabled || Game.Window.HasFocus() == false)
				return;

			var left = Keyboard.IsKeyPressed(Keyboard.Key.Left);
			var right = Keyboard.IsKeyPressed(Keyboard.Key.Right);

			TryMoveCursorLeftRight();

			if(Mouse.IsButtonPressed(Mouse.Button.Left).Once($"press-{GetHashCode()}"))
			{
				IsFocused = BoundingBox.IsHovered;
				ShowCursor();

				var index = GetSymbolIndex(Scene.MouseCursorPosition);
				CursorPositionIndex = index == -1 ? Value.Length : index;
			}
			if(left.Once($"left-{GetHashCode()}"))
			{
				holdTimer.Restart();
				SetIndex(CursorPositionIndex - 1);
			}
			if(right.Once($"right-{GetHashCode()}"))
			{
				holdTimer.Restart();
				SetIndex(CursorPositionIndex + 1);
			}
			if(Keyboard.IsKeyPressed(Keyboard.Key.Up).Once($"up-{GetHashCode()}"))
				SetIndex(0);
			if(Keyboard.IsKeyPressed(Keyboard.Key.Down).Once($"down-{GetHashCode()}"))
				SetIndex(Value.Length);
			if(Keyboard.IsKeyPressed(Keyboard.Key.Delete).Once($"delete-{GetHashCode()}") && CursorPositionIndex < Value.Length)
			{
				ShowCursor();
				Value = Value.Remove((int)CursorPositionIndex, 1);
			}

			void SetIndex(int index)
			{
				CursorPositionIndex = index;
				ShowCursor();
			}
			void TryMoveCursorLeftRight()
			{
				var moveLeft = false;
				var moveRight = false;
				if(holdTimer.ElapsedTime.AsSeconds() > SPEED)
				{
					if(left && right == false)
						moveLeft = true;
					else if(left == false && right)
						moveRight = true;
				}
				if(holdTickTimer.ElapsedTime.AsSeconds() > SPEED * 0.075f)
				{
					holdTickTimer.Restart();
					if(moveLeft)
						SetIndex(CursorPositionIndex - 1);
					else if(moveRight)
						SetIndex(CursorPositionIndex + 1);
				}
			}
		}
		private void TryDrawCursor()
		{
			if(IsFocused == false || IsDisabled)
				return;

			if(cursorBlinkTimer.ElapsedTime.AsSeconds() > SPEED)
			{
				cursorBlinkTimer.Restart();
				cursorIsVisible = !cursorIsVisible;
			}

			if(cursorIsVisible == false)
				return;

			var addLetter = Value.Length == 0 || Value[^1] == '\n';
			var isLast = CursorPositionIndex == Value.Length;

			if(addLetter)
				Value += "|";

			var corners = GetSymbolCorners(CursorPositionIndex - (isLast && addLetter == false ? 1 : 0));
			if(corners.Count == 0)
				return;

			if(addLetter)
				Value = Value.Remove(Value.Length - 1);

			var tl = corners[isLast ? 1 : 0];
			var bl = corners[isLast ? 2 : 3];
			var sz = SymbolSize * Scale * 0.05f;
			var br = bl.PointMoveAtAngle(Angle, sz, false);
			var tr = tl.PointMoveAtAngle(Angle, sz, false);

			cursorPosX = tl.X * Scale;

			var verts = new Vertex[]
			{
				new(tl.ToSFML(), CursorColor),
				new(bl.ToSFML(), CursorColor),
				new(br.ToSFML(), CursorColor),
				new(tr.ToSFML(), CursorColor),
			};
			GetCamera()?.RenderTexture.Draw(verts, PrimitiveType.Quads);
		}
		private void TryMoveTextWhenCursorOut()
		{
			var res = GetRes();
			if(cursorPosX >= res.X)
				textOffsetX -= SymbolSize * 0.3f;
			else if(cursorPosX < -res.X)
				textOffsetX += SymbolSize * 0.3f;
		}
		#endregion
	}
}
