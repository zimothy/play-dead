#region File Description
//-----------------------------------------------------------------------------
// Player.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

using Platformer.Camera;
using Platformer.Tiles;
using Platformer.Levels;
using Microsoft.Xna.Framework.Content;

namespace Platformer
{
    /// <summary>
    /// Different types of death cause for OnKilled() function
    /// </summary>
    enum DeathType
    {
        Default = 0,
        Water = 1,
        Spike = 2,
        Fall = 3,
    }

    /// <summary>
    /// Our fearless adventurer!
    /// </summary>
    class Player : ICameraTrackable
    {
        // Animations
        private Animation idleRightAnimation;
        private Animation idleLeftAnimation;
        private Animation runRightAnimation;
        private Animation runLeftAnimation;
        private Animation jumpRightAnimation;
        private Animation jumpLeftAnimation;
        private Animation dieRightAnimation;
        private Animation dieLeftAnimation;
        private Animation drownRightAnimation;
        private Animation drownLeftAnimation;
        private Animation ladderUpAnimation;
        private Animation ladderDownAnimation;
        private Animation fallRightAnimation;
        private Animation fallLeftAnimation;
        private AnimationPlayer sprite;

        bool isRight = true;

        //Flag to make sure it only plays the death sound once
        private bool deathPlayed = false;

        private DynamicMap dynamicMap;

        // Sounds
        private SoundEffect killedSound;
        private SoundEffect jumpSound;
        private SoundEffect fallSound;

        // New Sounds
        private SoundEffect spikeImpaleSound;
        private SoundEffect waterDrownSound;
        private SoundEffect fallImpactSound;

        //Ladder
        private bool isClimbing;
        public bool IsClimbing
        {
            get { return isClimbing; }
        }
        private bool wasClimbing;
        private const int LadderAlignment = 12;


        public Level Level
        {
            get { return level; }
        }
        Level level;

        public bool IsAlive
        {
            get { return isAlive; }
        }
        bool isAlive;

        // Physics state
        public Vector2 Position
        {
            get { return position; }
            set { position = value; }
        }
        Vector2 position;

        private float previousBottom;

        public Vector2 Velocity
        {
            get { return velocity; }
            set { velocity = value; }
        }
        Vector2 velocity;

        // Constants for controling horizontal movement
        private const float MoveAcceleration = 13000.0f;
        private const float MaxMoveSpeed = 1750.0f;
        private const float GroundDragFactor = 0.48f;
        private const float AirDragFactor = 0.58f;

        // Constants for controlling vertical movement
        private const float MaxJumpTime = 0.35f;
        private const float JumpLaunchVelocity = -3500.0f;
        private const float GravityAcceleration = 3400.0f;
        private const float MaxFallSpeed = 550.0f;
        private const float JumpControlPower = 0.14f; 

        // Input configuration
        private const float MoveStickScale = 1.0f;
        private const float AccelerometerScale = 1.5f;
        private const Buttons JumpButton = Buttons.A;

        // Variables to judge fall distance for fall damage
        public const int MAX_SAFE_FALL_DISTANCE = Tile.Height * 6;
        private Vector2 lastGroundPos;

        /// <summary>
        /// Gets whether or not the player's feet are on the ground.
        /// </summary>
        public bool IsOnGround
        {
            get { return isOnGround; }
        }
        bool isOnGround;

        /// <summary>
        /// Current user movement input.
        /// Changed to a vector2 for vertical movement (ladder movement)
        /// </summary>
        //private float movement;
        private Vector2 movement;

        // Jumping state
        private bool isJumping;
        private bool wasJumping;
        private float jumpTime;

        private RectangleF localBounds;
        /// <summary>
        /// Gets a rectangle which bounds this player in world space.
        /// </summary>
        public RectangleF BoundingRectangle
        {
            get
            {
                float left = Position.X - sprite.Origin.X + localBounds.X;
                float top = Position.Y - sprite.Origin.Y + localBounds.Y;

                return new RectangleF(left, top, localBounds.Width, localBounds.Height);
            }
        }

        /// <summary>
        /// The bounds for collision detection.
        /// Exactly like BoundingRectangle, but returns an int-rectangle for compatibility.
        /// </summary>
        public Rectangle CollideBounds
        {
            get
            {
                int left = (int)Math.Round(Position.X - sprite.Origin.X + localBounds.X); 
                int top = (int)Math.Round(Position.Y - sprite.Origin.Y + localBounds.Y); 

                return new Rectangle(left, top, (int)localBounds.Width, (int)localBounds.Height);
            }
        }

        /// <summary>
        /// Constructors a new player.
        /// </summary>
        public Player(ContentManager content, Vector2 position, DynamicMap dynamicMap)
        {
            LoadContent(content);

            Reset(position);

            this.dynamicMap = dynamicMap;
        }

        public void GotoLevel(int levelIndex)
        {
            dynamicMap.GotoLevel(levelIndex);
        }

        public void EnterLevel(Level level)
        {
            this.level = level;
            Position = level.ActiveSpawn.Position;
            lastGroundPos = position;
            Velocity = Vector2.Zero;
        }


        /// <summary>
        /// Loads the player sprite sheet and sounds.
        /// </summary>
        public void LoadContent(ContentManager content)
        {
            // Load animated textures.
            idleRightAnimation = new Animation(content.Load<Texture2D>("Sprites/Player/Idle-Right"), 0.1f, true);
            idleLeftAnimation = new Animation(content.Load<Texture2D>("Sprites/Player/Idle-Left"), 0.1f, true);
            runRightAnimation = new Animation(content.Load<Texture2D>("Sprites/Player/Walk-Right"), 0.1f, true);
            runLeftAnimation = new Animation(content.Load<Texture2D>("Sprites/Player/Walk-Left"), 0.1f, true);
            fallRightAnimation = new Animation(content.Load<Texture2D>("Sprites/Player/Fall-Right"), 0.15f, true);
            fallLeftAnimation = new Animation(content.Load<Texture2D>("Sprites/Player/Fall-Left"), 0.15f, true);
            jumpRightAnimation = new Animation(content.Load<Texture2D>("Sprites/Player/Jump-Right"), 0.05f, delegate
            {
                sprite.PlayAnimation(fallRightAnimation);
            });
            jumpLeftAnimation = new Animation(content.Load<Texture2D>("Sprites/Player/Jump-Left"), 0.05f, delegate
            {
                sprite.PlayAnimation(fallLeftAnimation);
            });
            dieRightAnimation = new Animation(content.Load<Texture2D>("Sprites/Player/Spiked-Right"), 0.05f, false);
            dieLeftAnimation = new Animation(content.Load<Texture2D>("Sprites/Player/Spiked-Left"), 0.05f, false);
            drownRightAnimation = new Animation(content.Load<Texture2D>("Sprites/Player/Drown-Right"), 0.1f, true);
            drownLeftAnimation = new Animation(content.Load<Texture2D>("Sprites/Player/Drown-Left"), 0.1f, true);

            ladderUpAnimation = new Animation(content.Load<Texture2D>("Sprites/Player/Ladder"), 0.15f, true);
            ladderDownAnimation = new Animation(content.Load<Texture2D>("Sprites/Player/Ladder"), 0.1f, true);

            // Calculate bounds within texture size.            
            float width = idleLeftAnimation.FrameWidth * 0.4f;
            float left = (idleLeftAnimation.FrameWidth - width) / 2;
            float height = idleLeftAnimation.FrameWidth * 0.8f;
            float top = idleLeftAnimation.FrameHeight - height;
            localBounds = new RectangleF(left, top, width, height);

            // Load sounds.            
            killedSound = content.Load<SoundEffect>("Sounds/PlayerKilled");
            jumpSound = content.Load<SoundEffect>("Sounds/PlayerJump");
            fallSound = content.Load<SoundEffect>("Sounds/PlayerFall");

            spikeImpaleSound = content.Load<SoundEffect>("Sounds/PlayerKilledSpike");
            //spikeImpaleSound = jumpSound;
            waterDrownSound = content.Load<SoundEffect>("Sounds/PlayerKilledWater");            
            fallImpactSound = content.Load<SoundEffect>("Sounds/PlayerKilledFall");
            //fallImpactSound = jumpSound;
            
        }

        /// <summary>
        /// Resets the player to life.
        /// </summary>
        /// <param name="position">The position to come to life at.</param>
        public void Reset(Vector2 position)
        {
            Position = position;
            lastGroundPos = position;
            Velocity = Vector2.Zero;
            isAlive = true;
            deathPlayed = false;
            isRight = true;
            sprite.PlayAnimation(idleRightAnimation);
        }

        public Vector2 getPosition()
        {
            return position;
        }

        private void idle()
        {
            if (isRight)
            {
                sprite.PlayAnimation(idleRightAnimation);
            }
            else
            {
                sprite.PlayAnimation(idleLeftAnimation);
            }
        }

        /// <summary>
        /// Handles input, performs physics, and animates the player sprite.
        /// </summary>
        /// <remarks>
        /// We pass in all of the input states so that our game is only polling the hardware
        /// once per frame. We also pass the game's orientation because when using the accelerometer,
        /// we need to reverse our motion when the orientation is in the LandscapeRight orientation.
        /// </remarks>
        public void Update(
            GameTime gameTime, 
            KeyboardState keyboardState, 
            GamePadState gamePadState, 
            TouchCollection touchState, 
            AccelerometerState accelState,
            DisplayOrientation orientation,
            InputManager inputManager)
        {
            
            // Hook for InputManager
            inputManager.Update();

            if (IsAlive)
            {
                GetInput(keyboardState, gamePadState, touchState, accelState, orientation, inputManager);
            }

            ApplyPhysics(gameTime);

            if (IsAlive)
            {
                if (isOnGround)
                {
                    if (Math.Abs(Velocity.X) - 0.02f > 0)
                    {
                        if (Velocity.X > 0)
                        {
                            sprite.PlayAnimation(runRightAnimation);
                            isRight = true;
                        }
                        else
                        {
                            sprite.PlayAnimation(runLeftAnimation);
                            isRight = false;
                        }
                    }
                    else
                    {
                        idle();
                    }
                }
                else if (isClimbing)
                {
                    if (Velocity.Y > 0.02f)
                    {
                        sprite.PlayAnimation(ladderDownAnimation);
                    }
                    else if (Velocity.Y < 0.02f)
                    {
                        sprite.PlayAnimation(ladderUpAnimation);
                    }
                    
                    if (Math.Abs(Velocity.Y) <= 0.02f)
                    {
                        sprite.Pause();
                    }
                }
                else if (!isJumping)
                {
                    if (Velocity.X > 0)
                    {
                        sprite.PlayAnimation(fallRightAnimation);
                    }
                    else if (Velocity.X < 0)
                    {
                        sprite.PlayAnimation(fallLeftAnimation);
                    }
                    else
                    {
                        sprite.PlayAnimation(isRight ? fallRightAnimation : fallLeftAnimation);
                    }
                }
            }

            // Check for fall damage
            checkFallDamage();

            // Clear input.
            movement = Vector2.Zero;
            wasClimbing = isClimbing;
            isClimbing = false;
            isJumping = false;
        }

        /// <summary>
        /// Gets player horizontal movement and jump commands from input.
        /// </summary>
        private void GetInput(
            KeyboardState keyboardState, 
            GamePadState gamePadState, 
            TouchCollection touchState,
            AccelerometerState accelState,
            DisplayOrientation orientation,
            InputManager inputManager)
        {
            // Get analog horizontal movement.
            movement.X = gamePadState.ThumbSticks.Left.X * MoveStickScale;
            // Get analog vertical movement.
            movement.Y = gamePadState.ThumbSticks.Left.Y * MoveStickScale;

            // Ignore small movements to prevent running in place.
            if (Math.Abs(movement.X) < 0.5f)
            {
                movement.X = 0.0f;
            }
            if (Math.Abs(movement.Y) < 0.5f)
            {
                movement.Y = 0.0f;
            }

            /*
            // Move the player with accelerometer
            if (Math.Abs(accelState.Acceleration.Y) > 0.10f)
            {
                // set our movement speed
                movement = MathHelper.Clamp(-accelState.Acceleration.Y * AccelerometerScale, -1f, 1f);

                // if we're in the LandscapeLeft orientation, we must reverse our movement
                if (orientation == DisplayOrientation.LandscapeRight)
                    movement = -movement;
            }
            */

            // If any digital horizontal movement input is found, override the analog movement.
            if (gamePadState.IsButtonDown(Buttons.DPadLeft) ||
                keyboardState.IsKeyDown(Keys.Left) ||
                keyboardState.IsKeyDown(Keys.A))
            {
                movement.X = -1.0f;
            }
            else if (gamePadState.IsButtonDown(Buttons.DPadRight) ||
                     keyboardState.IsKeyDown(Keys.Right) ||
                     keyboardState.IsKeyDown(Keys.D))
            {
                movement.X = 1.0f;
            }


            // Handle ladder up input
            if (gamePadState.IsButtonDown(Buttons.DPadUp) ||                
                keyboardState.IsKeyDown(Keys.Up) ||
                keyboardState.IsKeyDown(Keys.W))
            {                
                isClimbing = false;
                
                //makes sure the players position is aligned to the center of the ladder
                if (IsAlignedToLadder())
                {
                    //need to check the tile behind the player, not what he is standing on
                    if (level.GetTileCollisionBehindPlayer(position) == TileCollision.Ladder)
                    {
                        isClimbing = true;
                        isJumping = false;
                        isOnGround = false;
                        movement.Y = -1.0f;
                    }
                }

            }
            // Handle ladder down input
            else if (gamePadState.IsButtonDown(Buttons.DPadDown) ||               
                keyboardState.IsKeyDown(Keys.Down) ||
                keyboardState.IsKeyDown(Keys.S))
            {
                isClimbing = false;

                //makes sure the players position is aligned to the center of the ladder
                if (IsAlignedToLadder())
                {
                    //need to check the tile that the player is standing on
                    if (level.GetTileCollisionBelowPlayer(this.Position) == TileCollision.Ladder)
                    {
                        isClimbing = true;
                        isJumping = false;
                        isOnGround = false;
                        movement.Y = 2.0f;
                    }
                }

            }

            // Check if the player wants to jump.
            // Change this so that we only want to jump if it is a new press - i.e. KeyPressDown()
            //
            //isJumping = gamePadState.IsButtonDown(JumpButton) || keyboardState.IsKeyDown(Keys.Space) || keyboardState.IsKeyDown(Keys.Up) || 
            //    keyboardState.IsKeyDown(Keys.W) || touchState.AnyTouch();

            isJumping = inputManager.IsNewPress(JumpButton) || inputManager.IsNewPress(Keys.Space) || inputManager.IsNewPress(Keys.Up) ||
                inputManager.IsNewPress(Keys.W);
        }

        /// <summary>
        /// Checks how far (if at all) the player has fallen.
        /// If the player has fallen to far, the player is killed, otherwise the player survives
        /// </summary>
        private void checkFallDamage()
        {
            // We are falling if we are not on the ground or climbing
            if (isAlive && (isOnGround || isClimbing))
            {
                if (Vector2.Distance(lastGroundPos, position) > MAX_SAFE_FALL_DISTANCE)
                {
                    // We have fallen too far to survive
                    OnKilled("Can't survive that height", DeathType.Fall);
                }

                lastGroundPos = position;
            }
        }

        /// <summary>
        /// Makes sure that the player is aligned to the center of a ladder piece
        /// We may not want this, allow for horizontal ladder movement?
        /// </summary>
        private bool IsAlignedToLadder()
        {
            int playerOffset = ((int)position.X % Tile.Width) - Tile.Center;
            if (Math.Abs(playerOffset) <= LadderAlignment &&
                level.GetTileCollisionBelowPlayer(new Vector2(
                    this.position.X,
                    this.position.Y + 1)) == TileCollision.Ladder ||
                level.GetTileCollisionBelowPlayer(new Vector2(
                    this.position.X,
                    this.position.Y - 1)) == TileCollision.Ladder)
            {
                // Align the player with the middle of the tile 
                position.X -= playerOffset;
                return true;
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// Updates the player's velocity and position based on input, gravity, etc.
        /// </summary>
        public void ApplyPhysics(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            Vector2 previousPosition = Position;

            // Base velocity is a combination of horizontal movement control and
            // acceleration downward due to gravity.
            
            //velocity.X += movement * MoveAcceleration * elapsed;
            //velocity.Y = MathHelper.Clamp(velocity.Y + GravityAcceleration * elapsed, -MaxFallSpeed, MaxFallSpeed);

            // If not climbing ladder, handle Y movement as before
            if (!isClimbing)
            {
                if (wasClimbing)
                {
                    velocity.Y = 0;
                }
                else
                {
                    velocity.Y = MathHelper.Clamp(velocity.Y + GravityAcceleration * elapsed, -MaxFallSpeed, MaxFallSpeed);
                }
            }            
            else
            {
                velocity.Y = movement.Y * MoveAcceleration * elapsed;
            }

            velocity.X += movement.X * MoveAcceleration * elapsed;
            velocity.Y = DoJump(velocity.Y, gameTime);

            // Apply pseudo-drag horizontally.
            if (IsOnGround)
                velocity.X *= GroundDragFactor;
            else
                velocity.X *= AirDragFactor;

            // Prevent the player from running faster than his top speed.            
            velocity.X = MathHelper.Clamp(velocity.X, -MaxMoveSpeed, MaxMoveSpeed);

            // Apply velocity.
            Position += velocity * elapsed;
            //position = new Vector2((float)Math.Round(Position.X), (float)Math.Round(Position.Y));

            // If the player is now colliding with the level, separate them.
            HandleCollisions();

            // If the collision stopped us from moving, reset the velocity to zero.
            if (Position.X == previousPosition.X)
                velocity.X = 0;

            if (Position.Y == previousPosition.Y)
                velocity.Y = 0;
        }

        /// <summary>
        /// Calculates the Y velocity accounting for jumping and
        /// animates accordingly.
        /// </summary>
        /// <remarks>
        /// During the accent of a jump, the Y velocity is completely
        /// overridden by a power curve. During the decent, gravity takes
        /// over. The jump velocity is controlled by the jumpTime field
        /// which measures time into the accent of the current jump.
        /// </remarks>
        /// <param name="velocityY">
        /// The player's current velocity along the Y axis.
        /// </param>
        /// <returns>
        /// A new Y velocity if beginning or continuing a jump.
        /// Otherwise, the existing Y velocity.
        /// </returns>
        private float DoJump(float velocityY, GameTime gameTime)
        {
            // If the player wants to jump
            if (isJumping)
            {
                // Begin or continue a jump
                if ((!wasJumping && IsOnGround) || jumpTime > 0.0f)
                {
                    if (jumpTime == 0.0f)
                        jumpSound.Play();

                    jumpTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                    if (isRight || velocity.X > 0)
                    {
                        sprite.PlayAnimation(jumpRightAnimation);
                    }
                    else
                    {
                        sprite.PlayAnimation(jumpLeftAnimation);
                    }
                }

                // If we are in the ascent of the jump
                if (0.0f < jumpTime && jumpTime <= MaxJumpTime)
                {
                    // Fully override the vertical velocity with a power curve that gives players more control over the top of the jump
                    velocityY = JumpLaunchVelocity * (1.0f - (float)Math.Pow(jumpTime / MaxJumpTime, JumpControlPower));
                }
                else
                {
                    // Reached the apex of the jump
                    jumpTime = 0.0f;
                }
            }
            else
            {
                // Continues not jumping or cancels a jump in progress
                jumpTime = 0.0f;
            }
            wasJumping = isJumping;

            return velocityY;
        }

        /// <summary>
        /// Detects and resolves all collisions between the player and his neighboring
        /// tiles. When a collision is detected, the player is pushed away along one
        /// axis to prevent overlapping. There is some special logic for the Y axis to
        /// handle platforms which behave differently depending on direction of movement.
        /// </summary>
        private void HandleCollisions()
        {
            // Get the player's bounding rectangle and find neighboring tiles.
            RectangleF bounds = BoundingRectangle;
            int leftTile = (int)Math.Floor((float)bounds.Left / Tile.Width);
            int rightTile = (int)Math.Ceiling(((float)bounds.Right / Tile.Width)) - 1;
            int topTile = (int)Math.Floor((float)bounds.Top / Tile.Height);
            int bottomTile = (int)Math.Ceiling(((float)bounds.Bottom / Tile.Height)) - 1;

            List<Tile> candidateTiles = new List<Tile>();
            for (int y = topTile; y <= bottomTile; ++y)
            {
                for (int x = leftTile; x <= rightTile; ++x)
                {
                    Tile tile = level.getTile(x, y);
                    candidateTiles.Add(tile);
                }
            }
            candidateTiles.AddRange(level.MoveableTiles);

            // Reset flag to search for ground collision.
            isOnGround = false;

            bool movedByTile = false; // Ensure only none tile can move the player at a time

            // For each potentially colliding tile,
            foreach(Tile tile in candidateTiles)
            {
                if (tile == null)
                    continue;

                // If this tile is collidable,
                TileCollision collision = tile.Collision;
                if (collision != TileCollision.Passable)
                {
                    // Determine collision depth (with direction) and magnitude.
                    RectangleF tileBounds = tile.Sprite.Bounds;
                    Vector2 depth = RectangleExtensions.GetIntersectionDepth(bounds, tileBounds);
                    if (depth != Vector2.Zero)
                    {
                        float absDepthX = Math.Abs(depth.X);
                        float absDepthY = Math.Abs(depth.Y);

                        // Resolve the collision along the shallow axis.
                        if (collision != TileCollision.Death && collision != TileCollision.Water && collision != TileCollision.Ladder && (absDepthY < absDepthX || collision == TileCollision.Platform))
                        {
                            // If we crossed the top of a tile, we are on the ground.
                            
                            // This needs to change for ladder mechanic
                            //if (previousBottom <= tileBounds.Top)
                            if(tileBounds.Top - previousBottom < 0.001f) // 0.001 is the delta for floating point comparisons
                            {
                                isOnGround = true;
                                isClimbing = false;
                                isJumping = false;                             

                            }
                            

                            // Perform moving tile collition
                            if (tile is MoveableTile && !movedByTile)
                            {
                                MoveableTile moveableTile = (MoveableTile)tile;
                                position += moveableTile.FrameVelocity;
                                isOnGround = true;
                                movedByTile = true;
                            }

                            // Ignore platforms, unless we are on the ground.
                            if (collision == TileCollision.Impassable || IsOnGround)
                            {
                                // Resolve the collision along the Y axis.
                                Position = new Vector2(Position.X, Position.Y + depth.Y);

                                // Perform further collisions with the new bounds.
                                bounds = BoundingRectangle;
                            }
                        }
                        else if (collision == TileCollision.Impassable) // Ignore platforms.
                        {

                            // Resolve the collision along the X axis.
                            Position = new Vector2(Position.X + depth.X, Position.Y);

                            // Perform further collisions with the new bounds.
                            bounds = BoundingRectangle;
                        }
                        else if (isAlive && collision == TileCollision.Ladder && !isClimbing)
                        {
                            //when we are walking in front of a ladder, or falling past a ladder
                            isClimbing = true;

                            // Resolve the collision along the Y axis
                            Position = new Vector2(Position.X, Position.Y);
                                
                            // Future collisions with the new bounds
                            bounds = BoundingRectangle;
                        }
                        else if (isAlive && collision == TileCollision.Death) // Something that kills you!
                        {
                            if(absDepthY > tile.Sprite.Height/2)
                                OnKilled("You touched something stupid!", DeathType.Spike);
                        }
                        else if (isAlive && collision == TileCollision.Water)
                        {
                            RectangleF tileRect = tile.Sprite.Bounds;
                            RectangleF headRect = BoundingRectangle;
                            headRect.Height /= 8; // Take the bounds of the head only

                            if (tileRect.Intersects(headRect))
                                OnKilled("You drowned under water", DeathType.Water);
                        }
                    }
                }         
            }

            // Save the new bounds bottom.
            previousBottom = bounds.Bottom;
        }

        /// <summary>
        /// Called when the player has been killed.
        /// </summary>
        /// <param name="killedBy">
        /// The name of whatever killed the player.
        /// </param>
        public void OnKilled() { OnKilled(""); }
        public void OnKilled(String killedBy) { OnKilled(killedBy, DeathType.Default); }
        public void OnKilled(String killedBy, DeathType killedType)
        {

            isAlive = false;           

            if (!deathPlayed)
            {
                switch (killedType)
                {
                    case DeathType.Default:
                        killedSound.Play();
                        break;
                    case DeathType.Water:
                        waterDrownSound.Play();
                        break;
                    case DeathType.Fall:
                        fallImpactSound.Play();
                        break;
                    case DeathType.Spike:
                        spikeImpaleSound.Play();
                        break;
                    default:
                        killedSound.Play();
                        break;
                }

                deathPlayed = true;
            }

            if (isRight || velocity.X > 0)
            {
                if (killedType == DeathType.Water)
                {
                    sprite.PlayAnimation(drownRightAnimation);
                }
                else
                {
                    sprite.PlayAnimation(dieRightAnimation);
                }
            }
            else
            {
                if (killedType == DeathType.Water)
                {
                    sprite.PlayAnimation(drownLeftAnimation);
                }
                else
                {
                    sprite.PlayAnimation(dieLeftAnimation);
                }
            }
            //TODO: pan the camera + new death animations

        }

        /// <summary>
        /// Called when this player reaches the level's exit.
        /// </summary>
        public void OnReachedExit() {}

        /// <summary>
        /// Draws the animated player.
        /// </summary>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Draw that sprite.
            sprite.Draw(gameTime, spriteBatch, Position, SpriteEffects.None);
        }
    }
}
