using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Popgun.Screens;

namespace Popgun
{
    public class Bubble
    {
        public const int TopLeft = 0;
        public const int TopRight = 1;
        public const int Left = 2;
        public const int Right = 3;
        public const int BottomLeft = 4;
        public const int BottomRight = 5;

        public const int VerticalTolerance = 8;

        public const float MovementStep = 1f;

        public enum ColorIndices
        {
            Red,
            Blue,
            Green,
            Magenta,
            Yellow
        }
        public static Color[] AvailableColors = { Color.Red, Color.Blue, Color.Green, Color.Magenta, Color.Yellow };
        public static String GetFileName(int color)
        {
            return ((ColorIndices)color).ToString();
        }

        public Image Image;
        public bool Visible;
        public bool Offsetted;
        public Vector2 Velocity;
        public Color Color;
        public int ColorIndex;
        
        private Vector2 position;
        public Vector2 Position
        {
            get { return position; }
            set
            {
                position = value;
                if (Image != null)
                    Image.Position = value;
            }
        }

        public Bubble[] Neighbors;
        public List<Bubble> UnknownConnections = new List<Bubble>();

        public Bubble(Vector2 position)
        {
            Visible = true;
            Position = position;
            Velocity = Vector2.Zero;
            ColorIndex = GameplayScreen.Random.Next(AvailableColors.Length);
            Color = AvailableColors[ColorIndex];
            Neighbors = new Bubble[6];

            LoadContent();
        }

        // Returns true if it should be on the right
        private bool GetHorizontalContact(Bubble other)
        {
            if (other.Position.X <= Position.X + Image.SourceRect.Width / 2)
                return false;
            return true;
        }

        public int GetContactDirection(Bubble other)
        {
            // Check if it should be at the top
            if (other.Position.Y - other.Image.SourceRect.Height / 2 + VerticalTolerance <= Position.Y)
            {
                if (GetHorizontalContact(other))
                    return TopRight;
                return TopLeft;
            }
            else if (other.Position.Y + other.Image.SourceRect.Height - VerticalTolerance >= Position.Y + Image.SourceRect.Height) // Check if it should be at the bottom
            {
                if (GetHorizontalContact(other))
                    return BottomRight;
                return BottomLeft;
            }
            else // It should be right next to it
            {
                if (GetHorizontalContact(other))
                    return Right;
                return Left;
            }
        }

        public void LoadContent()
        {
            Image = new Image("Sprites/Bubbles/" + GetFileName(ColorIndex), Position);
            Image.LoadContent();
        }

        public void UnloadContent()
        {
            Image.UnloadContent();
        }

        public Rectangle GetRectangle()
        {
            Rectangle rect = Image.SourceRect;
            Vector2 init = Image.Position + Image.Origin;
            rect.Location = new Point((int)init.X, (int)init.Y);
            return rect;
        }

        public bool Contains(Vector2 position)
        {
            return Image.Contains(position);
        }

        public bool Contains(Rectangle rect)
        {
            return Image.Contains(rect);
        }

        public bool Intersects(Bubble bubble)
        {
            return Image.Intersects(bubble.Image);
        }

        public void Update(GameTime time)
        {
            var pos = Position;
            pos.X += Velocity.X * time.ElapsedGameTime.Milliseconds * MovementStep;
            pos.Y += Velocity.Y * time.ElapsedGameTime.Milliseconds * MovementStep;
            Position = pos;
        }

        public void Draw(SpriteBatch batch)
        {
            if (Visible)
                Image.Draw(batch);
        }

        public override string ToString()
        {
            String ret = "Visible: " + Visible + "\n Color: " + Color + "\n";
            for (int i = TopLeft; i <= BottomRight; ++i)
                ret += (Neighbors[i] != null ? String.Format("Neighbors[{0}] = {1}", i, Neighbors[i].Color) : "null") + "\n";

            return ret;
        }
    }
}
