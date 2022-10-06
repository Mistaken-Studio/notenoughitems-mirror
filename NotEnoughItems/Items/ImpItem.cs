// -----------------------------------------------------------------------
// <copyright file="ImpItem.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Attributes;
using Exiled.API.Features.Items;
using Exiled.API.Features.Spawn;
using Exiled.Events.EventArgs;
using Footprinting;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.ThrowableProjectiles;
using MapGeneration.Distributors;
using Mirror;
using Mistaken.API.CustomItems;
using Mistaken.API.Extensions;
using Mistaken.API.GUI;
using Mistaken.NotEnoughItems.Components;
using Mistaken.NotEnoughItems.Patches;
using Mistaken.RoundLogger;
using Respawning;
using UnityEngine;

namespace Mistaken.NotEnoughItems.Items
{
    /// <inheritdoc/>
    [CustomItem(ItemType.GrenadeHE)]
    public sealed class ImpItem : MistakenCustomGrenade
    {
        /// <summary>
        /// Throws Impact Grenade.
        /// </summary>
        /// <param name="ownerHub">Throwing player's hub.</param>
        /// <param name="grenade">Grenade to be thrown.</param>
        /// <returns>Thrown projectile.</returns>
        public static ThrownProjectile Throw(ReferenceHub ownerHub, Throwable grenade = null)
        {
            ownerHub ??= Server.Host.ReferenceHub;
            grenade ??= (Throwable)Item.Create(ItemType.GrenadeHE);
            grenade.Base.Owner = ownerHub;
            GameplayTickets.Singleton.HandleItemTickets(grenade.Base);
            var thrownProjectile = Object.Instantiate(grenade.Base.Projectile, ownerHub.PlayerCameraReference.position, ownerHub.PlayerCameraReference.rotation);
            var pickupSyncInfo = new PickupSyncInfo
            {
                ItemId = grenade.Type,
                Locked = !grenade.Base._repickupable,
                Serial = grenade.Serial,
                Weight = 0.01f,
                Position = thrownProjectile.transform.position,
                Rotation = new LowPrecisionQuaternion(thrownProjectile.transform.rotation),
            };

            thrownProjectile.NetworkInfo = pickupSyncInfo;
            thrownProjectile.PreviousOwner = new Footprint(ownerHub);
            NetworkServer.Spawn(thrownProjectile.gameObject, ownerConnection: null);
            ExplodeDestructiblesPatch.Grenades.Add(thrownProjectile.netId);
            thrownProjectile.InfoReceived(default, pickupSyncInfo);
            if (thrownProjectile.TryGetComponent<Rigidbody>(out var rb))
                grenade.Base.PropelBody(rb, new Vector3(10, 10, 0), ownerHub.playerMovementSync.PlayerVelocity, 35, 0.18f);

            thrownProjectile.gameObject.AddComponent<ImpComponent>();
            thrownProjectile.ServerActivate();
            return thrownProjectile;
        }

        /// <inheritdoc/>
        public override MistakenCustomItems CustomItem => MistakenCustomItems.IMPACT_GRENADE;

        /// <inheritdoc/>
        public override ItemType Type { get; set; } = ItemType.GrenadeHE;

        /// <inheritdoc/>
        public override string Name { get; set; } = "Impact Grenade";

        /// <inheritdoc/>
        public override string Description { get; set; } = "Grenade that explodes on impact";

        /// <inheritdoc/>
        public override string DisplayName => "Impact Grenade";

        /// <inheritdoc/>
        public override float Weight { get; set; } = 0.01f;

        /// <inheritdoc/>
        public override SpawnProperties SpawnProperties { get; set; }

        /// <inheritdoc/>
        public override bool ExplodeOnCollision { get; set; }

        /// <inheritdoc/>
        public override float FuseTime { get; set; } = 3;

        /// <inheritdoc/>
        public override void Init()
        {
            base.Init();
            Instance = this;
        }

        /// <inheritdoc/>
        public override void Give(Player player, bool displayMessage)
        {
            RLogger.Log("IMPACT GRENADE", "GIVE", $"{this.Name} given to {player.PlayerToString()}");
            base.Give(player, displayMessage);
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
            RLogger.Log("IMPACT GRENADE", "SPAWN", $"{this.Name} spawned");
            var grenade = item.Base as ThrowableItem;
            pickup.Scale = Size;
            grenade.PickupDropModel.Info.Serial = pickup.Serial;
            this.TrackedSerials.Add(pickup.Serial);
            return pickup;
        }

        internal static ImpItem Instance { get; private set; }

        /// <inheritdoc/>
        protected override void SubscribeEvents()
        {
            base.SubscribeEvents();
            Exiled.Events.Handlers.Server.RoundStarted += this.Server_RoundStarted;
        }

        /// <inheritdoc/>
        protected override void UnsubscribeEvents()
        {
            base.UnsubscribeEvents();
            Exiled.Events.Handlers.Server.RoundStarted -= this.Server_RoundStarted;
        }

        /// <inheritdoc/>
        protected override void OnPickingUp(PickingUpItemEventArgs ev)
        {
            RLogger.Log("IMPACT GRENADE", "PICKING UP", $"{ev.Player.Nickname} started picking up {this.Name} ({this.Type})");
            base.OnPickingUp(ev);
        }

        /// <inheritdoc/>
        protected override void OnAcquired(Player player)
        {
            RLogger.Log("IMPACT GRENADE", "ACQUIRED", $"{player.Nickname} acquired {this.Name} ({this.Type})");
            base.OnAcquired(player);
        }

        /// <inheritdoc/>
        protected override void ShowPickedUpMessage(Player player)
        {
            RLogger.Log("IMPACT GRENADE", "PICKUP", $"{player.PlayerToString()} Picked up an {this.Name}");
            player.SetGUI("imppickedupmessage", PseudoGUIPosition.MIDDLE, string.Format(PluginHandler.Instance.Translation.ItemPickedUpMessage, PluginHandler.Instance.Translation.ImpactGrenade), 2f);
        }

        /// <inheritdoc/>
        protected override void OnThrowing(ThrowingItemEventArgs ev)
        {
            base.OnThrowing(ev);
            if (ev.RequestType != ThrowRequest.BeginThrow)
            {
                RLogger.Log("IMPACT GRENADE", "THROW", $"{ev.Player.PlayerToString()} threw an {this.Name}");
                ServerThrowPatch.ThrowedItems.Add(ev.Item.Base);
                ev.Player.RemoveItem(ev.Item);
            }
        }

        /// <inheritdoc/>
        protected override void OnExploding(ExplodingGrenadeEventArgs ev)
        {
            base.OnExploding(ev);
            RLogger.Log("IMPACT GRENADE", "EXPLODED", "Impact grenade exploded");
            foreach (var player in ev.TargetsToAffect)
                RLogger.Log("IMPACT GRENADE", "HURT", $"{player.PlayerToString()} was hurt by an {this.Name}");
        }

        /// <inheritdoc/>
        protected override void OnWaitingForPlayers()
        {
            base.OnWaitingForPlayers();
            ExplodeDestructiblesPatch.Grenades.Clear();
            ServerThrowPatch.ThrowedItems.Clear();
        }

        /// <inheritdoc/>
        protected override void ShowSelectedMessage(Player player)
        {
        }

        private static readonly Vector3 Size = new (1f, 0.4f, 1f);

        private void Server_RoundStarted()
        {
            var structureLockers = Object.FindObjectsOfType<SpawnableStructure>().Where(x => x.StructureType == StructureType.LargeGunLocker);
            var lockers = structureLockers.Select(x => x as Locker).Where(x => x.Chambers.Length > 8).ToArray();
            var locker = lockers[Random.Range(0, lockers.Length)];
            var toSpawn = 6;
            while (toSpawn > 0)
            {
                var chamber = locker.Chambers[Random.Range(0, locker.Chambers.Length)];
                var pickup = Instance.Spawn(chamber._spawnpoint.position + (Vector3.up / 10), previousOwner: null);
                chamber._content.Add(pickup.Base);
                toSpawn--;
            }
        }
    }
}