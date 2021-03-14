﻿using Map_Editor.Engine.Commands.Interfaces;

namespace Map_Editor.Engine.Commands.Tiles
{
    /// <summary>
    /// Placement class.
    /// </summary>
    public class Placement : ICommand
    {
        /// <summary>
        /// Tile structure.
        /// </summary>
        public struct Tile
        {
            #region Member Declarations

            /// <summary>
            /// Gets or sets the X.
            /// </summary>
            /// <value>The X.</value>
            public int X { get; set; }

            /// <summary>
            /// Gets or sets the Y.
            /// </summary>
            /// <value>The Y.</value>
            public int Y { get; set; }

            /// <summary>
            /// Gets or sets the tile X.
            /// </summary>
            /// <value>The tile X.</value>
            public int TileX { get; set; }

            /// <summary>
            /// Gets or sets the tile Y.
            /// </summary>
            /// <value>The tile Y.</value>
            public int TileY { get; set; }

            /// <summary>
            /// Gets or sets the new ID.
            /// </summary>
            /// <value>The new ID.</value>
            public int NewID { get; set; }

            /// <summary>
            /// Gets or sets the old ID.
            /// </summary>
            /// <value>The old ID.</value>
            public int OldID { get; set; }

            #endregion
        };

        #region Member Declarations

        /// <summary>
        /// Gets or sets the tiles.
        /// </summary>
        /// <value>The tiles.</value>
        public Tile[] Tiles { get; set; }

        #endregion

        /// <summary>
        /// Undo command.
        /// </summary>
        public void Undo()
        {
            for (int i = 0; i < Tiles.Length; i++)
                MapManager.Heightmaps.Blocks[Tiles[i].Y, Tiles[i].X].ChangeTile(Tiles[i].TileX, Tiles[i].TileY, Tiles[i].OldID);

            if (ToolManager.GetToolMode() != ToolManager.ToolMode.Tiles)
            {
                for (int y = 0; y < 100; y++)
                {
                    for (int x = 0; x < 100; x++)
                    {
                        if (MapManager.Heightmaps.Blocks[y, x] == null)
                            continue;

                        MapManager.Heightmaps.Blocks[y, x].BuildBuffer();
                    }
                }
            }
        }

        /// <summary>
        /// Redo command.
        /// </summary>
        public void Redo()
        {
            for (int i = 0; i < Tiles.Length; i++)
                MapManager.Heightmaps.Blocks[Tiles[i].Y, Tiles[i].X].ChangeTile(Tiles[i].TileX, Tiles[i].TileY, Tiles[i].NewID);

            if (ToolManager.GetToolMode() != ToolManager.ToolMode.Tiles)
            {
                for (int y = 0; y < 100; y++)
                {
                    for (int x = 0; x < 100; x++)
                    {
                        if (MapManager.Heightmaps.Blocks[y, x] == null)
                            continue;

                        MapManager.Heightmaps.Blocks[y, x].BuildBuffer();
                    }
                }
            }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <returns>Name.</returns>
        public string GetName()
        {
            return "Tile Placement";
        }
    }
}