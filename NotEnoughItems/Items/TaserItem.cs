// -----------------------------------------------------------------------
// <copyright file="TaserItem.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Spawn;
using Exiled.CustomItems.API.Features;
using InventorySystem.Items.Firearms;
using MEC;
using Mistaken.API;
using Mistaken.API.CustomItems;
using Mistaken.API.Extensions;
using Mistaken.API.GUI;
using Mistaken.RoundLogger;
using UnityEngine;

namespace Mistaken.NotEnoughItems.Items
{
    /// <summary>
    /// Com18 that applies some effects on target.
    /// </summary>
    public class TaserItem : MistakenCustomWeapon
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
        public override Modifiers Modifiers { get; set; } = new Modifiers(0, 0, 0);

        /// <inheritdoc/>
        public override byte ClipSize { get; set; } = 1;

        /// <inheritdoc/>
        public override float Damage { get; set; } = 5;

        /// <inheritdoc/>
        public override void Give(Player player, bool displayMessage = true)
        {
            Exiled.API.Features.Items.Firearm firearm = new Exiled.API.Features.Items.Firearm(this.Type);
            firearm.Base.Status = new FirearmStatus(this.ClipSize, FirearmStatusFlags.Cocked, 75);
            player.AddItem(firearm);
            RLogger.Log("TASER", "GIVE", $"{this.Name} given to {player.PlayerToString()}");

            this.TrackedSerials.Add(firearm.Serial);
            if (displayMessage)
                this.ShowPickedUpMessage(player);
        }

        /// <inheritdoc/>
        public override void Give(Player player, Pickup pickup, bool displayMessage = true)
        {
            FirearmPickup firearm = (FirearmPickup)pickup.Base;
            player.AddItem(pickup);
            firearm.Status = new FirearmStatus(firearm.Status.Ammo, firearm.Status.Flags, 75);
            RLogger.Log("TASER", "GIVE", $"Given {this.Name} to {player.PlayerToString()}");

            this.TrackedSerials.Add(firearm.Info.Serial);
            if (displayMessage)
                this.ShowPickedUpMessage(player);
        }

        /// <inheritdoc/>
        public override Pickup Spawn(Vector3 position)
        {
            Exiled.API.Features.Items.Firearm firearm = new Exiled.API.Features.Items.Firearm(this.Type);
            RLogger.Log("TASER", "SPAWN", $"Taser spawned");
            return this.Spawn(position, firearm);
        }

        /// <inheritdoc/>
        public override Pickup Spawn(Vector3 position, Item item)
        {
            var firearm = item as Exiled.API.Features.Items.Firearm;
            firearm.Base.PickupDropModel.Info.Serial = firearm.Serial;
            firearm.Scale = Size;
            if (this.cooldowns.TryGetValue(item.Serial, out DateTime value))
            {
                this.cooldowns.Remove(item.Serial);
                this.cooldowns.Add(firearm.Serial, value);
            }

            this.TrackedSerials.Add(firearm.Serial);
            var pickup = firearm.Spawn(position);
            ((FirearmPickup)pickup.Base).Status = new FirearmStatus(this.ClipSize, FirearmStatusFlags.Cocked, 75);
            return pickup;
        }

        internal static readonly Vector3 Size = new Vector3(1f, 0.65f, 1f);

        /// <inheritdoc/>
        protected override void ShowSelectedMessage(Player player)
        {
            Handlers.TaserHandler.Instance.RunCoroutine(this.UpdateInterface(player), "Taser.UpdateInterface");
        }

        /// <inheritdoc/>
        protected override void OnReloading(Exiled.Events.EventArgs.ReloadingWeaponEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        /// <inheritdoc/>
        protected override void OnUnloadingFirearm(Events.EventArgs.UnloadingWeaponEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        /// <inheritdoc/>
        protected override void ShowPickedUpMessage(Player player)
        {
            RLogger.Log("TASER", "PICKUP", $"{player.PlayerToString()} Picked up an {this.Name}");
            player.SetGUI("taserpickedupmessage", PseudoGUIPosition.MIDDLE, string.Format(PluginHandler.Instance.Translation.ItemPickedUpMessage, PluginHandler.Instance.Translation.Taser), 2f);
        }

        /// <inheritdoc/>
        protected override void OnShooting(Exiled.Events.EventArgs.ShootingEventArgs ev)
        {
            if (!this.cooldowns.TryGetValue(ev.Shooter.CurrentItem.Serial, out DateTime time))
                this.cooldowns.Add(ev.Shooter.CurrentItem.Serial, DateTime.Now);
            if (DateTime.Now < time)
            {
                ev.Shooter.SetGUI("taserammo", PseudoGUIPosition.TOP, PluginHandler.Instance.Translation.TaserNoAmmo, 2);
                ev.IsAllowed = false;
                return;
            }
            else
            {
                (ev.Shooter.CurrentItem as Exiled.API.Features.Items.Firearm).Ammo += 1;
                this.cooldowns[ev.Shooter.CurrentItem.Serial] = DateTime.Now.AddSeconds(PluginHandler.Instance.Config.HitCooldown);
                Player targetPlayer = (RealPlayers.List.Where(x => x.NetworkIdentity.netId == ev.TargetNetId).Count() > 0) ? RealPlayers.List.First(x => x.NetworkIdentity.netId == ev.TargetNetId) : null;
                if (targetPlayer != null)
                {
                    Hitmarker.SendHitmarker(ev.Shooter.Connection, 20);
                    if (targetPlayer.Items.Select(x => x.Type).Any(x => x == ItemType.ArmorLight || x == ItemType.ArmorCombat || x == ItemType.ArmorHeavy))
                    {
                        RLogger.Log("TASER", "BLOCKED", $"{ev.Shooter.PlayerToString()} hit {targetPlayer.PlayerToString()} but effects were blocked by an armor");
                        return;
                    }

                    if (targetPlayer.GetSessionVar<bool>(SessionVarType.SPAWN_PROTECT))
                    {
                        RLogger.Log("TASER", "REVERSED", $"{ev.Shooter.PlayerToString()} hit {targetPlayer.PlayerToString()} but effects were reversed because of spawn protect");
                        targetPlayer = ev.Shooter;
                        return;
                    }

                    if (targetPlayer.IsHuman)
                    {
                        targetPlayer.EnableEffect<CustomPlayerEffects.Ensnared>(2);
                        targetPlayer.EnableEffect<CustomPlayerEffects.Flashed>(5);
                        targetPlayer.EnableEffect<CustomPlayerEffects.Deafened>(10);
                        targetPlayer.EnableEffect<CustomPlayerEffects.Blinded>(10);
                        targetPlayer.EnableEffect<CustomPlayerEffects.Amnesia>(5);
                        if (targetPlayer.CurrentItem != null && !Handlers.TaserHandler.UsableItems.Contains(targetPlayer.CurrentItem.Type))
                        {
                            Exiled.Events.Handlers.Player.OnDroppingItem(new Exiled.Events.EventArgs.DroppingItemEventArgs(targetPlayer, targetPlayer.CurrentItem.Base, false));
                            var pickup = MapPlus.Spawn(targetPlayer.CurrentItem.Type, targetPlayer.Position, Quaternion.identity, Vector3.one);
                            pickup.ItemSerial = targetPlayer.CurrentItem.Serial;

                            targetPlayer.DropItem(targetPlayer.CurrentItem);
                            targetPlayer.RemoveItem(targetPlayer.CurrentItem);
                            targetPlayer.CurrentItem = default;
                        }

                        RLogger.Log("TASER", "HIT", $"{ev.Shooter.PlayerToString()} hit {targetPlayer.PlayerToString()}");
                        targetPlayer.Broadcast($"<color=yellow>{PluginHandler.Instance.Translation.Taser}</color>", 10, string.Format(PluginHandler.Instance.Translation.TaserPlayerTased, ev.Shooter.Nickname, ev.Shooter.Role));
                        targetPlayer.SendConsoleMessage(string.Format(PluginHandler.Instance.Translation.TaserPlayerTased, ev.Shooter.Nickname, ev.Shooter.Role), "yellow");
                        return;
                    }
                }
                else
                {
                    UnityEngine.Physics.Raycast(ev.Shooter.Position, ev.Shooter.CameraTransform.forward, out RaycastHit hitinfo);
                    if (hitinfo.collider != null)
                    {
                        if (!Handlers.TaserHandler.Doors.TryGetValue(hitinfo.collider.gameObject, out var door) || door == null)
                        {
                            RLogger.Log("TASER", "HIT", $"{ev.Shooter.PlayerToString()} didn't hit anyone");
                            this.cooldowns[ev.Shooter.CurrentItem.Serial] = DateTime.Now.AddSeconds(PluginHandler.Instance.Config.MissCooldown);
                            return;
                        }

                        Hitmarker.SendHitmarker(ev.Shooter.Connection, 20);
                        door.ChangeLock(DoorLockType.NoPower);
                        RLogger.Log("TASER", "HIT", $"{ev.Shooter.PlayerToString()} hit door");
                        Handlers.TaserHandler.Instance.CallDelayed(10, () => door.ChangeLock(DoorLockType.NoPower), "UnlockDoors");
                        return;
                    }
                }
            }
        }

        private readonly Dictionary<ushort, DateTime> cooldowns = new Dictionary<ushort, DateTime>();

        private IEnumerator<float> UpdateInterface(Player player)
        {
            yield return Timing.WaitForSeconds(0.1f);
            while (this.Check(player.CurrentItem))
            {
                if (!this.cooldowns.TryGetValue(player.CurrentItem.Serial, out DateTime time))
                {
                    this.cooldowns.Add(player.CurrentItem.Serial, DateTime.Now);
                    time = DateTime.Now;
                }

                var diff = ((PluginHandler.Instance.Config.HitCooldown - (time - DateTime.Now).TotalSeconds) / PluginHandler.Instance.Config.HitCooldown) * 100;
                string bar = string.Empty;
                for (int i = 1; i <= 20; i++)
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
}
