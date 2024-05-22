using EC.Components.Render;
using EC.CoreSystem;
using EC.Services;
using EC.Utilities;
using Microsoft.Xna.Framework;
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



        private Direction currentDirection;

        public BodySection[] SerpentBody { get; set; }
        public BodySection SerpentHead { get; set; }

        private InputManager inputManager;

        private Func<Point, Vector2> pointToPosition;
        private Direction directionForTimerDuration; 

        float timer, startTimer;

        public event Action OnPositionChanged;
        public event Action OnPelletRespawn;

        public bool AllowMovement { get; set; }
        private bool aboutToCrashInEdge = false;
        private GridData gridData;

        private List<Point> gridLocationsThatAreOccupied;

        public bool AddBodyNextCycle { get; set; } = false;

        public event EventHandler<PlayerEventArgs> GameStateChanged;

        public Player(GridData gridData, Func<Point, Vector2> pointToPosition, Action<Entity> addEntity, Game game) : base(game)
        {
            //Pool up the serpent body sections to be activated when the tail grows
            SerpentBody = new BodySection[gridData.gridLength];
            for (int i = 0; i < gridData.gridLength; i++)
            {
                SerpentBody[i] = new BodySection(game);
               // SerpentBody[i].GetComponent<SpriteRenderer>().LayerDepth += .1f;
                addEntity(SerpentBody[i]);
                if (i > 0)
                    SerpentBody[i].IsActive = false;
                else //Activate the first body section to be used as the serpent head
                    SerpentHead = SerpentBody[i];

            }

            this.pointToPosition = pointToPosition;
            this.gridData = gridData;

            //set the sprite frame, grid location and position in the gameworld. 
            SerpentHead.GetComponent<SpriteRenderer>().SetSpriteFrame(0);
            SerpentHead.CurrentLocation = new Point(3, 3);
            SerpentHead.GetComponent<SpriteRenderer>().LayerDepth = .8f;
            SerpentHead.Transform.LocalPosition = pointToPosition(SerpentHead.CurrentLocation);

        }

        public override void Initialize()
        {
            base.Initialize();

            inputManager = Game.Services.GetService<InputManager>();

            gridLocationsThatAreOccupied = new List<Point>();

			timer = .35f;
			startTimer = timer;

			Reset();


        }

        public override void Update(GameTime gameTime)
        {
            if (SerpentBody.All(m => m.IsActive) && AllowMovement)
            {
				OnGameStateChanged(GameState.Win);
                return;
			}
                

            
            MoveSnake(gameTime);
			base.Update(gameTime);

        }

        public void OnGameStateChanged(PlayingScene.GameState state)
        {
            GameStateChanged.Invoke(this, new PlayerEventArgs { GameState = state });
        }

        private void MoveSnake(GameTime gameTime)
        {
            

            //Set the pending direction of the snake based on input
			if (directionForTimerDuration != Direction.Left && inputManager.KeyJustPressed(Keys.Right))
                currentDirection = Direction.Right;
            else if (directionForTimerDuration != Direction.Right && inputManager.KeyJustPressed(Keys.Left))
                currentDirection = Direction.Left;
            else if (directionForTimerDuration != Direction.Down && inputManager.KeyJustPressed(Keys.Up))
                currentDirection = Direction.Up;
            else if (directionForTimerDuration != Direction.Up && inputManager.KeyJustPressed(Keys.Down))
                currentDirection = Direction.Down;


			



            //detect when the serpent head would be crashing into the edge if the movement timer ran down
            if (currentDirection == Direction.Left && SerpentHead.CurrentLocation.X == 0 ||
                currentDirection == Direction.Right && SerpentHead.CurrentLocation.X == gridData.gridWidth - 1 ||
                currentDirection == Direction.Up && SerpentHead.CurrentLocation.Y == 0 ||
                currentDirection == Direction.Down && SerpentHead.CurrentLocation.Y == gridData.gridHeight - 1)
                aboutToCrashInEdge = true;
            else
                aboutToCrashInEdge = false;


            //set the relative location based on the input
            Point relativeLocation = new Point(0, 0);
            switch (currentDirection)
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
            if (AllowMovement)
            {

				timer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
			}
            else
                return;
      

            //when the timer runs out...
            if (timer <= 0 && currentDirection != Direction.None)
            {
                if (aboutToCrashInEdge)
                {
					OnGameStateChanged(PlayingScene.GameState.GameOver);
                    aboutToCrashInEdge = false;
                    
                    return;
				}
                    

				directionForTimerDuration = currentDirection;


                //grow the serpent body and respawn pellet
				if (AddBodyNextCycle)
                {
					//OnPelletRespawn.Invoke();

					AddBody();

					
                    AddBodyNextCycle = false;
                }

                //Set sprite frame of the serpent head based on it's direction deterined by input
				switch (currentDirection)
				{
					case Direction.Up:
						SetSpriteFrame(0, 0);
						break;
					case Direction.Down:
						SetSpriteFrame(0, 2);
						break;
					case Direction.Left:
						SetSpriteFrame(0, 3);
						break;
					case Direction.Right:
						SetSpriteFrame(0, 1);
						break;

				}

				for (int i = 0; i < SerpentBody.Count(); i++)
                {
					
					if (SerpentBody[i].IsActive)
                    {
						
						SerpentBody[i].PreviousLocation = SerpentBody[i].CurrentLocation;
					}
                    else
                        break;
				}

                
                //Add the relative location to the current location for it to move to a desired adjacent area. 
                SerpentHead.CurrentLocation += relativeLocation;

                

                //convert the grid position to the vector position
				SerpentHead.Transform.LocalPosition = pointToPosition(SerpentHead.CurrentLocation);

				for (int i = 1; i < SerpentBody.Count(); i++)
				{
                   
                    if (SerpentBody[i].IsActive)
                    {
						//Move each section of the snake to the position of the next section down
						SerpentBody[i].CurrentLocation = SerpentBody[i - 1].PreviousLocation;
						SerpentBody[i].Transform.LocalPosition = pointToPosition(SerpentBody[i].CurrentLocation);
                        gridLocationsThatAreOccupied.Add(SerpentBody[i].CurrentLocation);
					}
                       
                    else
                        break;
				}

				OnPositionChanged.Invoke();


                //Set the sprite of the body section based on it's location in the body
				for (int i = 1; i < SerpentBody.Count(); i++)
				{
                    if (SerpentBody[i].IsActive)
                    {
                        //Game over if the serpent runs into itself
                        if (SerpentHead.CurrentLocation == SerpentBody[i].CurrentLocation)
                            OnGameStateChanged(GameState.GameOver);
                        

                        SetBodySprite(SerpentBody[i]);
                    }
                    else
                        break;
                    
				}

                gridLocationsThatAreOccupied.Clear();

                //reset movement timer
				timer = startTimer;


			}



		}


        public void AddBody()
        {
            var body = SerpentBody.First(m => !m.IsActive);
			body.CurrentLocation = SerpentBody.Last(m => m.IsActive).PreviousLocation;
            body.GetComponent<SpriteRenderer>().SetSpriteFrame(0);
            body.IsActive = true;


		}

		void SetSpriteFrame(int bodySprite, int spriteIndex)
		{
			SerpentBody[bodySprite].GetComponent<SpriteRenderer>().SetSpriteFrame(spriteIndex);
		}


        //Function that sets the sprite of the body based on it's position
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


			bool BodyOnlyExistsAtPreviousLocations(Point previousRelativeLocation)
            {
                if (nextBodyNumber == 0)
                {
                    return currentBodyLocation - previousBodyLocation == previousRelativeLocation;
				}

                return currentBodyLocation - previousBodyLocation == previousRelativeLocation &&
                       SerpentBody[index + 1].IsActive == false;

            }

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


            if (BodyOnlyExistsAtPreviousLocations(new Point(1, 0)))
                SetSpriteFrame(index, 10);
            else if (BodyOnlyExistsAtPreviousLocations(new Point(-1, 0)))
                SetSpriteFrame(index, 11);
            else if (BodyOnlyExistsAtPreviousLocations(new Point(0, 1)))
                SetSpriteFrame(index, 8);
            else if (BodyOnlyExistsAtPreviousLocations(new Point(0, -1)))
                SetSpriteFrame(index, 9);
            else if (BodiesExistAtNearbyLocations(new Point(-1, 0), new Point(1, 0)))
                SetSpriteFrame(index, 13);
            else if (BodiesExistAtNearbyLocations(new Point(-1, 0), new Point(0, -1)))
                SetSpriteFrame(index, 7);
            else if (BodiesExistAtNearbyLocations(new Point(-1, 0), new Point(0, 1)))
                SetSpriteFrame(index, 6);
            else if (BodiesExistAtNearbyLocations(new Point(1, 0), new Point(0, -1)))
                SetSpriteFrame(index, 4);
            else if (BodiesExistAtNearbyLocations(new Point(1, 0), new Point(0, 1)))
                SetSpriteFrame(index, 5);
            else if (BodiesExistAtNearbyLocations(new Point(0, 1), new Point(0, -1)))
                SetSpriteFrame(index, 12);

        }

        public override void Reset()
        {
            for (int i = 1; i < SerpentBody.Count(); i++)
            {
                SerpentBody[i].IsActive = false;
            }


			currentDirection = Direction.None;
            directionForTimerDuration = currentDirection;
            timer = 0;
		}

	}

}
