// -----------------------------------------------------------------------
// <copyright file="MedicGunItem.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using Exiled.API.Features.Attributes;
using Exiled.API.Features.Items;
using Exiled.API.Features.Spawn;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.BasicMessages;
using Mistaken.API.CustomItems;
using Mistaken.API.Extensions;
using Mistaken.API.GUI;
using Mistaken.RoundLogger;
using UnityEngine;

namespace Mistaken.NotEnoughItems.Items
{
    /// <inheritdoc/>
    [CustomItem(ItemType.GunRevolver)]
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
        public override string DisplayName => "Medic Gun";

        /// <inheritdoc/>
        public override float Weight { get; set; } = 0.75f;

        /// <inheritdoc/>
        public override SpawnProperties SpawnProperties { get; set; }

        /// <inheritdoc/>
        public override float Damage { get; set; } = 0;

        /// <inheritdoc/>
        public override byte ClipSize { get; set; } = 4;

        /// <inheritdoc/>
        public override void Give(Player player, bool displayMessage)
        {
            Exiled.API.Features.Items.Firearm firearm = (Exiled.API.Features.Items.Firearm)Item.Create(this.Type);
            firearm.Base.Status = new FirearmStatus(this.ClipSize, FirearmStatusFlags.MagazineInserted, 594);
            player.AddItem(firearm);
            RLogger.Log("MEDIC GUN", "GIVE", $"Given {this.Name} to {player.PlayerToString()}");
            this.TrackedSerials.Add(firearm.Serial);
            if (displayMessage)
                this.ShowPickedUpMessage(player);
        }

        /// <inheritdoc/>
        public override void Give(Player player, Pickup pickup, bool displayMessage = true)
        {
            FirearmPickup firearm = (FirearmPickup)pickup.Base;
            firearm.Status = new FirearmStatus(firearm.Status.Ammo, firearm.Status.Flags, 594);
            player.AddItem(pickup);
            RLogger.Log("MEDIC GUN", "GIVE", $"Given {this.Name} to {player.PlayerToString()}");
            this.TrackedSerials.Add(firearm.Info.Serial);
            if (displayMessage)
                this.ShowPickedUpMessage(player);
        }

        /// <inheritdoc/>
        public override Pickup Spawn(Vector3 position)
        {
            Exiled.API.Features.Items.Firearm firearm = (Exiled.API.Features.Items.Firearm)Item.Create(this.Type);
            firearm.Ammo = this.ClipSize;
            firearm.Scale = Size;
            RLogger.Log("MEDIC GUN", "SPAWN", $"Spawned {this.Name}");
            return this.Spawn(position, firearm);
        }

        /// <inheritdoc/>
        public override Pickup Spawn(Vector3 position, Item item)
        {
            Exiled.API.Features.Items.Firearm firearm = (Exiled.API.Features.Items.Firearm)item;
            firearm.Scale = Size;
            this.TrackedSerials.Add(firearm.Serial);
            var pickup = firearm.Spawn(position);
            ((FirearmPickup)pickup.Base).Status = new FirearmStatus(firearm.Ammo, FirearmStatusFlags.Cocked, 594);
            return pickup;
        }

        internal static readonly Vector3 Size = new Vector3(2, 2, 2);

        /// <inheritdoc/>
        protected override void ShowPickedUpMessage(Player player)
        {
            RLogger.Log("MEDIC GUN", "PICKUP", $"{player.PlayerToString()} Picked up an {this.Name}");
            player.SetGUI("medicgunpickedupmessage", PseudoGUIPosition.MIDDLE, string.Format(PluginHandler.Instance.Translation.ItemPickedUpMessage, PluginHandler.Instance.Translation.MedicGun), 2f);
        }

        /// <inheritdoc/>
        protected override void OnReloading(Exiled.Events.EventArgs.ReloadingWeaponEventArgs ev)
        {
            base.OnReloading(ev);
            ev.IsAllowed = false;
            if (ev.Firearm.Ammo >= this.ClipSize)
            {
                ev.Player.SetGUI("MedicGunWarn", PseudoGUIPosition.BOTTOM, PluginHandler.Instance.Translation.FullMagazineError, 3);
                return;
            }

            if (this.cooldowns.TryGetValue(ev.Firearm.Serial, out var date) && date > DateTime.Now)
            {
                ev.Player.SetGUI("grenadeLauncherWarn", PseudoGUIPosition.BOTTOM, $"You're on a reload cooldown, you need to wait {(date - DateTime.Now).TotalSeconds} seconds", 3);
                return;
            }

            if (!ev.Player.Items.Any(i => i.Type == ItemType.Adrenaline))
            {
                ev.Player.SetGUI("MedicGunWarn", PseudoGUIPosition.BOTTOM, string.Format(PluginHandler.Instance.Translation.NoAmmoError, PluginHandler.Instance.Translation.MedicGunAmmo), 3);
                return;
            }

            if (!this.cooldowns.ContainsKey(ev.Firearm.Serial))
                this.cooldowns.Add(ev.Firearm.Serial, DateTime.Now);
            this.cooldowns[ev.Firearm.Serial] = DateTime.Now.AddSeconds(5);
            RLogger.Log("MEDIC GUN", "RELOAD", $"Player {ev.Player.PlayerToString()} reloaded {this.Name}");
            ev.Player.RemoveItem(ev.Player.Items.First(i => i.Type == ItemType.Adrenaline));
            ev.Player.SetGUI("MedicGunWarn", PseudoGUIPosition.BOTTOM, PluginHandler.Instance.Translation.ReloadedInfo, 3);
            ev.Player.Connection.Send(new RequestMessage(ev.Firearm.Serial, RequestType.Reload));
            ev.Firearm.Ammo++;
            ev.IsAllowed = false;
        }

        /// <inheritdoc/>
        protected override void OnUnloadingWeapon(Exiled.Events.EventArgs.UnloadingWeaponEventArgs ev)
        {
            base.OnUnloadingWeapon(ev);
            ev.IsAllowed = false;
        }

        /// <inheritdoc/>
        protected override void OnShot(Exiled.Events.EventArgs.ShotEventArgs ev)
        {
            base.OnShot(ev);
            if (!(ev.Target is null))
            {
                var hpToHeal = Math.Min(ev.Target.MaxHealth - ev.Target.Health, PluginHandler.Instance.Config.HealAmount);
                var ahpToHeal = (PluginHandler.Instance.Config.HealAmount - hpToHeal) * 2f;
                ev.Target.Health += hpToHeal;
                if (Math.Floor(ahpToHeal) != 0)
                    ((PlayerStatsSystem.AhpStat)ev.Target.ReferenceHub.playerStats.StatModules[1]).ServerAddProcess(ahpToHeal, ahpToHeal, ahpToHeal / 10f, 0.65f, 15f, false);
                RLogger.Log("MEDIC GUN", "HEAL", $"Player {ev.Shooter.PlayerToString()} hit player {ev.Target.PlayerToString()} and regenerated {hpToHeal} hp and {ahpToHeal} ahp");
                ev.CanHurt = false;
                Hitmarker.SendHitmarker(ev.Shooter.Connection, 2f);
            }
        }

        /// <inheritdoc/>
        protected override void ShowSelectedMessage(Player player)
        {
        }

        private readonly Dictionary<ushort, DateTime> cooldowns = new Dictionary<ushort, DateTime>();
    }
}
