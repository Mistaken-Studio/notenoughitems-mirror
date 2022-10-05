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
using Random = UnityEngine.Random;

namespace Mistaken.NotEnoughItems.Items
{
    /// <inheritdoc />
    [CustomItem(ItemType.GunCOM18)]
    [PublicAPI]
    public class GrenadeLauncherItem : MistakenCustomWeapon
    {
        private static readonly Vector3 Size = new(2f, 1.5f, 1.5f);

        private readonly Dictionary<ushort, DateTime> cooldowns = new();

        private readonly Dictionary<ushort, List<CustomGrenadeTypes>> grenadeQueue = new();

        private bool isShotAllowed = true;

        /// <inheritdoc />
        public override MistakenCustomItems CustomItem => MistakenCustomItems.GRENADE_LAUNCHER;

        /// <inheritdoc />
        public override bool AllowChangingAttachments => false;

        /// <inheritdoc />
        public override ItemType Type { get; set; } = ItemType.GunCOM18;

        /// <inheritdoc />
        public override string Name { get; set; } = "Grenade Launcher";

        /// <inheritdoc />
        public override string Description { get; set; } = "Sticky Grenade Launcher";

        /// <inheritdoc />
        public override string DisplayName => "Grenade Launcher";

        /// <inheritdoc />
        public override float Weight { get; set; } = 0.7f;

        /// <inheritdoc />
        public override SpawnProperties SpawnProperties { get; set; }

        /// <inheritdoc />
        public override float Damage { get; set; } = 0;

        /// <inheritdoc />
        public override byte ClipSize { get; set; } = 4;

        /// <inheritdoc />
        public override void Give(Player player, bool displayMessage = true)
        {
            var pickup = CreateCorrectItem().Spawn(Vector3.zero);
            var firearm = (FirearmPickup)pickup.Base;
            firearm.NetworkStatus = new FirearmStatus(ClipSize, FirearmStatusFlags.Cocked, 82);
            player.AddItem(pickup);
            RLogger.Log("GRENADE LAUNCHER", "GIVE", $"Given {Name} to {player.PlayerToString()}");

            if (!grenadeQueue.ContainsKey(pickup.Serial))
                grenadeQueue.Add(pickup.Serial, AddRandomGrenades());
            TrackedSerials.Add(pickup.Serial);
            if (displayMessage)
                ShowPickedUpMessage(player);
        }

        /// <inheritdoc />
        public override void Give(Player player, Pickup pickup, bool displayMessage = true)
        {
            var firearm = (FirearmPickup)pickup.Base;
            firearm.NetworkStatus = new FirearmStatus(ClipSize, FirearmStatusFlags.Cocked, 82);
            player.AddItem(pickup);
            RLogger.Log("GRENADE LAUNCHER", "GIVE", $"Given {Name} to {player.PlayerToString()}");

            if (!grenadeQueue.ContainsKey(firearm.Info.Serial))
                grenadeQueue.Add(firearm.Info.Serial, AddRandomGrenades());
            TrackedSerials.Add(firearm.Info.Serial);
            if (displayMessage)
                ShowPickedUpMessage(player);
        }

        /// <inheritdoc />
        public override Pickup Spawn(Vector3 position, Player previousOwner = null)
        {
            return Spawn(position, CreateCorrectItem(), previousOwner);
        }

        /// <inheritdoc />
        public override Pickup Spawn(Vector3 position, Item item, Player previousOwner = null)
        {
            var pickup = base.Spawn(position, item, previousOwner);
            RLogger.Log("GRENADE LAUNCHER", "SPAWN", $"{Name} spawned");

            pickup.Scale = Size;
            TrackedSerials.Add(pickup.Serial);
            if (!grenadeQueue.ContainsKey(pickup.Serial))
                grenadeQueue.Add(pickup.Serial, AddRandomGrenades());
            ((FirearmPickup)pickup.Base).Status = new FirearmStatus(ClipSize, FirearmStatusFlags.Cocked, 82);
            return pickup;
        }

        /// <inheritdoc />
        protected override void OnReloading(ReloadingWeaponEventArgs ev)
        {
            base.OnReloading(ev);
            ev.IsAllowed = false;
            if (!grenadeQueue.ContainsKey(ev.Firearm.Serial))
            {
                Log.Error("Somehow key not found");
                grenadeQueue.Add(ev.Firearm.Serial, new List<CustomGrenadeTypes>());
            }

            if (grenadeQueue[ev.Firearm.Serial].Count >= ClipSize)
            {
                ev.Player.SetGUI("grenadeLauncherWarn", PseudoGUIPosition.BOTTOM,
                    PluginHandler.Instance.Translation.FullMagazineError, 3);
                return;
            }

            if (cooldowns.TryGetValue(ev.Firearm.Serial, out var date) && date > DateTime.Now)
            {
                ev.Player.SetGUI("grenadeLauncherWarn", PseudoGUIPosition.BOTTOM,
                    $"You're on a reload cooldown, you need to wait {Math.Ceiling((date - DateTime.Now).TotalSeconds)} seconds",
                    3);
                return;
            }

            var item = ev.Player.Items.FirstOrDefault(i => i.Type == ItemType.GrenadeHE);
            if (item is null)
            {
                ev.Player.SetGUI("grenadeLauncherWarn", PseudoGUIPosition.BOTTOM,
                    string.Format(PluginHandler.Instance.Translation.NoAmmoError,
                        PluginHandler.Instance.Translation.GrenadeLauncherAmmo), 3);
                return;
            }

            if (!cooldowns.ContainsKey(ev.Firearm.Serial))
                cooldowns.Add(ev.Firearm.Serial, DateTime.Now);
            cooldowns[ev.Firearm.Serial] = DateTime.Now.AddSeconds(5);
            RLogger.Log("GRENADE LAUNCHER", "RELOAD", $"Player {ev.Player.PlayerToString()} reloaded {Name}");
            grenadeQueue[ev.Firearm.Serial].Add(GetTypeFromGrenade(item));
            ev.Player.RemoveItem(item);
            ev.Player.SetGUI("grenadeLauncherWarn", PseudoGUIPosition.BOTTOM,
                PluginHandler.Instance.Translation.ReloadedInfo, 3);
            ev.Player.Connection.Send(new RequestMessage(ev.Firearm.Serial, RequestType.Reload));
        }

        /// <inheritdoc />
        protected override void OnUnloadingWeapon(UnloadingWeaponEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        /// <inheritdoc />
        protected override void OnShooting(ShootingEventArgs ev)
        {
            base.OnShooting(ev);
            ev.IsAllowed = false;
            isShotAllowed = true;
            var serial = ev.Shooter.CurrentItem.Serial;
            if (!grenadeQueue.ContainsKey(serial))
            {
                Log.Error("Somehow key not found");
                grenadeQueue.Add(serial, new List<CustomGrenadeTypes>());
            }

            if (grenadeQueue[serial].Count == 0)
            {
                ev.Shooter.SetGUI("grenadeLauncherWarn", PseudoGUIPosition.BOTTOM,
                    PluginHandler.Instance.Translation.EmptyMagazineError, 3);
                isShotAllowed = false;
                return;
            }

            var name = "GrenadeHE";
            var toThrow = GetGrenadeFromType(grenadeQueue[serial][0]);
            if (grenadeQueue[serial][0] == CustomGrenadeTypes.STICKY)
            {
                StickyGrenadeItem.Throw(ev.Shooter.ReferenceHub, toThrow);
                name = StickyGrenadeItem.Instance.Name;
            }
            else if (grenadeQueue[serial][0] == CustomGrenadeTypes.IMPACT)
            {
                ImpItem.Throw(ev.Shooter.ReferenceHub, toThrow);
                name = ImpItem.Instance.Name;
            }
            else
            {
                toThrow.Base.Owner = ev.Shooter.ReferenceHub;
                ServerThrowPatch.ThrowedItems.Add(toThrow.Base);
                toThrow.Base.ServerThrow(8.5f, 0.2f, new Vector3(10, 10, 0),
                    ev.Shooter.ReferenceHub.playerMovementSync.PlayerVelocity);
            }

            RLogger.Log("GRENADE LAUNCHER", "FIRE", $"Player {ev.Shooter.PlayerToString()} fired {name}");
            grenadeQueue[serial].RemoveAt(0);
            Hitmarker.SendHitmarker(ev.Shooter.Connection, 3f);
        }

        /// <inheritdoc />
        protected override void ShowPickedUpMessage(Player player)
        {
            RLogger.Log("GRENADE LAUNCHER", "PICKUP", $"{player.PlayerToString()} Picked up an {Name}");
            player.SetGUI("glpickedupmessage", PseudoGUIPosition.MIDDLE,
                string.Format(PluginHandler.Instance.Translation.ItemPickedUpMessage,
                    PluginHandler.Instance.Translation.GrenadeLauncher), 2f);
        }

        /// <inheritdoc />
        protected override void OnPlayingGunAudio(PlayingGunAudioEventArgs ev)
        {
            base.OnPlayingGunAudio(ev);
            ev.IsAllowed = isShotAllowed;
        }

        /// <inheritdoc />
        protected override void ShowSelectedMessage(Player player)
        {
            Module.RunSafeCoroutine(UpdateInterface(player), "GrenadeLauncherItem_UpdateInterface");
        }

        private IEnumerator<float> UpdateInterface(Player player)
        {
            yield return Timing.WaitForSeconds(0.1f);
            while (Check(player.CurrentItem))
            {
                var serial = player.CurrentItem.Serial;
                var type = grenadeQueue[serial].FirstOrDefault();
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

                player.SetGUI("grenade_launcher_ammo", PseudoGUIPosition.BOTTOM,
                    $"Current grenade type: {grenadeType}");
                yield return Timing.WaitForSeconds(1f);
            }

            player.SetGUI("grenade_launcher_ammo", PseudoGUIPosition.BOTTOM, null);
        }

        private List<CustomGrenadeTypes> AddRandomGrenades()
        {
            var tor = new List<CustomGrenadeTypes>();
            while (tor.Count != 4)
                tor.Add((CustomGrenadeTypes)Random.Range(1, 4));

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
            if (StickyGrenadeItem.Instance.Check(item))
                return CustomGrenadeTypes.STICKY;

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (ImpItem.Instance.Check(item))
                return CustomGrenadeTypes.IMPACT;
            return CustomGrenadeTypes.FRAG;
        }
    }
}