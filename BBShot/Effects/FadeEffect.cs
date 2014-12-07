using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using System.Xml.Serialization;

namespace Popgun.Effects
{
    public class FadeEffect : ImageEffect
    {
        public uint FadeSpeed = 1;
        public bool Increase = true;

        public FadeEffect()
        {

        }

        public override void Update(GameTime time)
        {
            if (!Image.IsActive)
            {
                Image.Alpha = 1.0f;
                return;
            }

            if (Increase)
                Image.Alpha += FadeSpeed * (float)time.ElapsedGameTime.TotalSeconds;
            else
                Image.Alpha -= FadeSpeed * (float)time.ElapsedGameTime.TotalSeconds;

            if (Image.Alpha > 1.0f)
            {
                Image.Alpha = 1.0f;
                Increase = false;
            }
            else if (Image.Alpha < 0.0f)
            {
                Image.Alpha = 0.0f;
                Increase = true;
            }
        }
    }
}
