using EC;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Serpent.Scenes;

namespace Serpent
{
	public class Game1 : ExtendedGame
	{
		public const string PLAYING_SCENE = "PLAYING_SCENE";
		public const string START_SCREEN = "START_SCREEN";
		private readonly int width = 576;

		public Game1()
		{
			IsMouseVisible = true;
		}

		protected override void Initialize()
		{
			// TODO: Add your initialization logic here

			base.Initialize();

			ScaleResolutionToWindowSize(width, 480);
			IsFullScreen = false;
			SceneManager.AddScene(START_SCREEN, new StartScreen(width, this));
			SceneManager.AddScene(PLAYING_SCENE, new PlayingScene(width, this));
			SceneManager.ChangeScene(START_SCREEN);
		}


	}
}
