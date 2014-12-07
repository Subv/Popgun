using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Popgun.Input;

namespace Popgun.Screens
{
    public abstract class GameScreen
    {
        protected ContentManager Content;

        public virtual void LoadContent()
        {
            Content = new ContentManager(ScreenManager.Instance.Content.ServiceProvider, "Content");
        }

        public virtual void UnloadContent()
        {
            Content.Unload();
        }

        public virtual void Update(GameTime time)
        {
            InputManager.Instance.Update(time);
        }

        public abstract void Draw(SpriteBatch batch);

        public virtual void SetParameter(String parameter) { }
    }
}
