﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;

using Platformer;


namespace Platformer.Tiles
{
    abstract class Activator
    {

        protected Boolean on = false;
        protected List<IActivatable> list { get; set; }
        protected Vector2 position;

        protected Texture2D activated;
        protected Texture2D deactivated;
        protected Vector2 origin;

        public RectangleF BoundingRectangle
        {
            get
            {
                int left = (int)Math.Round(position.X - origin.X);
                int top = (int)Math.Round(position.Y - origin.Y);

                return new RectangleF(left, top, 32, 32);
            }
        }

        public Activator(Vector2 location)
        {
            this.position = location;
            this.list = new List<IActivatable>();
        }

        public void remove(IActivatable responder)
        {
            list.Remove(responder);
        }

        /**
         * Add a new Activatable to this Activator
         **/
        public void add(IActivatable responder)
        {
            list.Add(responder);
        }

        /**
         * Draws this activatable
         **/
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (on)
                spriteBatch.Draw(activated, position, null, Color.White, 0.0f, origin, 1.0f, SpriteEffects.None, 0.0f);
            else
                spriteBatch.Draw(deactivated, position, null, Color.White, 0.0f, origin, 1.0f, SpriteEffects.None, 0.0f);
        }

        #region abstract methods
        //change state sets the activator to on and off depending on players position and keystrokes
        public abstract void ChangeState(Player c, KeyboardState keyState, InputManager inputManager);
        #endregion
    }
}


        