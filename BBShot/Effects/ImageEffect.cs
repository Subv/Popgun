using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Popgun.Effects
{
    public abstract class ImageEffect : ICloneable
    {
        public Image Image;
        public bool Active;

        public ImageEffect()
        {
            Active = true;
        }

        public virtual void LoadContent(Image image)
        {
            Image = image;
        }

        public virtual void UnloadContent()
        {
            Active = false;
        }

        public abstract void Update(GameTime time);

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
