// -----------------------------------------------------------------------
// <copyright file="Config.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel;
using Exiled.API.Interfaces;
using Mistaken.Updater.Config;

namespace Mistaken.NotEnoughItems
{
    /// <inheritdoc/>
    public class Config : IAutoUpdatableConfig
    {
        /// <inheritdoc/>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets on hit cooldown.
        /// </summary>
        [Description("Taser Settings")]
        public float HitCooldown { get; set; } = 90f;

        /// <summary>
        /// Gets or sets on miss cooldown.
        /// </summary>
        public float MissCooldown { get; set; } = 45f;

        /// <summary>
        /// Gets or sets amount of hp healed in a single shot.
        /// </summary>
        [Description("Medic Gun Settings")]
        public float HealAmount { get; set; } = 35;

        /// <summary>
        /// Gets or sets a value indicating whether debug should be displayed.
        /// </summary>
        [Description("If true then debug will be displayed")]
        public bool VerbouseOutput { get; set; }

        /// <inheritdoc/>
        [Description("Auto Update Settings")]
        public System.Collections.Generic.Dictionary<string, string> AutoUpdateConfig { get; set; }
    }
}
