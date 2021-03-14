﻿using System.ComponentModel;
using Microsoft.Xna.Framework;

namespace Map_Editor.Misc.Properties
{
    /// <summary>
    /// Effect Property class.
    /// </summary>
    public class EffectProperty
    {
        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>The description.</value>
        [DisplayName("\t\tDescription")]
        [Category("\t\t\tObject")]
        public string Description
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the event ID.
        /// </summary>
        /// <value>The event ID.</value>
        [DisplayName("\tEvent ID")]
        [Category("\t\t\tObject")]
        public short EventID
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        [DisplayName("\tPath")]
        [Category("\t\tEffect")]
        public string Path
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the position.
        /// </summary>
        /// <value>The position.</value>
        [DisplayName("\t\t\tPosition")]
        [Category("\tWorld")]
        public Vector3 Position
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the rotation.
        /// </summary>
        /// <value>The rotation.</value>
        [DisplayName("\t\tRotation")]
        [Category("\tWorld")]
        public Quaternion Rotation
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the scale.
        /// </summary>
        /// <value>The scale.</value>
        [DisplayName("\tScale")]
        [Category("\tWorld")]
        public Vector3 Scale
        {
            get;
            set;
        }
    }
}