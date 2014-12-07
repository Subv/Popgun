using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Popgun.Screens;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.Reflection;
using Popgun.Effects;

namespace Popgun
{
    public class ScreenManager
    {
        private static ScreenManager instance;
        public static ScreenManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new ScreenManager();
                
                return instance;
            }
        }

        public Vector2 Dimensions { private set; get; }
        public ContentManager Content { private set; get; }
        public GraphicsDevice GraphicsDevice;
        private GameScreen CurrentScreen, NextScreen;
        public bool IsTransitioning { get; private set; }
        private Image TransitionImage;

        private ScreenManager()
        {
            Dimensions = new Vector2(800, 600);
            CurrentScreen = new SplashScreen();
            IsTransitioning = false;
        }

        public void ChangeScreen(String screen, String param = null)
        {
            NextScreen = Activator.CreateInstance(Type.GetType(Popgun.Namespace + ".Screens." + screen)) as GameScreen;
            NextScreen.SetParameter(param);
            IsTransitioning = true;
            TransitionImage.IsActive = true;
        }

        public void LoadContent(ContentManager content)
        {
            Content = new ContentManager(content.ServiceProvider, "Content");
            TransitionImage = new Image("Screens/TransitionImage", Vector2.Zero, Dimensions, 0.0f, "", "Fonts/Arial");
            TransitionImage.AddEffect<FadeEffect>();
            TransitionImage.LoadContent();
            CurrentScreen.LoadContent();
        }

        public void UnloadContent()
        {
            TransitionImage.UnloadContent();
            CurrentScreen.UnloadContent();
        }

        private void UpdateTransition(GameTime time)
        {
            if (!IsTransitioning)
                return;

            TransitionImage.Update(time);
            if (TransitionImage.Alpha == 1.0f)
            {
                CurrentScreen.UnloadContent();
                CurrentScreen = NextScreen;
                CurrentScreen.LoadContent();
                NextScreen = null;
            }
            else if (TransitionImage.Alpha == 0.0f)
            {
                TransitionImage.IsActive = false;
                IsTransitioning = false;
            }
        }

        public void Update(GameTime time)
        {
            // UpdateTransition has to be called before CurrentScreen is updated
            UpdateTransition(time);
            CurrentScreen.Update(time);
        }

        public void Draw(SpriteBatch batch)
        {
            CurrentScreen.Draw(batch);
            if (IsTransitioning)
                TransitionImage.Draw(batch);
        }
    }
}
