using Popgun.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Popgun
{
    public class Player
    {
        private Image Image;
        private Image AlternateImage;
        private Image CannonBase;
        private List<Image> Cannon = new List<Image>();
        private int CannonState = 0;
        private int CannonSwitchTimer = 0;
        private bool CannonAnimating = false;
        private const int CannonImageSwitchTime = 70;

        private const int ImageSwitchTime = 200;
        private int SwitchTimer = 0;
        private bool OtherImage = false;

        public Bubble Bullet;
        public int Score;
        private Bubble NextBullet;

        public Vector2 Position
        {
            get { return Image.Position; }
        }

        private Vector2 BulletShootPosition
        {
            get 
            {
                return Cannon[0].Position;
            }
        }

        public void LoadContent()
        {
            Score = 0;
            Image = new Image("Sprites/Player1", Vector2.Zero);
            AlternateImage = new Image("Sprites/Player2", Vector2.Zero);
            AlternateImage.LoadContent();
            Image.LoadContent();
            Image.Position = new Vector2((ScreenManager.Instance.Dimensions.X - Image.SourceRect.Width) / 2 - 100, ScreenManager.Instance.Dimensions.Y - Image.SourceRect.Height);
            AlternateImage.Position = new Vector2((ScreenManager.Instance.Dimensions.X - AlternateImage.SourceRect.Width) / 2 - 100, ScreenManager.Instance.Dimensions.Y - AlternateImage.SourceRect.Height);

            CannonBase = new Image("Sprites/CannonBase", new Vector2(Position.X + 100, Position.Y + 30));
            CannonBase.LoadContent();

            // Now load the cannon sprites
            for (int i = 1; i <= 3; ++i)
            {
                Image cannonStep = new Image("Sprites/Cannon/Cannon" + i, new Vector2(CannonBase.Position.X + 40, CannonBase.Position.Y - 50));
                cannonStep.LoadContent();
                Cannon.Add(cannonStep);
            }

            NextBullet = new Bubble(CannonBase.Position);
        }

        public void UnloadContent()
        {
            CannonBase.UnloadContent();
            Image.UnloadContent();
            AlternateImage.UnloadContent();
            NextBullet.UnloadContent();
            if (Bullet != null)
                Bullet.UnloadContent();
        }

        public void Update(GameTime time)
        {
            if (InputManager.Instance.LeftMouseButtonClicked())
            {
                SwitchTimer = ImageSwitchTime;
                OtherImage = true;
                CannonAnimating = true;
                CannonSwitchTimer = CannonImageSwitchTime;
                CannonState = 1;
            }

            if (OtherImage)
            {
                AlternateImage.Update(time);
                SwitchTimer -= time.ElapsedGameTime.Milliseconds;
                if (SwitchTimer <= 0)
                    OtherImage = false;
            }
            else
                Image.Update(time);

            CannonBase.Update(time);

            // Now update the cannon animation
            if (CannonAnimating)
            {
                CannonSwitchTimer -= time.ElapsedGameTime.Milliseconds;
                if (CannonSwitchTimer <= 0)
                {
                    CannonSwitchTimer = CannonImageSwitchTime;
                    CannonState = (CannonState + 1) % Cannon.Count;
                    if (CannonState == 0)
                    {
                        CannonAnimating = false;
                        Shoot();
                    }
                }
            }

            Cannon[CannonState].Update(time);

            if (Bullet != null)
                Bullet.Update(time);
            NextBullet.Update(time);
        }

        public void Draw(SpriteBatch batch)
        {
            if (OtherImage)
                AlternateImage.Draw(batch);
            else
                Image.Draw(batch);
            if (Bullet != null)
                Bullet.Draw(batch);

            // Rotate the cannon in the direction of the mouse
            Vector2 mouseDir = new Vector2(Mouse.GetState().Position.X, Mouse.GetState().Position.Y) - (BulletShootPosition + new Vector2(0, Cannon[CannonState].SourceRect.Height));
            mouseDir.Normalize();

            Cannon[CannonState].Rotation = (float)Math.PI / 2 + (float)Math.Atan2(mouseDir.Y, mouseDir.X);
            Cannon[CannonState].Draw(batch);
            CannonBase.Draw(batch);
            NextBullet.Draw(batch);
        }

        public void Shoot()
        {
            if (Bullet != null)
                return;

            // Load the next bullet, move it to the starting position, and launch it
            Bullet = NextBullet;
            Bullet.Position = BulletShootPosition;
            Vector2 mouseDir = new Vector2(Mouse.GetState().Position.X, Mouse.GetState().Position.Y) - BulletShootPosition;
            mouseDir.Normalize();
            Bullet.Velocity = mouseDir;

            // Now recreate the next bullet
            NextBullet = new Bubble(CannonBase.Position);
        }
    }
}
