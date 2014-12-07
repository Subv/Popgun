using Popgun.Effects;
using Popgun.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Popgun.Menus
{
    [XmlInclude(typeof(MultiplayerJoinMenu))]
    [XmlInclude(typeof(HighScoresMenu))]
    public class Menu
    {
        public event EventHandler OnMenuChange;

        public String Axis;
        public List<MenuItem> Items = new List<MenuItem>();

        [XmlArrayItem(typeof(FadeEffect))]
        public List<ImageEffect> Effects = new List<ImageEffect>();

        public int ItemNumber { get; set; }

        [XmlIgnore]
        public MenuItem SelectedItem
        {
            get { return Items[ItemNumber]; }
        }

        private String id;
        public String Id
        {
            get { return id; }
            set
            {
                id = value;
                if (OnMenuChange != null)
                    OnMenuChange(this, null);
            }
        }

        public Menu()
        {
            Axis = "Y";
            ItemNumber = 0;
        }

        protected void AlignMenuItems()
        {
            Vector2 dimensions = Vector2.Zero;

            foreach (MenuItem item in Items)
                dimensions += new Vector2(item.Image.SourceRect.Width, item.Image.SourceRect.Height);

            dimensions = new Vector2((ScreenManager.Instance.Dimensions.X - dimensions.X) / 2, (ScreenManager.Instance.Dimensions.Y - dimensions.Y) / 2);

            foreach (MenuItem item in Items)
            {
                if (Axis == "X")
                    item.Image.Position = new Vector2(dimensions.X, (ScreenManager.Instance.Dimensions.Y - item.Image.SourceRect.Height) / 2);
                else if (Axis == "Y")
                    item.Image.Position = new Vector2((ScreenManager.Instance.Dimensions.X - item.Image.SourceRect.Width) / 2, dimensions.Y);

                dimensions += new Vector2(item.Image.SourceRect.Width, item.Image.SourceRect.Height);
            }
        }

        public void Transition(float alpha)
        {
            foreach (MenuItem item in Items)
            {
                item.Image.IsActive = true;
                item.Image.Alpha = alpha;
                var effect = item.Image.GetEffect<FadeEffect>();
                if (effect != null)
                {
                    if (item.Image.Alpha == 0.0f)
                        effect.Increase = true;
                    else
                        effect.Increase = false;
                }
            }
        }

        public virtual void LoadContent()
        {
            foreach (MenuItem item in Items)
            {
                item.Image.Color = Color.Black;
                item.Image.LoadContent();
                foreach (ImageEffect effect in Effects)
                    item.Image.AddEffect(effect.Clone() as ImageEffect);
            }

            AlignMenuItems();
        }

        public virtual void UnloadContent()
        {
            foreach (MenuItem item in Items)
                item.Image.UnloadContent();
        }

        public virtual void Update(GameTime time)
        {
            if (Axis == "X")
            {
                if (InputManager.Instance.KeyPressed(Keys.Right))
                    ItemNumber++;
                else if (InputManager.Instance.KeyPressed(Keys.Left))
                    ItemNumber--;
            }
            else if (Axis == "Y")
            {
                if (InputManager.Instance.KeyPressed(Keys.Down))
                    ItemNumber++;
                else if (InputManager.Instance.KeyPressed(Keys.Up))
                    ItemNumber--;
            }

            if (ItemNumber < 0)
                ItemNumber = 0;
            else if (ItemNumber > Items.Count - 1)
                ItemNumber = Items.Count - 1;

            for (int i = 0; i < Items.Count; ++i)
            {
                if (ItemNumber == i)
                    Items[i].Image.IsActive = true;
                else
                    Items[i].Image.IsActive = false;

                Items[i].Image.Update(time);
            }
        }

        public void Draw(SpriteBatch batch)
        {
            foreach (MenuItem item in Items)
                item.Image.Draw(batch);
        }
    }
}
