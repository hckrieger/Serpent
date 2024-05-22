using EC.Components.Render;
using EC.CoreSystem;
using EC.Services;
using EC.Utilities;
using EC.Utilities.Extensions;
using Microsoft.Xna.Framework;
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

		private Keys[] directionKeys = new Keys[] { Keys.Left, Keys.Right, Keys.Up, Keys.Down };

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

		private string statusMessage;

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
			foodRenderer.SetSpriteFrame(14);

			pelletSpawnLocations = new List<Point>();
			RandomSpawnPellet();
			AddEntity(pellet);

			player.OnPositionChanged += HandlePlayerPositionChanged;
			player.OnPelletRespawn += RandomSpawnPellet;
			player.GameStateChanged += HandleGameStateChanged;

			inputManager = Game.Services.GetService<InputManager>();	

			player.OnGameStateChanged(GameState.Ready);
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			if (currentGameState == GameState.Ready) 
			{
				foreach (var key in directionKeys)
				{
					if (inputManager.KeyJustPressed(key)) 
						player.OnGameStateChanged(GameState.Playing);
				}
			}


			if (currentGameState == GameState.GameOver ||
				currentGameState == GameState.Win)
			{
				if (inputManager.KeyJustPressed(Keys.Space))
				{
					player.OnGameStateChanged(GameState.Ready);
					player.Reset();
				}
			}

			if (currentGameState == GameState.Playing)
				player.AllowMovement = true;
			else 
				player.AllowMovement = false;

			
			score.GetComponent<TextRenderer>().Text = $"{player.SerpentBody.Count(m => m.IsActive)} / {GridWidth*GridHeight}";
		}

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
					break;
				case GameState.Win:
					currentGameState = GameState.Win;
					break;

			}

			status.GetComponent<TextRenderer>().Text = e.StastusMessage;
			status.Transform.LocalPosition = new Vector2(width / 2, 24);
		}


		private void HandlePlayerPositionChanged()
		{
			
			if (player.SerpentHead.CurrentLocation == PositionToPoint(pellet.Transform.LocalPosition))
			{
				RandomSpawnPellet();
				player.AddBodyNextCycle = true;
			}
				
		}

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
			

		

		private Vector2 PointToPosition(Point point)
		{
			var index = MathUtils.GridPointToIndex(point, GridWidth);
			return positions[index];
		}

		private Point PositionToPoint(Vector2 position)
		{
			var index = positions.IndexOf(position);
			return MathUtils.GridIndexToPoint(index, GridWidth);

		}
	}
}
