using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Popgun.Effects;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.Collections.Concurrent;

namespace Popgun
{
    public class Image
    {
        private static ConcurrentDictionary<String, Texture2D> TextureCache = new ConcurrentDictionary<string, Texture2D>();
        private Texture2D Texture;
        private RenderTarget2D RenderTarget;
        public Vector2 Position, Scale, Origin;
        public float Alpha;
        public String Text, FontName, Path;
        private SpriteFont Font;
        public Rectangle SourceRect { get; set; }
        private ContentManager Content;
        public bool IsActive;
        public Color Color = Color.White;
        public float Rotation;

        private Dictionary<Type, ImageEffect> Effects;

        public Image()
        {
            IsActive = false;
            Scale = Vector2.One;
            SourceRect = Rectangle.Empty;
            Path = String.Empty;
            Position = Vector2.Zero;
            Text = String.Empty;
            FontName = "Fonts/Arial";
            Alpha = 1.0f;
            Effects = new Dictionary<Type, ImageEffect>();
        }
        
        public Image(String path, Vector2 position)
        {
            IsActive = false;
            Scale = Vector2.One;
            SourceRect = Rectangle.Empty;
            Path = path;
            Position = position;
            Text = String.Empty;
            FontName = "Fonts/Arial";
            Alpha = 1.0f;
            Effects = new Dictionary<Type, ImageEffect>();
        }

        public Image(String path, Vector2 position, Vector2 scale, float alpha = 1.0f, String text = "", String fontName = "Fonts/Arial")
        {
            IsActive = false;
            Scale = scale;
            SourceRect = Rectangle.Empty;
            Path = path;
            Position = position;
            Text = text;
            FontName = fontName;
            Alpha = alpha;
            Effects = new Dictionary<Type, ImageEffect>();
        }

        #region Effects
        public void AddEffect<T>() where T : ImageEffect
        {
            Effects[typeof(T)] = (ImageEffect)Activator.CreateInstance(typeof(T));
            Effects[typeof(T)].LoadContent(this);
        }

        public void AddEffect(ImageEffect type)
        {
            Effects[type.GetType()] = type;
            type.LoadContent(this);
        }

        public T GetEffect<T>() where T : ImageEffect
        {
            if (Effects.ContainsKey(typeof(T)))
                return Effects[typeof(T)] as T;
            return null;
        }

        public void StopEffect<T>() where T : ImageEffect
        {
            T effect = GetEffect<T>();
            if (effect != null)
                effect.UnloadContent();
        }

        public void StopAllEffects()
        {
            foreach (var effect in Effects)
                effect.Value.UnloadContent();
        }
        #endregion

        public void LoadContent()
        {
            Content = new ContentManager(ScreenManager.Instance.Content.ServiceProvider, "Content");

            if (Path != String.Empty)
            {
                if (TextureCache.ContainsKey(Path))
                    Texture = TextureCache[Path];
                else
                {
                    Texture = Content.Load<Texture2D>(Path);
                    TextureCache.TryAdd(Path, Texture);
                }
            }
            
            Font = Content.Load<SpriteFont>(FontName);

            Vector2 dimensions = Vector2.Zero;

            if (Texture != null)
            {
                dimensions.X = Texture.Width;
                dimensions.Y = Texture.Height;
                if (Text != String.Empty)
                    dimensions.Y = Math.Max(Font.MeasureString(Text).Y, Texture.Height);
            }
            else
                dimensions.Y = Font.MeasureString(Text).Y;

            if (Text != String.Empty)
                dimensions.X += Font.MeasureString(Text).X;

            SourceRect = new Rectangle(0, 0, (int)dimensions.X, (int)dimensions.Y);
            Origin = new Vector2(SourceRect.Width / 2, SourceRect.Height / 2);

            RenderTarget = new RenderTarget2D(ScreenManager.Instance.GraphicsDevice, (int)dimensions.X, (int)dimensions.Y);
            SpriteBatch batch = new SpriteBatch(ScreenManager.Instance.GraphicsDevice);
            // Setup our rendertarget
            ScreenManager.Instance.GraphicsDevice.SetRenderTarget(RenderTarget);
            ScreenManager.Instance.GraphicsDevice.Clear(Color.Transparent);
            batch.Begin();
            if (Texture != null)
                batch.Draw(Texture, Vector2.Zero, Color.White);
            if (Text != String.Empty)
                batch.DrawString(Font, Text, Vector2.Zero, Color.White);
            batch.End();

            // Reset it to the default rendertarget
            ScreenManager.Instance.GraphicsDevice.SetRenderTarget(null);
        }

        public void UnloadContent()
        {
            //Content.Unload();
            RenderTarget.Dispose();
            StopAllEffects();
        }

        public void Update(GameTime time)
        {
            foreach (var effect in Effects)
                if (effect.Value.Active)
                    effect.Value.Update(time);
        }

        public bool Contains(Vector2 point)
        {
            Rectangle rect = SourceRect;
            Vector2 init = Position + Origin;
            rect.Location = new Point((int)init.X, (int)init.Y);
            return rect.Contains(point);
        }

        public bool Contains(Rectangle other)
        {
            Rectangle rect = SourceRect;
            Vector2 init = Position + Origin;
            rect.Location = new Point((int)init.X, (int)init.Y);
            return rect.Contains(other);
        }

        public bool Intersects(Image image)
        {
            Rectangle rect1 = SourceRect;
            Vector2 init = Position + Origin;
            rect1.Location = new Point((int)init.X, (int)init.Y);

            Rectangle rect2 = image.SourceRect;
            Vector2 init2 = image.Position + image.Origin;
            rect2.Location = new Point((int)init2.X, (int)init2.Y);
            return rect1.Intersects(rect2);
        }

        public void Draw(SpriteBatch batch)
        {
            batch.Draw(RenderTarget, Position + Origin, SourceRect, Color * Alpha, Rotation, Origin, Scale, SpriteEffects.None, 0.0f);
        }
    }
}
