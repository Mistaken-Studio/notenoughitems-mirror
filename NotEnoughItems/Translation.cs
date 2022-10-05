// -----------------------------------------------------------------------
// <copyright file="Translation.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Exiled.API.Interfaces;

namespace Mistaken.NotEnoughItems
{
    internal class Translation : ITranslation
    {
        public string GrenadeLauncher { get; set; } = "Grenade Launcher";

        public string GrenadeLauncherAmmo { get; set; } = "Grenade HE";

        public string ImpactGrenade { get; set; } = "Impact Grenade";

        public string MedicGun { get; set; } = "Medic Gun";

        public string MedicGunAmmo { get; set; } = "Adrenaline";

        public string StickyGrenade { get; set; } = "Sticky Grenade";

        public string Taser { get; set; } = "Taser";

        public string TaserHold { get; set; } =
            "You are holding <color=yellow>Taser</color><br><mspace=0.5em><color=yellow>[<color=green>{0}</color>]</color></mspace>";

        public string TaserNoAmmo { get; set; } = "You have no ammo";

        public string TaserPlayerTased { get; set; } = "<color=yellow>You have been tased by: {0} [{1}]</color>";

        public string ItemHoldingMessage { get; set; } = "You are holding <color=yellow>{0}</color>";

        public string ItemPickedUpMessage { get; set; } = "You have picked up <color=yellow>{0}</color>";

        public string NoAmmoError { get; set; } = "You have no ammo (<color=yellow>{0}</color>)";

        public string ReloadedInfo { get; set; } = "Reloaded";

        public string EmptyMagazineError { get; set; } = "You can't fire with empty magazine";

        public string FullMagazineError { get; set; } = "You can't reload a full magazine";
    }
}