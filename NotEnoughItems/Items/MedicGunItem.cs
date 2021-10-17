﻿// -----------------------------------------------------------------------
// <copyright file="MedicGunItem.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Spawn;
using Exiled.CustomItems.API.EventArgs;
using Exiled.CustomItems.API.Features;
using Exiled.Events.EventArgs;
using InventorySystem.Items.Firearms.BasicMessages;
using MEC;
using Mistaken.API.CustomItems;
using Mistaken.API.Extensions;
using Mistaken.API.GUI;
using Mistaken.Events.EventArgs;
using Mistaken.RoundLogger;
using UnityEngine;

namespace Mistaken.NotEnoughItems.Items
{
    /// <inheritdoc/>
    public class MedicGunItem : MistakenCustomWeapon
    {
        /// <inheritdoc/>
        public override MistakenCustomItems CustomItem => MistakenCustomItems.MEDIC_GUN;

        /// <inheritdoc/>
        public override bool AllowChangingAttachments => false;

        /// <inheritdoc/>
        public override ItemType Type { get; set; } = ItemType.GunRevolver;

        /// <inheritdoc/>
        public override string Name { get; set; } = "Medic Gun";

        /// <inheritdoc/>
        public override string Description { get; set; } = "Medic Gun";

        /// <inheritdoc/>
        public override float Weight { get; set; } = 0.75f;

        /// <inheritdoc/>
        public override SpawnProperties SpawnProperties { get; set; }

        /// <inheritdoc/>
        public override Modifiers Modifiers { get; set; }

        /// <inheritdoc/>
        public override float Damage { get; set; } = 0;

        /// <inheritdoc/>
        public override byte ClipSize { get; set; } = 4;

        /// <inheritdoc/>
        public override void Give(Player player, bool displayMessage)
        {
            Firearm item = (Firearm)player.AddItem(this.Type);
            item.Ammo = this.ClipSize;
            item.Base.Status = new InventorySystem.Items.Firearms.FirearmStatus(item.Ammo, item.Base.Status.Flags, 594);
            RLogger.Log("MEDIC GUN", "GIVE", $"Given {this.Name} to {player.PlayerToString()}");
            this.TrackedSerials.Add(item.Serial);
        }

        /// <inheritdoc/>
        public override Pickup Spawn(Vector3 position)
        {
            var item = new Firearm(this.Type);
            item.Ammo = this.ClipSize;
            RLogger.Log("MEDIC GUN", "SPAWN", $"Spawned {this.Name}");
            return this.Spawn(position, item);
        }

        /// <inheritdoc/>
        public override Pickup Spawn(Vector3 position, Item item)
        {
            var firearm = item as Firearm;
            firearm.Base.PickupDropModel.Info.Serial = firearm.Serial;
            firearm.Scale = Size;
            firearm.Base.Status = new InventorySystem.Items.Firearms.FirearmStatus(firearm.Ammo, InventorySystem.Items.Firearms.FirearmStatusFlags.Cocked, 594);
            this.TrackedSerials.Add(firearm.Serial);
            return firearm.Spawn(position);
        }

        internal static readonly Vector3 Size = new Vector3(2, 2, 2);

        /// <inheritdoc/>
        protected override void ShowPickedUpMessage(Player player)
        {
            RLogger.Log("MEDIC GUN", "PICKUP", $"{player.PlayerToString()} Picked up an {this.Name}");
            player.SetGUI("medicgunpickedupmessage", PseudoGUIPosition.MIDDLE, string.Format(PluginHandler.Instance.Translation.ItemPickedUpMessage, PluginHandler.Instance.Translation.MedicGun), 2f);
        }

        /// <inheritdoc/>
        protected override void OnReloading(ReloadingWeaponEventArgs ev)
        {
            if (ev.Firearm.Ammo >= this.ClipSize)
            {
                ev.Player.SetGUI("MedicGunWarn", PseudoGUIPosition.BOTTOM, PluginHandler.Instance.Translation.FullMagazineError, 3);
                ev.IsAllowed = false;
                return;
            }

            if (!ev.Player.Items.Any(i => i.Type == ItemType.Adrenaline))
            {
                ev.Player.SetGUI("MedicGunWarn", PseudoGUIPosition.BOTTOM, string.Format(PluginHandler.Instance.Translation.NoAmmoError, PluginHandler.Instance.Translation.MedicGunAmmo), 3);
                ev.IsAllowed = false;
                return;
            }

            RLogger.Log("MEDIC GUN", "RELOAD", $"Player {ev.Player.PlayerToString()} reloaded {this.Name}");
            ev.Player.RemoveItem(ev.Player.Items.First(i => i.Type == ItemType.Adrenaline));
            ev.Player.SetGUI("MedicGunWarn", PseudoGUIPosition.BOTTOM, PluginHandler.Instance.Translation.ReloadedInfo, 3);
            ev.Player.Connection.Send(new RequestMessage(ev.Firearm.Serial, RequestType.Reload));
            ev.Firearm.Ammo++;
            ev.IsAllowed = false;
        }

        /// <inheritdoc/>
        protected override void OnUnloadingFirearm(UnloadingFirearmEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        /// <inheritdoc/>
        protected override void OnShot(ShotEventArgs ev)
        {
            if (!(ev.Target is null))
            {
                var hpToHeal = Math.Min(ev.Target.MaxHealth - ev.Target.Health, PluginHandler.Instance.Config.HealAmount);
                var ahpToHeal = PluginHandler.Instance.Config.HealAmount - hpToHeal;
                ev.Target.Health += hpToHeal;
                ev.Target.ArtificialHealth += ahpToHeal;
                RLogger.Log("MEDIC GUN", "HEAL", $"Player {ev.Shooter.PlayerToString()} hit player {ev.Target.PlayerToString()} and regenerated {hpToHeal} hp and {ahpToHeal} ahp");
                ev.CanHurt = false;
            }
        }

        /// <inheritdoc/>
        protected override void ShowSelectedMessage(Player player)
        {
            Handlers.MedicGunHandler.Instance.RunCoroutine(this.UpdateInterface(player));
        }

        private IEnumerator<float> UpdateInterface(Player player)
        {
            yield return Timing.WaitForSeconds(0.1f);
            while (this.Check(player.CurrentItem))
            {
                player.SetGUI("ci_medic_gun_hold", PseudoGUIPosition.BOTTOM, string.Format(PluginHandler.Instance.Translation.ItemHoldingMessage, PluginHandler.Instance.Translation.MedicGun));
                yield return Timing.WaitForSeconds(1f);
            }

            player.SetGUI("ci_medic_gun_hold", PseudoGUIPosition.BOTTOM, null);
        }
    }
}