using EC.Services.AssetManagers;
using Serpent.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serpent
{
	internal class PlayerEventArgs : EventArgs
	{
		public PlayingScene.GameState GameState { get; set; }


        public string StastusMessage 
		{
			get
			{
				switch (GameState)
				{
					case PlayingScene.GameState.Playing:
						return "Game on";
					case PlayingScene.GameState.GameOver:
						return "Game over\npress space to reset";
					case PlayingScene.GameState.Win:
						return "You win!\npress space to reset";
					case PlayingScene.GameState.Ready:
						return "Ready?\nmove with directional keys";
					default:
						return "";
				}
			}
		}

	}
}
