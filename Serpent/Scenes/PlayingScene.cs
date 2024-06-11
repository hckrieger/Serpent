using EC.Components.Render;
using EC.CoreSystem;
using EC.Services;
using EC.Services.AssetManagers;
using EC.Utilities;
using EC.Utilities.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;
using Serpent.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serpent.Scenes
{
	internal class PlayingScene : Scene
	{
		public enum GameState { Ready, Playing, GameOver, Win }
        public GameState currentGameState { get; set; }

		private Keys[] directionKeys = new Keys[] { Keys.Left, Keys.Right, Keys.Up, Keys.Down,
													Keys.W, Keys.A, Keys.S, Keys.D};

		private GridSpace[,] gridBoard;
		private Entity gameBoardEntity;

		private const int GridWidth = 12;
		private const int GridHeight = 9;
		private const int GridLength = GridWidth * GridHeight;
		public readonly record struct GridData(int gridWidth, int gridHeight, int gridLength);
		private const int GridSize = 48;
		private Player player;
		private List<Vector2> positions;
		private List<Point> pelletSpawnLocations;
		private Entity pellet;
		private int width;

		private InputManager inputManager;

		private Entity status, score, title;
		private float jingleDelayTimer;
		private bool delaySong = false;
		private AudioAssetManager audioAssetManager;
		

		public PlayingScene(int width, Game game) : base(game)
        {
			this.width = width;
        }

		public override void Initialize()
		{
			base.Initialize();

			gameBoardEntity = new Entity(Game);
			gameBoardEntity.Transform.LocalPosition = new Vector2(0, GridSize);
			AddEntity(gameBoardEntity);
			positions = new List<Vector2>();
			GameBoardInitialize();

			GridData gridData = new GridData(GridWidth, GridHeight, GridLength);

			player = new Player(gridData, PointToPosition, AddEntity, Game);
			AddEntity(player, gameBoardEntity);

			pellet = new Entity(Game);

			pellet.LoadSpriteComponents("Sprites/MainSprites", Game);

			status = new Entity(Game);
			status.LoadTextComponents("Fonts/HUDFont", "", Color.White, Game, TextRenderer.Alignment.Center);
			AddEntity(status);

			score = new Entity(Game);
			score.LoadTextComponents("Fonts/HUDFont", "", Color.White, Game, TextRenderer.Alignment.Center);
			score.Transform.LocalPosition = new Vector2(500, 24);
			AddEntity(score);

			title = new Entity(Game);
			title.LoadTextComponents("Fonts/HUDFont", "Serpent", Color.Green, Game);
			title.Transform.LocalPosition = new Vector2(30, 12);

			AddEntity(title);

			var foodRenderer = pellet.GetComponent<SpriteRenderer>();
			foodRenderer.LayerDepth = .6f;
			foodRenderer.SetSpriteFrame(4);

			pelletSpawnLocations = new List<Point>();
			RandomSpawnPellet();
			AddEntity(pellet);

			player.OnPositionChanged += HandlePlayerPositionChanged;
			player.GameStateChanged += HandleGameStateChanged;

			inputManager = Game.Services.GetService<InputManager>();	
			audioAssetManager = Game.Services.GetService<AudioAssetManager>();

			player.OnGameStateChanged(GameState.Ready);


			audioAssetManager.PlayMusic("Audio/serpentJingle");

			
		}

		public override void Update(GameTime gameTime)
		{
			//RemoveEntity(title);
			base.Update(gameTime);
			

			//If the player hits a directional key when read then change to playing game state
			if (currentGameState == GameState.Ready) 
			{
				foreach (var key in directionKeys)
				{
					if (inputManager.KeyJustPressed(key))
						player.OnGameStateChanged(GameState.Playing);
				}
			}


			//If game over or a win then allow to press space to reset the snake and set to ready.
			if (currentGameState == GameState.GameOver ||
				currentGameState == GameState.Win)
			{
				if (inputManager.KeyJustPressed(Keys.Space))
				{
					player.OnGameStateChanged(GameState.Ready);
					player.Reset();
					RandomSpawnPellet();
					
				}
			}


			//If the game state is playing then communicate to the player object to allow movement
			if (currentGameState == GameState.Playing)
				player.AllowMovement = true;
			else 
				player.AllowMovement = false;

			
			//Set the score based on the number of active sections of the serpent to the number of grid areas that could be covered
			
				score.GetComponent<TextRenderer>().Text = $"{player.SerpentBody.Count(m => m.IsActive)} / {GridWidth*GridHeight}";

			
			//After a win or a loss pause the music and resume it when the win/loss jingle is finished.
			if (delaySong)
			{
				audioAssetManager.PauseMusic();
				jingleDelayTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

				if (jingleDelayTimer < 0)
				{
					audioAssetManager.ResumeMusic();
					delaySong = false;
				}
			}
			
		}


		//set game states  and the audio details of the jingle when won/loss
		private void HandleGameStateChanged(object sender, PlayerEventArgs e)
		{
			switch (e.GameState)
			{
				case GameState.Ready:

					currentGameState = GameState.Ready;
					
					break;
				case GameState.Playing:
					currentGameState = GameState.Playing;
					
					break;
				case GameState.GameOver:
					currentGameState = GameState.GameOver;
					if (!delaySong)
					{
						audioAssetManager.PlaySoundEffect("Audio/lose");
						jingleDelayTimer = (float)audioAssetManager.LoadSoundEffect("Audio/lose").Duration.TotalSeconds + .5f;
					}
					delaySong = true;
					break;
				case GameState.Win:
					currentGameState = GameState.Win;
					audioAssetManager.PlaySoundEffect("Audio/win");
					jingleDelayTimer = (float)audioAssetManager.LoadSoundEffect("Audio/win").Duration.TotalSeconds + .5f;
					delaySong = true;
					break;

			}

			//Provide a message through the PlayerEvent Args and set the message position
			status.GetComponent<TextRenderer>().Text = e.StastusMessage;
			status.Transform.LocalPosition = new Vector2(width / 2, 24);
		}




		//Invoked when the player moves from the player object and then processes information when the serpent head lands on a pellet.
		//Will then respawn pellet at random avilable location, play sound effect, and communicate to the player object that it's set to add to the body in the next cycle
		private void HandlePlayerPositionChanged()
		{
			
			if (player.SerpentHead.CurrentLocation == PositionToPoint(pellet.Transform.LocalPosition))
			{
				RandomSpawnPellet();
				audioAssetManager.PlaySoundEffect("Audio/eatPellet");
				player.AddBodyNextCycle = true;
			}
				
		}


		//Initializes the grid 
		private void GameBoardInitialize()
		{
			
			gridBoard = new GridSpace[GridWidth, GridHeight];

			for (int y = 0; y < GridHeight; y++) 
			{
				for (int x = 0; x < GridWidth; x++)
				{
					GridSpace gridSpace = new GridSpace(new Point(x, y), GridSize, Game);
					
					gridBoard[x, y] = gridSpace;
					AddEntity(gridSpace, gameBoardEntity);
					positions.Add(gridSpace.Transform.Position);

				}
			}
		}


		//Method that contains logic for respawning pellet at a random area that is open
		private void RandomSpawnPellet()
		{

			pelletSpawnLocations.Clear();

			for (int y = 0; y < GridHeight; y++)
			{
				for (int x = 0; x < GridWidth; x++)
				{
					Point currentPoint = new Point(x, y);
					bool isOccupied = false;

					foreach (var body in player.SerpentBody)
					{
						if (body.IsActive && currentPoint == body.CurrentLocation)
						{
							isOccupied = true;
							break;
						}
					}

					if (!isOccupied)
					{
						pelletSpawnLocations.Add(currentPoint); 
					}
				}
			}

			if (pelletSpawnLocations.Count > 0)
			{
				var randomInt = MathUtils.RandomInt(pelletSpawnLocations.Count);
				var pelletPoint = pelletSpawnLocations[randomInt];
				pellet.Transform.LocalPosition = PointToPosition(pelletPoint);
			}

		}
			

		
		//Converts the point on a grid to a Vector position
		private Vector2 PointToPosition(Point point)
		{
			var index = MathUtils.GridPointToIndex(point, GridWidth);
			return positions[index];
		}


		//Converts a vector position to a point location 
		private Point PositionToPoint(Vector2 position)
		{
			var index = positions.IndexOf(position);
			return MathUtils.GridIndexToPoint(index, GridWidth);

		}
	}
}
