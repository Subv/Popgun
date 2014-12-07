using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Popgun.Effects;
using Microsoft.Xna.Framework.Input;
using Popgun.Input;
using System.Timers;

namespace Popgun.Screens
{
    public class SplashScreen : GameScreen
    {
        private Image Image = new Image("Screens/Splash/Splash", Vector2.Zero);
        private Timer timer = new Timer(3000);

        public override void LoadContent()
        {
            timer.Elapsed += (src, e) => ScreenManager.Instance.ChangeScreen("TitleScreen");
            timer.AutoReset = false;
            timer.Enabled = true;

            base.LoadContent();
            Image.LoadContent();
        }

        public override void UnloadContent()
        {
            Image.UnloadContent();
            base.UnloadContent();
        }

        public override void Update(GameTime time)
        {
            Image.Update(time);
            base.Update(time);

            if (InputManager.Instance.KeyPressed(Keys.P, Keys.Enter))
            {
                timer.Enabled = false;
                ScreenManager.Instance.ChangeScreen("TitleScreen");
            }
        }

        public override void Draw(SpriteBatch batch)
        {
            Image.Draw(batch);
        }
    }
}
