// -----------------------------------------------------------------------
// <copyright file="Config.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel;
using Exiled.API.Interfaces;

namespace Mistaken.NotEnoughItems
{
    internal sealed class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;

        [Description("If true then debug will be displayed")]
        public bool VerboseOutput { get; set; }

        [Description("Taser Settings")]
        public float HitCooldown { get; set; } = 90f;

        public float MissCooldown { get; set; } = 45f;

        [Description("Medic Gun Settings")]
        public float HealAmount { get; set; } = 35;
    }
}