using EC.Components.Render;
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
	internal class BodySection : Entity
	{
        public Point CurrentLocation { get; set; }
        public Point PreviousLocation { get; set; }
        public BodySection(Game game) : base(game)
        {
            this.LoadSpriteComponents("Sprites/MainSprites", game, EntityExtensions.ColliderShape.None, new Point(48, 48));


            GetComponent<SpriteRenderer>().LayerDepth = .7f;
        }
    }
}
