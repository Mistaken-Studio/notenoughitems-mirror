// -----------------------------------------------------------------------
// <copyright file="PluginHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Exiled.API.Enums;
using Exiled.API.Features;
using HarmonyLib;

namespace Mistaken.NotEnoughItems
{
    internal class PluginHandler : Plugin<Config, Translation>
    {
        private static Harmony harmony;

        /// <inheritdoc />
        public override string Author => "Mistaken Devs";

        /// <inheritdoc />
        public override string Name => "NotEnoughItems";

        /// <inheritdoc />
        public override string Prefix => "MNEI";

        /// <inheritdoc />
        public override PluginPriority Priority => PluginPriority.Default;

        /// <inheritdoc />
        public override Version RequiredExiledVersion => new(5, 0, 0);

        internal static PluginHandler Instance { get; private set; }

        /// <inheritdoc />
        public override void OnEnabled()
        {
            Instance = this;
            harmony = new Harmony("mistaken.notenoughitems.patch");
            harmony.PatchAll();

            base.OnEnabled();
        }

        /// <inheritdoc />
        public override void OnDisabled()
        {
            harmony.UnpatchAll();

            base.OnDisabled();
        }
    }
}