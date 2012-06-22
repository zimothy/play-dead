﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;

using Platformer.Tiles;
using Platformer.Levels;

namespace Platformer.Tiles
{
    class WaterSource : Tile, IActivatable
    {
        /*public int WaterLevel
        {
            get { return waterLevel; }
            set
            {
                if (value > 0)
                    waterLevel = value;
                else
                    waterLevel = 0;
            }
        }*/
        private int waterLevel = 1;

        private Sprite emptySprite;
        private Sprite fullSprite;
        private Level level;

        public WaterSource(Sprite emptySprite, Sprite fullSprite)
            : base(fullSprite, TileCollision.Water)
        {
            this.emptySprite = emptySprite;
            this.fullSprite = fullSprite;
        }

        public void increaseWaterLevel()
        {
            waterLevel++;
        }

        public void bindToLevel(Level level)
        {
            this.level = level;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            Vector2 gridPos = level.getGridPosition(Sprite.X, Sprite.Y);
            int currentCol = (int)gridPos.X;
            int currentRow = (int)gridPos.Y;

            // Fill the world upwards, row by row, with water.
            for (int row = 0; row < waterLevel; row++)
            {
                // Fill left
                fillRow(currentCol    , currentRow - row, true);

                // Fill right
                fillRow(currentCol + 1, currentRow - row, false);
            }
        }

        private void fillRow(int col, int row, bool lookLeft)
        {
            Tile tile = level.getTile(col, row);

            while (tile.Collision == TileCollision.Passable)
            {
                tile.Collision = TileCollision.Water;
                tile.Sprite.Texture = fullSprite.Texture;

                if (lookLeft)
                    col--;
                else
                    col++;

                if (level.isTileInBounds(col, row))
                {
                    tile = level.getTile(col, row);
                }
                else
                {
                    break;
                }
            }
        }

        public bool IsActive()
        {
            // Nothing to do here
            return true;
        }

        public void SetState(bool active)
        {
            increaseWaterLevel();
        }

        public void ChangeState()
        {
            increaseWaterLevel();
        }
    }
}
