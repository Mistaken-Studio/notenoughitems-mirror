// -----------------------------------------------------------------------
// <copyright file="MedicGunHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Exiled.API.Interfaces;
using Mistaken.API.Diagnostics;

namespace Mistaken.NotEnoughItems.Handlers
{
    /// <summary>
    /// Gun that heals hit players.
    /// </summary>
    public class MedicGunHandler : Module
    {
        /// <inheritdoc cref="Module.Module(IPlugin{IConfig})"/>
        public MedicGunHandler(IPlugin<IConfig> plugin)
            : base(plugin)
        {
            Instance = this;
        }

        /// <inheritdoc/>
        public override string Name => nameof(MedicGunHandler);

        /// <inheritdoc/>
        public override void OnEnable()
        {
        }

        /// <inheritdoc/>
        public override void OnDisable()
        {
        }

        internal static MedicGunHandler Instance { get; private set; }
    }
}
