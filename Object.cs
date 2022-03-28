﻿using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;

namespace SMPL
{
	public class Object
	{
		private readonly List<Object> children = new();
		private Object parent;
		private Vector2 localPos;
		private float localAng, localSc;
		private Matrix3x2 global;

		/// <summary>
		/// Direction relative to <see cref="Parent"/>.
		/// </summary>
		public Vector2 LocalDirection
		{
			get => Vector2.Normalize(LocalAngle.AngleToDirection());
			set => LocalAngle = Vector2.Normalize(value).DirectionToAngle();
		}
		/// <summary>
		/// Direction in the world (where this <see cref="Object"/> is facing towards as a unit vector).
		/// </summary>
		public Vector2 Direction
		{
			get => Vector2.Normalize(Angle.AngleToDirection());
			set => Angle = Vector2.Normalize(value).DirectionToAngle();
		}

		/// <summary>
		/// Position relative to <see cref="Parent"/>.
		/// </summary>
		public Vector2 LocalPosition
		{
			get => localPos;
			set { localPos = value; UpdateSelfAndChildren(); }
		}
		/// <summary>
		/// Scale relative to <see cref="Parent"/>.
		/// </summary>
		public float LocalScale
		{
			get => localSc;
			set { localSc = value; UpdateSelfAndChildren(); }
		}
		/// <summary>
		/// Angle relative to <see cref="Parent"/>.
		/// </summary>
		public float LocalAngle
		{
			get => localAng;
			set { localAng = value; UpdateSelfAndChildren(); }
		}

		/// <summary>
		/// Position in the world.
		/// </summary>
		public Vector2 Position
		{
			get => GetPosition(global);
			set => LocalPosition = GetLocalPosition(value);
		}
		/// <summary>
		/// Scale in the world. This is used with some size value (for example <see cref="Sprite.LocalSize"/>).
		/// </summary>
		public float Scale
		{
			get => GetScale(global);
			set => LocalScale = GetScale(GlobalToLocal(value, LocalAngle, LocalPosition));
		}
		/// <summary>
		/// Angle in the world (where this <see cref="Object"/> is facing towards in 0-360 degrees).
		/// </summary>
		public float Angle
		{
			get => GetAngle(global);
			set => LocalAngle = GetAngle(GlobalToLocal(LocalScale, value, LocalPosition));
		}

		/// <summary>
		/// Having a <see cref="Parent"/> will make this <see cref="Object"/> move, rotate and scale as if they are one <see cref="Object"/>.
		/// </summary>
		public Object Parent
		{
			get => parent;
			set
			{
				if (parent != value && parent != null)
					parent.children.Remove(this);

				var prevPos = Position;
				var prevAng = Angle;
				var prevSc = Scale;

				parent = value;

				if (parent != null)
					parent.children.Add(this);

				Position = prevPos;
				Angle = prevAng;
				Scale = prevSc;
			}
		}
		/// <summary>
		/// See <see cref="Parent"/> for info.
		/// </summary>
		public ReadOnlyCollection<Object> Children => children.AsReadOnly();

		/// <summary>
		/// Transform a world <paramref name="position"/> into a position that's relative to <see cref="Parent"/>.
		/// </summary>
		public Vector2 GetLocalPosition(Vector2 position)
		{
			return GetPosition(GlobalToLocal(LocalScale, LocalAngle, position));
		}
		/// <summary>
		/// Transform a <paramref name="localPosition"/> (relative to <see cref="Parent"/>) to a position in the world.
		/// </summary>
		public Vector2 GetPosition(Vector2 localPosition)
		{
			return GetPosition(LocalToGlobal(LocalScale, LocalAngle, localPosition));
		}

		public Object()
		{
			LocalScale = 1;
		}

		private void UpdateSelfAndChildren()
		{
			UpdateGlobalMatrix();

			for (int i = 0; i < children.Count; i++)
				children[i].UpdateGlobalMatrix();
		}
		private void UpdateGlobalMatrix()
		{
			global = LocalToGlobal(LocalScale, LocalAngle, LocalPosition);
		}

		private Matrix3x2 LocalToGlobal(float localScale, float localAngle, Vector2 localPosition)
		{
			var c = Matrix3x2.Identity;
			c *= Matrix3x2.CreateTranslation(localPosition);
			c *= Matrix3x2.CreateRotation(localAngle.DegreesToRadians());
			c *= Matrix3x2.CreateScale(localScale, localScale);

			var p = Matrix3x2.Identity;
			if (parent != null)
			{
				p *= Matrix3x2.CreateScale(parent.Scale);
				p *= Matrix3x2.CreateRotation(parent.Angle.DegreesToRadians());
				p *= Matrix3x2.CreateTranslation(parent.Position);
			}
			return c * p;
		}
		private Matrix3x2 GlobalToLocal(float scale, float angle, Vector2 position)
		{
			var c = Matrix3x2.Identity;
			c *= Matrix3x2.CreateScale(scale, scale);
			c *= Matrix3x2.CreateRotation(angle.DegreesToRadians());
			c *= Matrix3x2.CreateTranslation(position);

			var inverseParent = Matrix3x2.Identity;
			if (parent != null)
			{
				Matrix3x2.Invert(Matrix3x2.CreateScale(parent.Scale), out var s);
				Matrix3x2.Invert(Matrix3x2.CreateRotation(parent.Angle.DegreesToRadians()), out var r);
				Matrix3x2.Invert(Matrix3x2.CreateTranslation(parent.Position), out var t);

				inverseParent *= t;
				inverseParent *= r;
				inverseParent *= s;
			}

			return c * inverseParent;
		}

		private static float GetAngle(Matrix3x2 matrix)
		{
			return MathF.Atan2(matrix.M12, matrix.M11).RadiansToDegrees();
		}
		private static Vector2 GetPosition(Matrix3x2 matrix)
		{
			return new(matrix.M31, matrix.M32);
		}
		private static float GetScale(Matrix3x2 matrix)
		{
			return MathF.Sqrt(matrix.M11 * matrix.M11 + matrix.M12 * matrix.M12);
			//return new(MathF.Sqrt(matrix.M11 * matrix.M11 + matrix.M12 * matrix.M12),
			//	MathF.Sqrt(matrix.M21 * matrix.M21 + matrix.M22 * matrix.M22));
		}
	}
}
