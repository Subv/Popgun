using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Popgun.Input;
using Popgun.Menus;
using Microsoft.Xna.Framework.Input;
using Popgun.Serialization;

namespace Popgun.Screens
{
    public class TitleScreen : GameScreen
    {
        private MenuManager MenuManager = new MenuManager();
        private Image Background = new Image("Screens/Title/Background", Vector2.Zero);

        public override void LoadContent()
        {
            base.LoadContent();
            Background.LoadContent();
            MenuManager.LoadContent("Xml/Menus/MainMenu.xml");
        }

        public override void UnloadContent()
        {
            base.UnloadContent();
            Background.UnloadContent();
            MenuManager.UnloadContent();
        }

        public override void Update(GameTime time)
        {
            base.Update(time);
            Background.Update(time);
            MenuManager.Update(time);
        }

        public override void Draw(SpriteBatch batch)
        {
            Background.Draw(batch);
            MenuManager.Draw(batch);
        }
    }
}
