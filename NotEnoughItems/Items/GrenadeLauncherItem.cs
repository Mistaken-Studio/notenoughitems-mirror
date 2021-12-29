// -----------------------------------------------------------------------
// <copyright file="GrenadeLauncherItem.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Spawn;
using Exiled.CustomItems.API.Features;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.BasicMessages;
using MEC;
using Mistaken.API.CustomItems;
using Mistaken.API.Extensions;
using Mistaken.API.GUI;
using Mistaken.RoundLogger;
using UnityEngine;

namespace Mistaken.NotEnoughItems.Items
{
    /// <inheritdoc/>
    public class GrenadeLauncherItem : MistakenCustomWeapon
    {
        /// <inheritdoc/>
        public override MistakenCustomItems CustomItem => MistakenCustomItems.GRENADE_LAUNCHER;

        /// <inheritdoc/>
        public override bool AllowChangingAttachments => false;

        /// <inheritdoc/>
        public override ItemType Type { get; set; } = ItemType.GunCOM18;

        /// <inheritdoc/>
        public override string Name { get; set; } = "Grenade Launcher";

        /// <inheritdoc/>
        public override string Description { get; set; } = "Sticky Grenade Launcher";

        /// <inheritdoc/>
        public override float Weight { get; set; } = 0.7f;

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
            Exiled.API.Features.Items.Firearm firearm = new Exiled.API.Features.Items.Firearm(this.Type);
            firearm.Base.Status = new FirearmStatus(this.ClipSize, FirearmStatusFlags.Cocked, 82);
            player.AddItem(firearm);
            RLogger.Log("GRENADE LAUNCHER", "GIVE", $"{this.Name} given to {player.PlayerToString()}");

            this.TrackedSerials.Add(firearm.Serial);
            if (displayMessage)
                this.ShowPickedUpMessage(player);
        }

        /// <inheritdoc/>
        public override void Give(Player player, Pickup pickup, bool displayMessage = true)
        {
            FirearmPickup firearm = (FirearmPickup)pickup.Base;
            player.AddItem(pickup);
            firearm.Status = new FirearmStatus(firearm.Status.Ammo, firearm.Status.Flags, 82);
            RLogger.Log("GRENADE LAUNCHER", "GIVE", $"Given {this.Name} to {player.PlayerToString()}");

            this.TrackedSerials.Add(firearm.Info.Serial);
            if (displayMessage)
                this.ShowPickedUpMessage(player);
        }

        /// <inheritdoc/>
        public override Pickup Spawn(Vector3 position)
        {
            Exiled.API.Features.Items.Firearm firearm = new Exiled.API.Features.Items.Firearm(this.Type);
            RLogger.Log("GRENADE LAUNCHER", "SPAWN", $"{this.Name} spawned");
            return this.Spawn(position, firearm);
        }

        /// <inheritdoc/>
        public override Pickup Spawn(Vector3 position, Item item)
        {
            var firearm = item as Exiled.API.Features.Items.Firearm;
            firearm.Scale = Size;
            firearm.Base.PickupDropModel.Info.Serial = firearm.Serial;
            this.TrackedSerials.Add(firearm.Serial);
            var pickup = firearm.Spawn(position);
            ((FirearmPickup)pickup.Base).Status = new FirearmStatus(firearm.Ammo, FirearmStatusFlags.Cocked, 82);
            return pickup;
        }

        internal static readonly Vector3 Size = new Vector3(2f, 1.5f, 1.5f);

        /// <inheritdoc/>
        protected override void OnReloading(Exiled.Events.EventArgs.ReloadingWeaponEventArgs ev)
        {
            if (ev.Firearm.Ammo >= this.ClipSize)
            {
                ev.Player.SetGUI("grenadeLauncherWarn", PseudoGUIPosition.BOTTOM, PluginHandler.Instance.Translation.FullMagazineError, 3);
                ev.IsAllowed = false;
                return;
            }

            if (!ev.Player.Items.Any(i => i.Type == ItemType.GrenadeHE))
            {
                ev.Player.SetGUI("grenadeLauncherWarn", PseudoGUIPosition.BOTTOM, string.Format(PluginHandler.Instance.Translation.NoAmmoError, PluginHandler.Instance.Translation.GrenadeLauncherAmmo), 3);
                ev.IsAllowed = false;
                return;
            }

            RLogger.Log("GRENADE LAUNCHER", "RELOAD", $"Player {ev.Player.PlayerToString()} reloaded {this.Name}");
            ev.Player.RemoveItem(ev.Player.Items.First(i => i.Type == ItemType.GrenadeHE));
            ev.Player.SetGUI("grenadeLauncherWarn", PseudoGUIPosition.BOTTOM, PluginHandler.Instance.Translation.ReloadedInfo, 3);
            ev.Player.Connection.Send(new RequestMessage(ev.Firearm.Serial, RequestType.Reload));
            ev.Firearm.Ammo++;
            ev.IsAllowed = false;
        }

        /// <inheritdoc/>
        protected override void OnUnloadingWeapon(Exiled.Events.EventArgs.UnloadingWeaponEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        /// <inheritdoc/>
        protected override void OnShooting(Exiled.Events.EventArgs.ShootingEventArgs ev)
        {
            if (((Exiled.API.Features.Items.Firearm)ev.Shooter.CurrentItem).Ammo == 0)
            {
                ev.Shooter.SetGUI("grenadeLauncherWarn", PseudoGUIPosition.BOTTOM, PluginHandler.Instance.Translation.EmptyMagazineError, 3);
                ev.IsAllowed = false;
                return;
            }

            StickyGrenadeItem.Throw(ev.Shooter);
            RLogger.Log("GRENADE LAUNCHER", "FIRE", $"Player {ev.Shooter.PlayerToString()} fired {this.Name}");
            ((Exiled.API.Features.Items.Firearm)ev.Shooter.CurrentItem).Ammo--;
            Hitmarker.SendHitmarker(ev.Shooter.Connection, 5f);
            ev.IsAllowed = false;
            return;
        }

        /// <inheritdoc/>
        protected override void ShowPickedUpMessage(Player player)
        {
            RLogger.Log("GRENADE LAUNCHER", "PICKUP", $"{player.PlayerToString()} Picked up an {this.Name}");
            player.SetGUI("glpickedupmessage", PseudoGUIPosition.MIDDLE, string.Format(PluginHandler.Instance.Translation.ItemPickedUpMessage, PluginHandler.Instance.Translation.GrenadeLauncher), 2f);
        }

        /// <inheritdoc/>
        protected override void ShowSelectedMessage(Player player)
        {
            Handlers.GrenadeLauncherHandler.Instance.RunCoroutine(this.UpdateInterface(player));
        }

        private IEnumerator<float> UpdateInterface(Player player)
        {
            bool ammogiven = false;
            yield return Timing.WaitForSeconds(0.1f);
            while (this.Check(player.CurrentItem))
            {
                if (!ammogiven)
                {
                    player.Ammo[ItemType.Ammo9x19]++;
                    ammogiven = true;
                }

                player.SetGUI("grenadeLauncher", PseudoGUIPosition.BOTTOM, string.Format(PluginHandler.Instance.Translation.ItemHoldingMessage, PluginHandler.Instance.Translation.GrenadeLauncher));
                yield return Timing.WaitForSeconds(1);
            }

            player.Ammo[ItemType.Ammo9x19]--;
            player.SetGUI("grenadeLauncher", PseudoGUIPosition.BOTTOM, null);
        }
    }
}
