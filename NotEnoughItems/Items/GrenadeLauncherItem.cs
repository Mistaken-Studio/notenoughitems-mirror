// -----------------------------------------------------------------------
// <copyright file="GrenadeLauncherItem.cs" company="Mistaken">
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
using Mistaken.API.Diagnostics;
using Mistaken.API.Extensions;
using Mistaken.API.GUI;
using Mistaken.RoundLogger;
using UnityEngine;

namespace Mistaken.NotEnoughItems.Items
{
    /// <inheritdoc/>
    [CustomItem(ItemType.GunCOM18)]
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
        public override float Damage { get; set; } = 0;

        /// <inheritdoc/>
        public override byte ClipSize { get; set; } = 4;

        /// <inheritdoc/>
        public override void Give(Player player, bool displayMessage)
        {
            Exiled.API.Features.Items.Firearm firearm = (Exiled.API.Features.Items.Firearm)Item.Create(this.Type);
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
            firearm.Status = new FirearmStatus(this.ClipSize, FirearmStatusFlags.Cocked, 82);
            player.AddItem(pickup);
            RLogger.Log("GRENADE LAUNCHER", "GIVE", $"Given {this.Name} to {player.PlayerToString()}");
            if (!this.grenadeQueue.ContainsKey(firearm.Info.Serial))
                this.grenadeQueue.Add(firearm.Info.Serial, this.AddRandomGrenades());
            this.TrackedSerials.Add(firearm.Info.Serial);
            if (displayMessage)
                this.ShowPickedUpMessage(player);
        }

        /// <inheritdoc/>
        public override Pickup Spawn(Vector3 position, Player previousOwner = null)
        {
            return this.Spawn(position, this.CreateCorrectItem(), previousOwner);
        }

        /// <inheritdoc/>
        public override Pickup Spawn(Vector3 position, Item item, Player previousOwner = null)
        {
            var pickup = base.Spawn(position, item, previousOwner);
            RLogger.Log("GRENADE LAUNCHER", "SPAWN", $"{this.Name} spawned");
            var firearm = item.Base as InventorySystem.Items.Firearms.Firearm;
            pickup.Scale = Size;
            this.TrackedSerials.Add(pickup.Serial);
            if (!this.grenadeQueue.ContainsKey(pickup.Serial))
                this.grenadeQueue.Add(pickup.Serial, this.AddRandomGrenades());
            ((FirearmPickup)pickup.Base).Status = new FirearmStatus(this.ClipSize, FirearmStatusFlags.Cocked, 82);
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
            this.isShotAllowed = true;
            var serial = ev.Shooter.CurrentItem.Serial;
            if (!this.grenadeQueue.ContainsKey(serial))
            {
                Log.Error("Somehow key not found");
                this.grenadeQueue.Add(serial, new List<CustomGrenadeTypes>());
            }

            if (this.grenadeQueue[serial].Count == 0)
            {
                ev.Shooter.SetGUI("grenadeLauncherWarn", PseudoGUIPosition.BOTTOM, PluginHandler.Instance.Translation.EmptyMagazineError, 3);
                this.isShotAllowed = false;
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
        protected override void OnPlayingGunAudio(PlayingGunAudioEventArgs ev)
        {
            base.OnPlayingGunAudio(ev);
            ev.IsAllowed = this.isShotAllowed;
        }

        /// <inheritdoc/>
        protected override void ShowSelectedMessage(Player player)
        {
            Module.RunSafeCoroutine(this.UpdateInterface(player), "GrenadeLauncherItem_UpdateInterface");
        }

        private readonly Dictionary<ushort, List<CustomGrenadeTypes>> grenadeQueue = new Dictionary<ushort, List<CustomGrenadeTypes>>();

        private readonly Dictionary<ushort, DateTime> cooldowns = new Dictionary<ushort, DateTime>();

        private bool isShotAllowed = true;

        private IEnumerator<float> UpdateInterface(Player player)
        {
            yield return MEC.Timing.WaitForSeconds(0.1f);
            while (this.Check(player.CurrentItem))
            {
                var serial = player.CurrentItem.Serial;
                var type = this.grenadeQueue[serial].FirstOrDefault();
                string grenadeType;
                switch (type)
                {
                    case CustomGrenadeTypes.FRAG:
                        grenadeType = "HE Grenade";
                        break;
                    case CustomGrenadeTypes.STICKY:
                        grenadeType = "Sticky Grenade";
                        break;
                    case CustomGrenadeTypes.IMPACT:
                        grenadeType = "Impact Grenade";
                        break;
                    default:
                        grenadeType = "None";
                        break;
                }

                player.SetGUI("grenade_launcher_ammo", PseudoGUIPosition.BOTTOM, $"Current grenade type: {grenadeType}");
                yield return MEC.Timing.WaitForSeconds(1f);
            }

            player.SetGUI("grenade_launcher_ammo", PseudoGUIPosition.BOTTOM, null);
        }

        private List<CustomGrenadeTypes> AddRandomGrenades()
        {
            List<CustomGrenadeTypes> tor = new List<CustomGrenadeTypes>();
            while (tor.Count != 4)
                tor.Add((CustomGrenadeTypes)UnityEngine.Random.Range(1, 4));

            return tor;
        }

        private ExplosiveGrenade GetGrenadeFromType(CustomGrenadeTypes type, Player owner = null)
        {
            Item grenade = null;
            switch (type)
            {
                case CustomGrenadeTypes.FRAG:
                    {
                        grenade = Item.Create(ItemType.GrenadeHE, owner);
                        break;
                    }

                case CustomGrenadeTypes.STICKY:
                    {
                        grenade = Item.Create(ItemType.GrenadeHE, owner);
                        StickyGrenadeItem.Instance.TrackedSerials.Add(grenade.Serial);
                        break;
                    }

                case CustomGrenadeTypes.IMPACT:
                    {
                        grenade = Item.Create(ItemType.GrenadeHE, owner);
                        ImpItem.Instance.TrackedSerials.Add(grenade.Serial);
                        break;
                    }
            }

            return (ExplosiveGrenade)grenade;
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
