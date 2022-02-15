// -----------------------------------------------------------------------
// <copyright file="GrenadeLauncherItem.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Spawn;
using Exiled.CustomItems.API.Features;
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
        public override string DisplayName => "Grenade Launcher";

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
            RLogger.Log("GRENADE LAUNCHER", "GIVE", $"Given {this.Name} to {player.PlayerToString()}");
            if (!this.grenadeQueue.ContainsKey(firearm.Serial))
                this.grenadeQueue.Add(firearm.Serial, this.AddRandomGrenades());
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
            if (!this.grenadeQueue.ContainsKey(firearm.Info.Serial))
                this.grenadeQueue.Add(firearm.Info.Serial, this.AddRandomGrenades());
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
            if (!this.grenadeQueue.ContainsKey(firearm.Serial))
                this.grenadeQueue.Add(firearm.Serial, this.AddRandomGrenades());
            var pickup = firearm.Spawn(position);
            ((FirearmPickup)pickup.Base).Status = new FirearmStatus(firearm.Ammo, FirearmStatusFlags.Cocked, 82);
            return pickup;
        }

        internal static readonly Vector3 Size = new Vector3(2f, 1.5f, 1.5f);

        /// <inheritdoc/>
        protected override void OnReloading(Exiled.Events.EventArgs.ReloadingWeaponEventArgs ev)
        {
            base.OnReloading(ev);
            ev.IsAllowed = false;
            if (!this.grenadeQueue.ContainsKey(ev.Firearm.Serial))
            {
                Log.Error("Somehow key not found");
                this.grenadeQueue.Add(ev.Firearm.Serial, new List<CustomGrenadeTypes>());
            }

            if (this.grenadeQueue[ev.Firearm.Serial].Count >= this.ClipSize)
            {
                ev.Player.SetGUI("grenadeLauncherWarn", PseudoGUIPosition.BOTTOM, PluginHandler.Instance.Translation.FullMagazineError, 3);
                return;
            }

            if (this.cooldowns.TryGetValue(ev.Firearm.Serial, out var date) && date > DateTime.Now)
            {
                ev.Player.SetGUI("grenadeLauncherWarn", PseudoGUIPosition.BOTTOM, $"You're on a reload cooldown, you need to wait {(date - DateTime.Now).TotalSeconds} seconds", 3);
                return;
            }

            var item = ev.Player.Items.FirstOrDefault(i => i.Type == ItemType.GrenadeHE);
            if (item is null)
            {
                ev.Player.SetGUI("grenadeLauncherWarn", PseudoGUIPosition.BOTTOM, string.Format(PluginHandler.Instance.Translation.NoAmmoError, PluginHandler.Instance.Translation.GrenadeLauncherAmmo), 3);
                return;
            }

            if (!this.cooldowns.ContainsKey(ev.Firearm.Serial))
                this.cooldowns.Add(ev.Firearm.Serial, DateTime.Now);
            this.cooldowns[ev.Firearm.Serial] = DateTime.Now.AddSeconds(5);
            RLogger.Log("GRENADE LAUNCHER", "RELOAD", $"Player {ev.Player.PlayerToString()} reloaded {this.Name}");
            this.grenadeQueue[ev.Firearm.Serial].Add(this.GetTypeFromGrenade(item));
            ev.Player.RemoveItem(item);
            ev.Player.SetGUI("grenadeLauncherWarn", PseudoGUIPosition.BOTTOM, PluginHandler.Instance.Translation.ReloadedInfo, 3);
            ev.Player.Connection.Send(new RequestMessage(ev.Firearm.Serial, RequestType.Reload));
        }

        /// <inheritdoc/>
        protected override void OnUnloadingWeapon(Exiled.Events.EventArgs.UnloadingWeaponEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        /// <inheritdoc/>
        protected override void OnShooting(Exiled.Events.EventArgs.ShootingEventArgs ev)
        {
            base.OnShooting(ev);
            ev.IsAllowed = false;
            var serial = ev.Shooter.CurrentItem.Serial;
            if (!this.grenadeQueue.ContainsKey(serial))
            {
                Log.Error("Somehow key not found");
                this.grenadeQueue.Add(serial, new List<CustomGrenadeTypes>());
            }

            if (this.grenadeQueue[serial].Count == 0)
            {
                ev.Shooter.SetGUI("grenadeLauncherWarn", PseudoGUIPosition.BOTTOM, PluginHandler.Instance.Translation.EmptyMagazineError, 3);
                return;
            }

            string name = "GrenadeHE";
            var toThrow = this.GetGrenadeFromType(this.grenadeQueue[serial][0]);
            if (this.grenadeQueue[serial][0] == CustomGrenadeTypes.STICKY)
            {
                StickyGrenadeItem.Throw(ev.Shooter.ReferenceHub, toThrow);
                name = StickyGrenadeItem.Instance.Name;
            }
            else if (this.grenadeQueue[serial][0] == CustomGrenadeTypes.IMPACT)
            {
                ImpItem.Throw(ev.Shooter.ReferenceHub, toThrow);
                name = ImpItem.Instance.Name;
            }
            else
            {
                toThrow.Base.Owner = ev.Shooter.ReferenceHub;
                Patches.ServerThrowPatch.ThrowedItems.Add(toThrow.Base);
                toThrow.Base.ServerThrow(8.5f, 0.2f, new Vector3(10, 10, 0), ev.Shooter.ReferenceHub.playerMovementSync.PlayerVelocity);
            }

            RLogger.Log("GRENADE LAUNCHER", "FIRE", $"Player {ev.Shooter.PlayerToString()} fired {name}");
            this.grenadeQueue[serial].RemoveAt(0);
            Hitmarker.SendHitmarker(ev.Shooter.Connection, 3f);
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
        }

        private readonly Dictionary<ushort, List<CustomGrenadeTypes>> grenadeQueue = new Dictionary<ushort, List<CustomGrenadeTypes>>();

        private readonly Dictionary<ushort, DateTime> cooldowns = new Dictionary<ushort, DateTime>();

        private List<CustomGrenadeTypes> AddRandomGrenades()
        {
            List<CustomGrenadeTypes> tor = new List<CustomGrenadeTypes>();
            while (tor.Count != 4)
                tor.Add((CustomGrenadeTypes)UnityEngine.Random.Range(0, 3));

            return tor;
        }

        private ExplosiveGrenade GetGrenadeFromType(CustomGrenadeTypes type, Player owner = null)
        {
            ExplosiveGrenade grenade = null;
            switch (type)
            {
                case CustomGrenadeTypes.FRAG:
                    {
                        grenade = new ExplosiveGrenade(ItemType.GrenadeHE, owner);
                        return grenade;
                    }

                case CustomGrenadeTypes.STICKY:
                    {
                        grenade = new ExplosiveGrenade(ItemType.GrenadeHE, owner);
                        StickyGrenadeItem.Instance.TrackedSerials.Add(grenade.Serial);
                        return grenade;
                    }

                case CustomGrenadeTypes.IMPACT:
                    {
                        grenade = new ExplosiveGrenade(ItemType.GrenadeHE, owner);
                        ImpItem.Instance.TrackedSerials.Add(grenade.Serial);
                        return grenade;
                    }

                default:
                    return grenade;
            }
        }

        private CustomGrenadeTypes GetTypeFromGrenade(Item item)
        {
            if (!(item is Throwable) && item.Type != ItemType.GrenadeHE)
                return CustomGrenadeTypes.NONE;
            if (StickyGrenadeItem.Instance.Check(item))
                return CustomGrenadeTypes.STICKY;
            else if (ImpItem.Instance.Check(item))
                return CustomGrenadeTypes.IMPACT;
            else
                return CustomGrenadeTypes.FRAG;
        }
    }
}
