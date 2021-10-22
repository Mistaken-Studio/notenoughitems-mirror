// -----------------------------------------------------------------------
// <copyright file="StickyGrenadeItem.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Spawn;
using Exiled.Events.EventArgs;
using InventorySystem.Items.ThrowableProjectiles;
using MEC;
using Mirror;
using Mistaken.API.CustomItems;
using Mistaken.API.Extensions;
using Mistaken.API.GUI;
using Mistaken.RoundLogger;
using UnityEngine;

namespace Mistaken.NotEnoughItems.Items
{
    /// <inheritdoc/>
    public class StickyGrenadeItem : MistakenCustomGrenade
    {
        /// <summary>
        /// Throws Sticky Grenade.
        /// </summary>
        /// <param name="player">Throwing Player.</param>
        /// <returns>Thrown projectile.</returns>
        public static ThrownProjectile Throw(Player player = null)
        {
            var grenade = new Throwable(ItemType.GrenadeHE, player);
            if (grenade.Base.Owner.characterClassManager.CurRole.team == Team.CHI || grenade.Base.Owner.characterClassManager.CurClass == RoleType.ClassD)
                Respawning.GameplayTickets.Singleton.HandleItemTickets(grenade.Base.OwnerInventory.CurItem);

            ThrownProjectile thrownProjectile = UnityEngine.Object.Instantiate<ThrownProjectile>(grenade.Base.Projectile, grenade.Base.Owner.PlayerCameraReference.position, grenade.Base.Owner.PlayerCameraReference.rotation);
            InventorySystem.Items.Pickups.PickupSyncInfo pickupSyncInfo = new InventorySystem.Items.Pickups.PickupSyncInfo
            {
                ItemId = grenade.Type,
                Locked = !grenade.Base._repickupable,
                Serial = grenade.Serial,
                Weight = 0.01f,
                Position = thrownProjectile.transform.position,
                Rotation = new LowPrecisionQuaternion(thrownProjectile.transform.rotation),
            };

            thrownProjectile.NetworkInfo = pickupSyncInfo;
            thrownProjectile.PreviousOwner = new Footprinting.Footprint(grenade.Base.Owner);
            NetworkServer.Spawn(thrownProjectile.gameObject, ownerConnection: null);
            Patches.ExplodeDestructiblesPatch.Grenades.Add(thrownProjectile);
            thrownProjectile.InfoReceived(default(InventorySystem.Items.Pickups.PickupSyncInfo), pickupSyncInfo);
            Rigidbody rb;
            if (thrownProjectile.TryGetComponent<Rigidbody>(out rb))
                grenade.Base.PropelBody(rb, new Vector3(10, 10, 0), 50, 0.2f);

            thrownProjectile.gameObject.AddComponent<Components.StickyComponent>();
            thrownProjectile.ServerActivate();
            return thrownProjectile;
        }

        /// <inheritdoc/>
        public override MistakenCustomItems CustomItem => MistakenCustomItems.STICKY_GRENADE;

        /// <inheritdoc/>
        public override ItemType Type { get; set; } = ItemType.GrenadeHE;

        /// <inheritdoc/>
        public override string Name { get; set; } = "Sticky Grenade";

        /// <inheritdoc/>
        public override string Description { get; set; } = "A Sticky Grenade";

        /// <inheritdoc/>
        public override float Weight { get; set; } = 0.01f;

        /// <inheritdoc/>
        public override SpawnProperties SpawnProperties { get; set; }

        /// <inheritdoc/>
        public override bool ExplodeOnCollision { get; set; } = false;

        /// <inheritdoc/>
        public override float FuseTime { get; set; } = 3f;

        /// <inheritdoc/>
        public override void Give(Player player, bool displayMessage = true)
        {
            RLogger.Log("STICKY GRENADE", "GIVE", $"{this.Name} given to {player.PlayerToString()}");
            base.Give(player, displayMessage);
        }

        /// <inheritdoc/>
        public override Pickup Spawn(Vector3 position)
        {
            ExplosiveGrenade grenade = new ExplosiveGrenade(this.Type);
            RLogger.Log("STICKY GRENADE", "SPAWN", $"{this.Name} spawned");
            return this.Spawn(position, grenade);
        }

        /// <inheritdoc/>
        public override Pickup Spawn(Vector3 position, Item item)
        {
            var grenade = item as Throwable;
            if (grenade is null) Log.Debug("Throwable is null");
            grenade.Base.PickupDropModel.Info.Serial = grenade.Serial;
            this.TrackedSerials.Add(grenade.Serial);
            return grenade.Spawn(position);
        }

        /// <inheritdoc/>
        protected override void OnThrowing(ThrowingItemEventArgs ev)
        {
            if (ev.RequestType != ThrowRequest.BeginThrow)
            {
                RLogger.Log("STICKY GRENADE", "THROW", $"Player {ev.Player.PlayerToString()} threw a {this.Name}");
                Patches.ServerThrowPatch.ThrowedItems.Add(ev.Item.Base);
                ev.Player.RemoveItem(ev.Item);
            }
        }

        /// <inheritdoc/>
        protected override void ShowPickedUpMessage(Player player)
        {
            RLogger.Log("STICKY GRENADE", "PICKUP", $"{player.PlayerToString()} Picked up an {this.Name}");
            player.SetGUI("stickygrenadepickedupmessage", PseudoGUIPosition.MIDDLE, string.Format(PluginHandler.Instance.Translation.ItemPickedUpMessage, PluginHandler.Instance.Translation.StickyGrenade), 2f);
        }

        /// <inheritdoc/>
        protected override void ShowSelectedMessage(Player player)
        {
            Handlers.StickyGrenadeHandler.Instance.RunCoroutine(this.UpdateInterface(player));
        }

        private IEnumerator<float> UpdateInterface(Player player)
        {
            yield return Timing.WaitForSeconds(0.1f);
            while (this.Check(player.CurrentItem))
            {
                player.SetGUI("stickyhold", PseudoGUIPosition.BOTTOM, string.Format(PluginHandler.Instance.Translation.ItemHoldingMessage, PluginHandler.Instance.Translation.StickyGrenade));
                yield return Timing.WaitForSeconds(1f);
            }

            player.SetGUI("stickyhold", PseudoGUIPosition.BOTTOM, null);
        }
    }
}
