// -----------------------------------------------------------------------
// <copyright file="GrenadeLauncherHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Exiled.API.Interfaces;
using Mistaken.API.Diagnostics;

namespace Mistaken.NotEnoughItems.Handlers
{
    /// <summary>
    /// Grenade that attaches to surfaces/players.
    /// </summary>
    public class GrenadeLauncherHandler : Module
    {
        /// <summary>
        /// Gets instance of the class.
        /// </summary>
        public static GrenadeLauncherHandler Instance { get; private set; }

        /// <inheritdoc cref="Module.Module(IPlugin{IConfig})"/>
        public GrenadeLauncherHandler(IPlugin<IConfig> plugin)
            : base(plugin)
        {
            Instance = this;
        }

        /// <inheritdoc/>
        public override string Name => nameof(GrenadeLauncherHandler);

        /// <inheritdoc/>
        public override void OnEnable()
        {
        }

        /// <inheritdoc/>
        public override void OnDisable()
        {
        }
    }
}
