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
    internal sealed class PluginHandler : Plugin<Config, Translation>
    {
        public override string Author => "Mistaken Devs";

        public override string Name => "NotEnoughItems";

        public override string Prefix => "MNEI";

        public override PluginPriority Priority => PluginPriority.Default;

        public override Version RequiredExiledVersion => new (5, 2, 2);

        public override void OnEnabled()
        {
            Instance = this;
            harmony = new Harmony("mistaken.notenoughitems.patch");
            harmony.PatchAll();

            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            harmony.UnpatchAll();

            base.OnDisabled();
        }

        internal static PluginHandler Instance { get; private set; }

        private static Harmony harmony;
    }
}