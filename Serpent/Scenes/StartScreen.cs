using EC.Components;
using EC.Components.Render;
using EC.CoreSystem;
using EC.Services;
using EC.Utilities.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Serpent.Scenes
{
	internal class StartScreen : Scene
	{
		private InputManager inputManager;
		private SceneManager sceneManager;

		Entity pressSpace;

		float timer, startTimer;

        public StartScreen(int width, Game game) : base(game)
        {
            Entity title = new Entity(game);
            title.LoadTextComponents("Fonts/Title", "Serpent", Color.Green, game, TextRenderer.Alignment.Center);
            title.Transform.LocalPosition = new Vector2(width / 2, 75);
            AddEntity(title);

			pressSpace = new Entity(game);
			pressSpace.LoadTextComponents("Fonts/HUDFont", "Press Space To Start", Color.Green, game, TextRenderer.Alignment.Center);
			pressSpace.Transform.LocalPosition = new Vector2(width / 2, 380);
			AddEntity(pressSpace);

			Entity credit = new Entity(game);
			credit.LoadTextComponents("Fonts/Credit", "Krieger Interactive", Color.Green, game, TextRenderer.Alignment.Center);
			credit.Transform.LocalPosition = new Vector2(width / 2, 450);
			AddEntity(credit);

			inputManager = Game.Services.GetService<InputManager>();
			sceneManager = Game.Services.GetService<SceneManager>();

			timer = .8f;
			startTimer = timer;

			Entity snakeSprite = new Entity(game);
			snakeSprite.LoadSpriteComponents("Sprites/MainSprites", game, EntityExtensions.ColliderShape.None, new Point(48, 48));
			snakeSprite.Transform.LocalScale = 2.5f;
			snakeSprite.Transform.LocalPosition = new Vector2(width / 2, 240);
			snakeSprite.AddComponent(new Origin(new Vector2(24, 24), snakeSprite));
			AddEntity(snakeSprite);
			
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			if (inputManager.KeyJustPressed(Keys.Space))
				sceneManager.ChangeScene(Game1.PLAYING_SCENE);

			timer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

			if (timer <= 0)
			{

				pressSpace.Visible = !pressSpace.Visible;
				timer = startTimer;
			}
		}
	}
}
