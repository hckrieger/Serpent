using EC.Components;
using EC.Components.Render;
using EC.CoreSystem;
using EC.Services;
using EC.Services.AssetManagers;
using EC.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;
using Serpent.Scenes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Serpent.Scenes.PlayingScene;

namespace Serpent.Entities
{
    internal class Player : Entity
    {
        public enum Direction
        {
            Up, Down, Left, Right, None
        }


		private readonly float[] angles = new float[4] { 0, MathHelper.PiOver2, MathHelper.Pi, 3 * MathHelper.PiOver2 };
		private Direction currentDirection, nextDirection;

        public BodySection[] SerpentBody { get; set; }
        public BodySection SerpentHead { get; set; }

        private InputManager inputManager;

        private Func<Point, Vector2> pointToPosition;

        float timer, startTimer;

        public event Action OnPositionChanged;

        Vector2 centerOfSprite = new Vector2(24, 24);

        public bool AllowMovement { get; set; }
        private bool aboutToCrashInEdge = false;
        private GridData gridData;

        public bool AddBodyNextCycle { get; set; } = false;

        public event EventHandler<PlayerEventArgs> GameStateChanged;
        
        public Player(GridData gridData, Func<Point, Vector2> pointToPosition, Action<Entity> addEntity, Game game) : base(game)
        {
            //Pool up the serpent body sections to be activated when the body grows
            SerpentBody = new BodySection[gridData.gridLength];

            

			for (int i = 0; i < gridData.gridLength; i++)
            {
                SerpentBody[i] = new BodySection(game);

                addEntity(SerpentBody[i]);
                if (i > 0)
                    SerpentBody[i].IsActive = false;
                else //Activate the first body section to be used as the serpent head
                    SerpentHead = SerpentBody[i];

               
                //center the origin of the snake parts so that each sprite can rotate based on it's direction and location
				SerpentBody[i].AddComponent(new Origin(centerOfSprite, SerpentBody[i]));

            }

            this.pointToPosition = pointToPosition;
            this.gridData = gridData;

           

            //set the sprite frame, grid location and position in the gameworld. 
            SerpentHead.GetComponent<SpriteRenderer>().SetSpriteFrame(0);
            SerpentHead.CurrentLocation = new Point(3, 3);
            SerpentHead.GetComponent<SpriteRenderer>().LayerDepth = .8f;
            SerpentHead.Transform.LocalPosition = pointToPosition(SerpentHead.CurrentLocation)+centerOfSprite;

			inputManager = game.Services.GetService<InputManager>();

		}

        public override void Initialize()
        {
            base.Initialize();

            

            

          //  gridLocationsThatAreOccupied = new List<Point>();

			timer = .375f;
			startTimer = timer;

			Reset();


        }

        public override void Update(GameTime gameTime)
        {

            //If all the parts of the snake are active then change to win state
            if (SerpentBody.All(m => m.IsActive) && AllowMovement)
            {
				OnGameStateChanged(GameState.Win);
                return;
			}
                

            
            MoveSnake(gameTime);

			base.Update(gameTime);

        }


        //Method that makes it more readable to change game state by invoking an event
        public void OnGameStateChanged(GameState state)
        {
            GameStateChanged.Invoke(this, new PlayerEventArgs { GameState = state });
        }


        //Method that deals with serpent movement
        private void MoveSnake(GameTime gameTime)
        {
            

            //Set the pending direction of the snake based on input
			if (currentDirection != Direction.Left && (inputManager.KeyJustPressed(Keys.Right) || inputManager.KeyJustPressed(Keys.D)))
                nextDirection = Direction.Right;
            else if (currentDirection != Direction.Right && (inputManager.KeyJustPressed(Keys.Left) || inputManager.KeyJustPressed(Keys.A)))
                nextDirection = Direction.Left;
            else if (currentDirection != Direction.Down && (inputManager.KeyJustPressed(Keys.Up) || inputManager.KeyJustPressed(Keys.W)))
                nextDirection = Direction.Up;
            else if (currentDirection != Direction.Up && (inputManager.KeyJustPressed(Keys.Down) || inputManager.KeyJustPressed(Keys.S)))
                nextDirection = Direction.Down;


			



            //detect when the serpent head would be crashing into the edge if the movement timer ran down
            //This makes it so it can detect a loss without going out of bounds
            if (nextDirection == Direction.Left && SerpentHead.CurrentLocation.X == 0 ||
                nextDirection == Direction.Right && SerpentHead.CurrentLocation.X == gridData.gridWidth - 1 ||
                nextDirection == Direction.Up && SerpentHead.CurrentLocation.Y == 0 ||
                nextDirection == Direction.Down && SerpentHead.CurrentLocation.Y == gridData.gridHeight - 1)
                aboutToCrashInEdge = true;
            else
                aboutToCrashInEdge = false;


            //set the relative location based on the input direction
            Point relativeLocation = new Point(0, 0);
            switch (nextDirection)
            {
                case Direction.Up:
                    relativeLocation = new Point(0, -1);
                    break;
                case Direction.Down:
                    relativeLocation = new Point(0, 1);
                    break;
                case Direction.Left:
                    relativeLocation = new Point(-1, 0);
                    break;
                case Direction.Right:
                    relativeLocation = new Point(1, 0);
                    break;

            }


            //run the timer
            //This bool is set in the PlayerScene class
            if (AllowMovement)
            {

				timer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
			}
            else
                return;
      

            //when the timer runs out...
            if (timer <= 0 && nextDirection != Direction.None)
            {

                //Chagne state to game over if it would crash into the boundry upon movement and return to halt movement
                if (aboutToCrashInEdge)
                {
					OnGameStateChanged(PlayingScene.GameState.GameOver);
                    aboutToCrashInEdge = false;
                    
                    return;
				}
                    

                //Set the sprite frame of the serpent head based on chosen direction
				switch (nextDirection)
				{
					case Direction.Up:
						SetSpriteFrame(0, 0, angles[2]);
						break;
					case Direction.Down:
						SetSpriteFrame(0, 0, angles[0]);
						break;
					case Direction.Left:
						SetSpriteFrame(0, 0, angles[1]);
						break;
					case Direction.Right:
						SetSpriteFrame(0, 0, angles[3]);
						break;

				}


				//grow the serpent body and respawn pellet
				if (AddBodyNextCycle)
                {

					AddBody();

					
                    AddBodyNextCycle = false;
                }


                //Make the previous location of each part of the snake the current location before the current location changes to where it would move
				for (int i = 0; i < SerpentBody.Count(); i++)
                {
					
					if (SerpentBody[i].IsActive)
                    {
						
						SerpentBody[i].PreviousLocation = SerpentBody[i].CurrentLocation;
					}
                    else
                        break;
				}

                
                //Add the relative location to the current location for the serpent head to move to a desired adjacent area. 
                SerpentHead.CurrentLocation += relativeLocation;

                

                //convert the grid position to the vector position
				SerpentHead.Transform.LocalPosition = pointToPosition(SerpentHead.CurrentLocation)+centerOfSprite;

				for (int i = 1; i < SerpentBody.Count(); i++)
				{
                   
                    if (SerpentBody[i].IsActive)
                    {
						//Move each section of the snake to the position of the next section down
						SerpentBody[i].CurrentLocation = SerpentBody[i - 1].PreviousLocation;
						SerpentBody[i].Transform.LocalPosition = pointToPosition(SerpentBody[i].CurrentLocation)+centerOfSprite;
					}
                       
                    else
                        break;
				}

                
                //Invoke the the event method is the PlayingScene class that detects if there's a pellet to collide with in the new direction
				OnPositionChanged.Invoke();

				currentDirection = nextDirection;

				//Set the sprite of the body section based on it's location in the body
				for (int i = 1; i < SerpentBody.Count(); i++)
				{
                    if (SerpentBody[i].IsActive)
                    {
                        //Game over if the serpent runs into itself
                        if (SerpentHead.CurrentLocation == SerpentBody[i].CurrentLocation)
                            OnGameStateChanged(GameState.GameOver);
                        
                        //see method that sets the sprites below
                        SetBodySprite(SerpentBody[i]);
                    }
                    else
                        break;
                    
				}


                //reset movement timer
				timer = startTimer;


			}



		}

        //Method adds body to snake by detecting the first inactive body in list
        //and set's it to the position of the previous location of the last active body and activates it.
        public void AddBody()
        {
            var body = SerpentBody.First(m => !m.IsActive);
			body.CurrentLocation = SerpentBody.Last(m => m.IsActive).PreviousLocation;
            body.IsActive = true;


		}



		void SetSpriteFrame(int bodySprite, int spriteIndex, float rotation)
		{
			SerpentBody[bodySprite].GetComponent<SpriteRenderer>().SetSpriteFrame(spriteIndex);
            SerpentBody[bodySprite].Transform.LocalRotation = rotation;
		}


        //Function finds the location of each part of the snake body to determine what it's sprite frame should be
        public void SetBodySprite(BodySection bodySection)
        {
            
            int index = Array.IndexOf(SerpentBody, bodySection);
            int nextBodyNumber = 0;

            //Used to keep the index from going out of it's bounds
            if (index < SerpentBody.Length - 1)
                nextBodyNumber = 1; 

            var currentBodyLocation = SerpentBody[index].CurrentLocation;
			var previousBodyLocation = SerpentBody[index - 1].CurrentLocation;
			var nextBodyLocation = SerpentBody[index + nextBodyNumber].CurrentLocation;

    
            //determines if there is a body at a chosen location one way.  Only relevent for the tail of the serpent.
			bool BodyOnlyExistsAtPreviousLocations(Point previousRelativeLocation)
            {
                if (nextBodyNumber == 0)
                {
                    return currentBodyLocation - previousBodyLocation == previousRelativeLocation;
				}

                return currentBodyLocation - previousBodyLocation == previousRelativeLocation &&
                       SerpentBody[index + 1].IsActive == false;

            }


            //Determines if there are bodies and the chosen locations before and after the current body. 
            bool BodiesExistAtNearbyLocations(Point previousRelativeLocation, Point nextRelativeLocation)
            {
                if (nextBodyNumber == 0)
                {
                    return false;
                }
                return (previousBodyLocation - currentBodyLocation == previousRelativeLocation &&
                       nextBodyLocation - currentBodyLocation == nextRelativeLocation) ||
					   (previousBodyLocation - currentBodyLocation == nextRelativeLocation &&
					   nextBodyLocation - currentBodyLocation == previousRelativeLocation);

            }

            

            var isEvenIndex = index % 2 == 0;
            var isOddIndex = index % 2 == 1;


            
			if (BodyOnlyExistsAtPreviousLocations(new Point(1, 0)))
				SetSpriteFrame(index, 3, angles[3]);
            else if (BodyOnlyExistsAtPreviousLocations(new Point(-1, 0)))
                SetSpriteFrame(index, 3, angles[1]);
            else if (BodyOnlyExistsAtPreviousLocations(new Point(0, 1)))
                SetSpriteFrame(index, 3, angles[0]);
            else if (BodyOnlyExistsAtPreviousLocations(new Point(0, -1)))
                SetSpriteFrame(index, 3, angles[2]);
            else if (BodiesExistAtNearbyLocations(new Point(-1, 0), new Point(1, 0)) && isEvenIndex)
                SetSpriteFrame(index, 2, angles[1]);
            else if (BodiesExistAtNearbyLocations(new Point(-1, 0), new Point(0, -1)) && isEvenIndex)
				SetSpriteFrame(index, 1, angles[3]);
            else if (BodiesExistAtNearbyLocations(new Point(-1, 0), new Point(0, 1)) && isEvenIndex)
                SetSpriteFrame(index, 1, angles[2]);
            else if (BodiesExistAtNearbyLocations(new Point(1, 0), new Point(0, -1)) && isEvenIndex)
                SetSpriteFrame(index, 1, angles[0]);
            else if (BodiesExistAtNearbyLocations(new Point(1, 0), new Point(0, 1)) && isEvenIndex)
                SetSpriteFrame(index, 1, angles[1]);
            else if (BodiesExistAtNearbyLocations(new Point(0, 1), new Point(0, -1)) && isEvenIndex)
                SetSpriteFrame(index, 2, angles[0]);
			else if (BodiesExistAtNearbyLocations(new Point(-1, 0), new Point(1, 0)) && isOddIndex)
				SetSpriteFrame(index, 6, angles[1]);
			else if (BodiesExistAtNearbyLocations(new Point(-1, 0), new Point(0, -1)) && isOddIndex)
				SetSpriteFrame(index, 5, angles[3]);
			else if (BodiesExistAtNearbyLocations(new Point(-1, 0), new Point(0, 1)) && isOddIndex)
				SetSpriteFrame(index, 5, angles[2]);
			else if (BodiesExistAtNearbyLocations(new Point(1, 0), new Point(0, -1)) && isOddIndex)
				SetSpriteFrame(index, 5, angles[0]);
			else if (BodiesExistAtNearbyLocations(new Point(1, 0), new Point(0, 1)) && isOddIndex)
				SetSpriteFrame(index, 5, angles[1]);
			else if (BodiesExistAtNearbyLocations(new Point(0, 1), new Point(0, -1)) && isOddIndex)
				SetSpriteFrame(index, 6, angles[0]);

		}

        public override void Reset()
        {
            for (int i = 1; i < SerpentBody.Count(); i++)
            {
                SerpentBody[i].IsActive = false;
            }


			currentDirection = Direction.None;
            nextDirection = currentDirection;
            timer = 0;
		}

	}

}
