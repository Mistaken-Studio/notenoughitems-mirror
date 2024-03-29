// -----------------------------------------------------------------------
// <copyright file="TaserItem.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

/*using System;
using System.Collections.Generic;
using System.Linq;
using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Attributes;
using Exiled.API.Features.Items;
using Exiled.API.Features.Spawn;
using Exiled.Events.EventArgs;
using InventorySystem.Items.Firearms;
using MapGeneration.Distributors;
using MEC;
using Mistaken.API;
using Mistaken.API.CustomItems;
using Mistaken.API.Diagnostics;
using Mistaken.API.Extensions;
using Mistaken.API.GUI;
using Mistaken.RoundLogger;
using UnityEngine;
using StructureType = MapGeneration.Distributors.StructureType;

namespace Mistaken.NotEnoughItems.Items
{
    /// <inheritdoc/>
    [CustomItem(ItemType.GunCOM15)]
    public sealed class TaserItem : MistakenCustomWeapon
    {
        /// <inheritdoc/>
        public override MistakenCustomItems CustomItem => MistakenCustomItems.TASER;

        /// <inheritdoc/>
        public override bool AllowChangingAttachments => false;

        /// <inheritdoc/>
        public override ItemType Type { get; set; } = ItemType.GunCOM15;

        /// <inheritdoc/>
        public override string Name { get; set; } = "Taser";

        /// <inheritdoc/>
        public override string Description { get; set; } = "Taser";

        /// <inheritdoc/>
        public override float Weight { get; set; } = 0.1f;

        /// <inheritdoc/>
        public override SpawnProperties SpawnProperties { get; set; }

        /// <inheritdoc/>
        public override byte ClipSize { get; set; } = 1;

        /// <inheritdoc/>
        public override float Damage { get; set; } = 5;

        /// <inheritdoc/>
        public override void Init()
        {
            base.Init();
            Instance = this;
        }

        /// <inheritdoc/>
        public override void Give(Player player, bool displayMessage = true)
        {
            var pickup = this.CreateCorrectItem().Spawn(Vector3.zero);
            var firearm = (FirearmPickup)pickup.Base;
            firearm.NetworkStatus = new FirearmStatus(this.ClipSize, FirearmStatusFlags.Cocked, 75);
            player.AddItem(pickup);
            RLogger.Log("TASER", "GIVE", $"{this.Name} given to {player.PlayerToString()}");
            this.TrackedSerials.Add(pickup.Serial);
            if (displayMessage)
                this.ShowPickedUpMessage(player);
        }

        /// <inheritdoc/>
        public override void Give(Player player, Pickup pickup, bool displayMessage = true)
        {
            var firearm = (FirearmPickup)pickup.Base;
            firearm.NetworkStatus = new FirearmStatus(this.ClipSize, FirearmStatusFlags.Cocked, 75);
            player.AddItem(pickup);
            RLogger.Log("TASER", "GIVE", $"Given {this.Name} to {player.PlayerToString()}");
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
            RLogger.Log("TASER", "SPAWN", "Taser spawned");
            pickup.Scale = Size;

            if (Cooldowns.TryGetValue(pickup.Serial, out var value))
            {
                Cooldowns.Remove(pickup.Serial);
                Cooldowns.Add(pickup.Serial, value);
            }

            this.TrackedSerials.Add(pickup.Serial);
            ((FirearmPickup)pickup.Base).Status = new FirearmStatus(this.ClipSize, FirearmStatusFlags.Cocked, 75);
            return pickup;
        }

        internal static TaserItem Instance { get; private set; }

        /// <inheritdoc/>
        protected override void SubscribeEvents()
        {
            base.SubscribeEvents();
            Exiled.Events.Handlers.Server.RoundStarted += this.Server_RoundStarted;
            Exiled.Events.Handlers.Player.ChangingRole += this.Player_ChangingRole;
        }

        /// <inheritdoc/>
        protected override void UnsubscribeEvents()
        {
            base.UnsubscribeEvents();
            Exiled.Events.Handlers.Server.RoundStarted -= this.Server_RoundStarted;
            Exiled.Events.Handlers.Player.ChangingRole -= this.Player_ChangingRole;
        }

        /// <inheritdoc/>
        protected override void ShowSelectedMessage(Player player)
        {
            Module.RunSafeCoroutine(this.UpdateInterface(player), nameof(this.UpdateInterface));
        }

        /// <inheritdoc/>
        protected override void OnReloading(ReloadingWeaponEventArgs ev)
        {
            base.OnReloading(ev);
            ev.IsAllowed = false;
        }

        /// <inheritdoc/>
        protected override void OnUnloadingWeapon(UnloadingWeaponEventArgs ev)
        {
            base.OnUnloadingWeapon(ev);
            ev.IsAllowed = false;
        }

        /// <inheritdoc/>
        protected override void ShowPickedUpMessage(Player player)
        {
            RLogger.Log("TASER", "PICKUP", $"{player.PlayerToString()} Picked up an {this.Name}");
            player.SetGUI("taserpickedupmessage", PseudoGUIPosition.MIDDLE, string.Format(PluginHandler.Instance.Translation.ItemPickedUpMessage, PluginHandler.Instance.Translation.Taser), 2f);
        }

        /// <inheritdoc/>
        protected override void OnShooting(ShootingEventArgs ev)
        {
            base.OnShooting(ev);
            this.isShotAllowed = true;
            if (!Cooldowns.TryGetValue(ev.Shooter.CurrentItem.Serial, out var time))
                Cooldowns.Add(ev.Shooter.CurrentItem.Serial, DateTime.Now);

            if (DateTime.Now < time)
            {
                this.isShotAllowed = false;
                ev.Shooter.SetGUI("taserammo", PseudoGUIPosition.TOP, PluginHandler.Instance.Translation.TaserNoAmmo, 2);
                ev.IsAllowed = false;
                return;
            }

            (ev.Shooter.CurrentItem as Exiled.API.Features.Items.Firearm).Ammo += 1;
            Cooldowns[ev.Shooter.CurrentItem.Serial] = DateTime.Now.AddSeconds(PluginHandler.Instance.Config.HitCooldown);
            var targetPlayer = RealPlayers.List.FirstOrDefault(x => x.NetworkIdentity.netId == ev.TargetNetId);
            if (targetPlayer is not null)
            {
                Hitmarker.SendHitmarker(ev.Shooter.Connection, 20);
                if (targetPlayer.Items.Select(x => x.Type).Any(x => x == ItemType.ArmorLight || x == ItemType.ArmorCombat || x == ItemType.ArmorHeavy))
                {
                    RLogger.Log("TASER", "BLOCKED", $"{ev.Shooter.PlayerToString()} hit {targetPlayer.PlayerToString()} but effects were blocked by an armor");
                    return;
                }

                if (targetPlayer.GetSessionVariable<bool>(SessionVarType.SPAWN_PROTECT))
                {
                    RLogger.Log("TASER", "REVERSED", $"{ev.Shooter.PlayerToString()} hit {targetPlayer.PlayerToString()} but effects were reversed because of spawn protect");
                    targetPlayer = ev.Shooter;
                    return;
                }

                if (targetPlayer.IsHuman)
                {
                    targetPlayer.EnableEffect<Ensnared>(2);
                    targetPlayer.EnableEffect<Flashed>(5);
                    targetPlayer.EnableEffect<Deafened>(10);
                    targetPlayer.EnableEffect<Blinded>(10);
                    if (targetPlayer.CurrentItem is not null && !UsableItems.Contains(targetPlayer.CurrentItem.Type))
                        targetPlayer.DropHeldItem();

                    RLogger.Log("TASER", "HIT", $"{ev.Shooter.PlayerToString()} hit {targetPlayer.PlayerToString()}");
                    targetPlayer.Broadcast($"<color=yellow>{PluginHandler.Instance.Translation.Taser}</color>", 10, string.Format(PluginHandler.Instance.Translation.TaserPlayerTased, ev.Shooter.Nickname, ev.Shooter.Role.Type.ToString()));
                    targetPlayer.SendConsoleMessage(string.Format(PluginHandler.Instance.Translation.TaserPlayerTased, ev.Shooter.Nickname, ev.Shooter.Role.Type.ToString()), "yellow");
                }
            }
            else
            {
                Physics.Raycast(ev.Shooter.Position, ev.Shooter.CameraTransform.forward, out var hitinfo);
                if (hitinfo.collider != null)
                {
                    Log.Debug($"TaserItem Debug: {hitinfo.collider.name}", PluginHandler.Instance.Config.VerbouseOutput);
                    if (!Doors.TryGetValue(hitinfo.collider.gameObject, out var door) || door == null)
                    {
                        RLogger.Log("TASER", "HIT", $"{ev.Shooter.PlayerToString()} didn't hit anyone");
                        Cooldowns[ev.Shooter.CurrentItem.Serial] = DateTime.Now.AddSeconds(PluginHandler.Instance.Config.MissCooldown);
                        return;
                    }

                    Hitmarker.SendHitmarker(ev.Shooter.Connection, 20);
                    door.ChangeLock(DoorLockType.NoPower);
                    RLogger.Log("TASER", "HIT", $"{ev.Shooter.PlayerToString()} hit door");
                    void UnlockDoor()
                    {
                        if (door.DoorLockType.HasFlag(DoorLockType.NoPower))
                            door.ChangeLock(DoorLockType.NoPower);
                    }

                    Module.CallSafeDelayed(10, UnlockDoor, nameof(UnlockDoor), true);
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnPlayingGunAudio(PlayingGunAudioEventArgs ev)
        {
            base.OnPlayingGunAudio(ev);
            ev.IsAllowed = this.isShotAllowed;
        }

        /// <inheritdoc/>
        protected override void OnWaitingForPlayers()
        {
            base.OnWaitingForPlayers();
            Cooldowns.Clear();
            Doors.Clear();

            foreach (var door in Door.List)
            {
                foreach (var child in door.Base.GetComponentsInChildren<BoxCollider>())
                    Doors[child.gameObject] = door;
            }
        }

        private static readonly Dictionary<GameObject, Door> Doors = new ();

        private static readonly HashSet<ItemType> UsableItems = new ()
        {
            ItemType.MicroHID,
            ItemType.Medkit,
            ItemType.Painkillers,
            ItemType.SCP018,
            ItemType.SCP207,
            ItemType.SCP268,
            ItemType.SCP500,
            ItemType.GrenadeHE,
            ItemType.GrenadeFlash,
            ItemType.Adrenaline,
        };

        private static readonly Vector3 Size = new (1f, 0.65f, 1f);

        private static readonly Dictionary<ushort, DateTime> Cooldowns = new ();

        private bool isShotAllowed = true;

        private void Server_RoundStarted()
        {
            var structureLockers = UnityEngine.Object.FindObjectsOfType<SpawnableStructure>().Where(x => x.StructureType == StructureType.LargeGunLocker);
            var lockers = structureLockers.Select(x => x as Locker).Where(x => x.Chambers.Length > 8).ToArray();
            var locker = lockers[UnityEngine.Random.Range(0, lockers.Length)];
            var toSpawn = 1;
            while (toSpawn > 0)
            {
                var chamber = locker.Chambers[UnityEngine.Random.Range(0, locker.Chambers.Length)];
                var pickup = Instance.Spawn(chamber._spawnpoint.position + (Vector3.up / 10), previousOwner: null);
                chamber._content.Add(pickup.Base);
                toSpawn--;
            }
        }

        private void Player_ChangingRole(ChangingRoleEventArgs ev)
        {
            if (ev.Player.GetSessionVariable<bool>(SessionVarType.ITEM_LESS_CLSSS_CHANGE))
                return;

            if (ev.NewRole != RoleType.FacilityGuard)
                return;

            void AddTaser()
            {
                ev.Player.RemoveItem(ev.Player.Items.FirstOrDefault(x => x.Type == ItemType.GunCOM18));
                if (ev.Player.Items.Count >= 8)
                    Instance.Spawn(ev.Player.Position, ev.Player);
                else
                    Instance.Give(ev.Player);
            }

            Module.CallSafeDelayed(0.25f, AddTaser, nameof(AddTaser));
        }

        private IEnumerator<float> UpdateInterface(Player player)
        {
            yield return Timing.WaitForSeconds(0.1f);
            while (this.Check(player.CurrentItem))
            {
                if (!Cooldowns.TryGetValue(player.CurrentItem.Serial, out var time))
                {
                    Cooldowns.Add(player.CurrentItem.Serial, DateTime.Now);
                    time = DateTime.Now;
                }

                var diff = (PluginHandler.Instance.Config.HitCooldown - (time - DateTime.Now).TotalSeconds) / PluginHandler.Instance.Config.HitCooldown * 100;
                var bar = string.Empty;
                for (var i = 1; i <= 20; i++)
                {
                    if (i * (100 / 20) > diff)
                        bar += "<color=red>|</color>";
                    else
                        bar += "|";
                }

                player.SetGUI("taserholding", PseudoGUIPosition.BOTTOM, string.Format(PluginHandler.Instance.Translation.TaserHold, bar));
                yield return Timing.WaitForSeconds(1f);
            }

            player.SetGUI("taserholding", PseudoGUIPosition.BOTTOM, null);
        }
    }
}*/