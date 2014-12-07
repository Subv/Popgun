using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Popgun.Serialization;
using Popgun.Input;
using Microsoft.Xna.Framework.Input;

namespace Popgun.Menus
{
    public class MenuManager
    {
        private Menu CurrentMenu;
        private bool IsTransitioning;

        public MenuManager()
        {
            CurrentMenu = new Menu();
            CurrentMenu.OnMenuChange += CurrentMenu_OnMenuChange;
        }

        void CurrentMenu_OnMenuChange(object sender, EventArgs e)
        {
            CurrentMenu.UnloadContent();
            var newMenu = Serializer<Menu>.Load(CurrentMenu.Id);
            newMenu.LoadContent();
            CurrentMenu = newMenu;
            CurrentMenu.OnMenuChange += CurrentMenu_OnMenuChange;
            CurrentMenu.Transition(0.0f);
        }

        public void LoadContent(String path)
        {
            if (path != String.Empty)
                CurrentMenu.Id = path;
        }

        public void UnloadContent()
        {
            CurrentMenu.UnloadContent();
        }

        private void UpdateTransition(GameTime time)
        {
            if (!IsTransitioning)
                return;

            // Save the previous count, CurrentMenu will change inside the loop
            int count = CurrentMenu.Items.Count;
            for (int i = 0; i < count; ++i)
            {
                CurrentMenu.Items[i].Image.Update(time);
                float first = CurrentMenu.Items[0].Image.Alpha;
                float last = CurrentMenu.Items[CurrentMenu.Items.Count - 1].Image.Alpha;
                if (first == 0.0f && last == 0.0f)
                    CurrentMenu.Id = CurrentMenu.SelectedItem.LinkID;
                else if (first == 1.0f && last == 1.0f)
                    IsTransitioning = false;
            }
        }

        public void Update(GameTime time)
        {
            if (!IsTransitioning)
                CurrentMenu.Update(time);

            if (InputManager.Instance.KeyPressed(Keys.Enter))
            {
                if (CurrentMenu.SelectedItem.LinkType == MenuItem.LinkTypes.Exit)
                {
                    Environment.Exit(0);
                    return;
                }

                if (CurrentMenu.SelectedItem.LinkType == MenuItem.LinkTypes.EnterIP)
                {
                    var form = new EnterIPForm();
                    form.ShowDialog();
                    var ip = form.GetIP();
                    ScreenManager.Instance.ChangeScreen("GameplayScreen", "multijoin;" + ip.ToString());
                }

                if (CurrentMenu.SelectedItem.LinkType == MenuItem.LinkTypes.Screen)
                    ScreenManager.Instance.ChangeScreen(CurrentMenu.SelectedItem.LinkID, CurrentMenu.SelectedItem.Parameter);
                else if (CurrentMenu.SelectedItem.LinkType == MenuItem.LinkTypes.Menu)
                {
                    IsTransitioning = true;
                    CurrentMenu.Transition(1.0f);
                }
            }

            UpdateTransition(time);
        }

        public void Draw(SpriteBatch batch)
        {
            CurrentMenu.Draw(batch);
        }
    }
}
