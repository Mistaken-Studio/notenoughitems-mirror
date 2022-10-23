// -----------------------------------------------------------------------
// <copyright file="GrenadeLauncherItem.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

/*using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using Exiled.API.Features.Attributes;
using Exiled.API.Features.Items;
using Exiled.API.Features.Spawn;
using Exiled.Events.EventArgs;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.BasicMessages;
using JetBrains.Annotations;
using MEC;
using Mistaken.API.CustomItems;
using Mistaken.API.Diagnostics;
using Mistaken.API.Extensions;
using Mistaken.API.GUI;
using Mistaken.NotEnoughItems.Patches;
using Mistaken.RoundLogger;
using UnityEngine;

namespace Mistaken.NotEnoughItems.Items
{
    /// <inheritdoc/>
    [CustomItem(ItemType.GunCOM18)]
    [PublicAPI]
    public sealed class GrenadeLauncherItem : MistakenCustomWeapon
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
        public override void Give(Player player, bool displayMessage = true)
        {
            var pickup = this.CreateCorrectItem().Spawn(Vector3.zero);
            var firearm = (FirearmPickup)pickup.Base;
            firearm.NetworkStatus = new FirearmStatus(this.ClipSize, FirearmStatusFlags.Cocked, 82);
            player.AddItem(pickup);
            RLogger.Log("GRENADE LAUNCHER", "GIVE", $"Given {this.Name} to {player.PlayerToString()}");
            if (!GrenadeQueue.ContainsKey(pickup.Serial))
                GrenadeQueue.Add(pickup.Serial, this.AddRandomGrenades());

            this.TrackedSerials.Add(pickup.Serial);
            if (displayMessage)
                this.ShowPickedUpMessage(player);
        }

        /// <inheritdoc/>
        public override void Give(Player player, Pickup pickup, bool displayMessage = true)
        {
            var firearm = (FirearmPickup)pickup.Base;
            firearm.NetworkStatus = new FirearmStatus(this.ClipSize, FirearmStatusFlags.Cocked, 82);
            player.AddItem(pickup);
            RLogger.Log("GRENADE LAUNCHER", "GIVE", $"Given {this.Name} to {player.PlayerToString()}");
            if (!GrenadeQueue.ContainsKey(firearm.Info.Serial))
                GrenadeQueue.Add(firearm.Info.Serial, this.AddRandomGrenades());

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
            pickup.Scale = Size;
            this.TrackedSerials.Add(pickup.Serial);
            if (!GrenadeQueue.ContainsKey(pickup.Serial))
                GrenadeQueue.Add(pickup.Serial, this.AddRandomGrenades());
            ((FirearmPickup)pickup.Base).Status = new FirearmStatus(this.ClipSize, FirearmStatusFlags.Cocked, 82);

            return pickup;
        }

        /// <inheritdoc/>
        protected override void OnReloading(ReloadingWeaponEventArgs ev)
        {
            base.OnReloading(ev);
            ev.IsAllowed = false;
            if (!GrenadeQueue.ContainsKey(ev.Firearm.Serial))
            {
                Log.Error("Somehow key not found");
                GrenadeQueue.Add(ev.Firearm.Serial, new List<CustomGrenadeTypes>());
            }

            if (GrenadeQueue[ev.Firearm.Serial].Count >= this.ClipSize)
            {
                ev.Player.SetGUI("grenadeLauncherWarn", PseudoGUIPosition.BOTTOM, PluginHandler.Instance.Translation.FullMagazineError, 3);
                return;
            }

            if (Cooldowns.TryGetValue(ev.Firearm.Serial, out var date) && date > DateTime.Now)
            {
                ev.Player.SetGUI("grenadeLauncherWarn", PseudoGUIPosition.BOTTOM, $"You're on a reload cooldown, you need to wait {Math.Ceiling((date - DateTime.Now).TotalSeconds)} seconds", 3);
                return;
            }

            var item = ev.Player.Items.FirstOrDefault(i => i.Type == ItemType.GrenadeHE);
            if (item is null)
            {
                ev.Player.SetGUI("grenadeLauncherWarn", PseudoGUIPosition.BOTTOM, string.Format(PluginHandler.Instance.Translation.NoAmmoError, PluginHandler.Instance.Translation.GrenadeLauncherAmmo), 3);
                return;
            }

            if (!Cooldowns.ContainsKey(ev.Firearm.Serial))
                Cooldowns.Add(ev.Firearm.Serial, DateTime.Now);

            Cooldowns[ev.Firearm.Serial] = DateTime.Now.AddSeconds(5);
            RLogger.Log("GRENADE LAUNCHER", "RELOAD", $"Player {ev.Player.PlayerToString()} reloaded {this.Name}");
            GrenadeQueue[ev.Firearm.Serial].Add(this.GetTypeFromGrenade(item));
            ev.Player.RemoveItem(item);
            ev.Player.SetGUI("grenadeLauncherWarn", PseudoGUIPosition.BOTTOM, PluginHandler.Instance.Translation.ReloadedInfo, 3);
            ev.Player.Connection.Send(new RequestMessage(ev.Firearm.Serial, RequestType.Reload));
        }

        /// <inheritdoc/>
        protected override void OnUnloadingWeapon(UnloadingWeaponEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        /// <inheritdoc/>
        protected override void OnShooting(ShootingEventArgs ev)
        {
            base.OnShooting(ev);
            ev.IsAllowed = false;
            this.isShotAllowed = true;
            var serial = ev.Shooter.CurrentItem.Serial;
            if (!GrenadeQueue.ContainsKey(serial))
            {
                Log.Error("Somehow key not found");
                GrenadeQueue.Add(serial, new List<CustomGrenadeTypes>());
            }

            if (GrenadeQueue[serial].Count == 0)
            {
                ev.Shooter.SetGUI("grenadeLauncherWarn", PseudoGUIPosition.BOTTOM, PluginHandler.Instance.Translation.EmptyMagazineError, 3);
                this.isShotAllowed = false;
                return;
            }

            var name = "GrenadeHE";
            var toThrow = this.GetGrenadeFromType(GrenadeQueue[serial][0]);
            if (GrenadeQueue[serial][0] == CustomGrenadeTypes.STICKY)
            {
                StickyGrenadeItem.Throw(ev.Shooter.ReferenceHub, toThrow);
                name = StickyGrenadeItem.Instance.Name;
            }
            else if (GrenadeQueue[serial][0] == CustomGrenadeTypes.IMPACT)
            {
                ImpItem.Throw(ev.Shooter.ReferenceHub, toThrow);
                name = ImpItem.Instance.Name;
            }
            else
            {
                toThrow.Base.Owner = ev.Shooter.ReferenceHub;
                ServerThrowPatch.ThrowedItems.Add(toThrow.Base);
                toThrow.Base.ServerThrow(8.5f, 0.2f, new Vector3(10, 10, 0), ev.Shooter.ReferenceHub.playerMovementSync.PlayerVelocity);
            }

            RLogger.Log("GRENADE LAUNCHER", "FIRE", $"Player {ev.Shooter.PlayerToString()} fired {name}");
            GrenadeQueue[serial].RemoveAt(0);
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
            Module.RunSafeCoroutine(this.UpdateInterface(player), nameof(this.UpdateInterface));
        }

        /// <inheritdoc/>
        protected override void OnWaitingForPlayers()
        {
            base.OnWaitingForPlayers();
            Cooldowns.Clear();
            GrenadeQueue.Clear();
        }

        private static readonly Vector3 Size = new (2f, 1.5f, 1.5f);

        private static readonly Dictionary<ushort, DateTime> Cooldowns = new ();

        private static readonly Dictionary<ushort, List<CustomGrenadeTypes>> GrenadeQueue = new ();

        private bool isShotAllowed = true;

        private IEnumerator<float> UpdateInterface(Player player)
        {
            yield return Timing.WaitForSeconds(0.1f);
            while (this.Check(player.CurrentItem))
            {
                var serial = player.CurrentItem.Serial;
                var type = GrenadeQueue[serial].FirstOrDefault();
                string grenadeType = type switch
                {
                    CustomGrenadeTypes.FRAG => "HE Grenade",
                    CustomGrenadeTypes.STICKY => "Sticky Grenade",
                    CustomGrenadeTypes.IMPACT => "Impact Grenade",
                    _ => "None",
                };

                player.SetGUI("grenade_launcher_ammo", PseudoGUIPosition.BOTTOM, $"Current grenade type: {grenadeType}");
                yield return Timing.WaitForSeconds(1f);
            }

            player.SetGUI("grenade_launcher_ammo", PseudoGUIPosition.BOTTOM, null);
        }

        private List<CustomGrenadeTypes> AddRandomGrenades()
        {
            var tor = new List<CustomGrenadeTypes>();
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
            if (item is not Throwable && item.Type != ItemType.GrenadeHE)
                return CustomGrenadeTypes.NONE;
            else if (StickyGrenadeItem.Instance.Check(item))
                return CustomGrenadeTypes.STICKY;

            // ReSharper disable once ConvertIfStatementToReturnStatement
            else if (ImpItem.Instance.Check(item))
                return CustomGrenadeTypes.IMPACT;

            return CustomGrenadeTypes.FRAG;
        }
    }
}*/