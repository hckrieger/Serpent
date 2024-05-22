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

			SceneManager.AddScene(PLAYING_SCENE, new PlayingScene(width, this));
			SceneManager.ChangeScene(PLAYING_SCENE);
		}


	}
}
