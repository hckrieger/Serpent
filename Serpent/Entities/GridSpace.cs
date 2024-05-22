using EC.CoreSystem;
using EC.Utilities.Extensions;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serpent.Entities
{
	internal class GridSpace : Entity
	{
		public bool IsOccupied { get; set; } = false;
		public Point CurrentLocation { get; set; }
		public GridSpace(Point gridLocation, int gridSize, Game game) : base(game)
		{
			Color color;  

			if ((gridLocation.X + gridLocation.Y) % 2 == 0)
				color = new Color(55, 148, 110);
			else
				color = new Color(48, 96, 130);

			CurrentLocation = gridLocation;

			this.LoadRectangleComponents("gridSpace", gridSize, gridSize, color, game);
            Transform.LocalPosition = new Vector2(gridSize * gridLocation.X, gridSize * gridLocation.Y);


		}
    }
}
