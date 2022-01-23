// -----------------------------------------------------------------------
// <copyright file="ImpItem.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Spawn;
using Exiled.Events.EventArgs;
using InventorySystem.Items.ThrowableProjectiles;
using Mirror;
using Mistaken.API.CustomItems;
using Mistaken.API.Extensions;
using Mistaken.API.GUI;
using Mistaken.RoundLogger;
using UnityEngine;

namespace Mistaken.NotEnoughItems.Items
{
    /// <summary>
    /// Grenade that explodes on impact.
    /// </summary>
    public class ImpItem : MistakenCustomGrenade
    {
        /// <summary>
        /// Throws Impact Grenade.
        /// </summary>
        /// <param name="player">Throwing player.</param>
        /// <param name="grenade">Grenade to be thrown.</param>
        /// <returns>Thrown projectile.</returns>
        public static ThrownProjectile Throw(Player player = null, Throwable grenade = null)
        {
            if (grenade is null)
                grenade = new Throwable(ItemType.GrenadeHE, player);
            Respawning.GameplayTickets.Singleton.HandleItemTickets(grenade.Base);
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
                grenade.Base.PropelBody(rb, new Vector3(10, 10, 0), Vector3.zero, 35, 0.18f);

            thrownProjectile.gameObject.AddComponent<Components.ImpComponent>();
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
        public override Pickup Spawn(Vector3 position)
        {
            ExplosiveGrenade grenade = new ExplosiveGrenade(this.Type);
            RLogger.Log("IMPACT GRENADE", "SPAWN", $"{this.Name} spawned");
            return this.Spawn(position, grenade);
        }

        /// <inheritdoc/>
        public override Pickup Spawn(Vector3 position, Item item)
        {
            var grenade = item as Throwable;
            if (grenade is null) Log.Debug("Throwable is null");
            grenade.Scale = Handlers.ImpHandler.Size;
            grenade.Base.PickupDropModel.Info.Serial = grenade.Serial;
            this.TrackedSerials.Add(grenade.Serial);
            return grenade.Spawn(position);
        }

        internal static ImpItem Instance { get; private set; }

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
                Patches.ServerThrowPatch.ThrowedItems.Add(ev.Item.Base);
                ev.Player.RemoveItem(ev.Item);
            }
        }

        /// <inheritdoc/>
        protected override void OnExploding(ExplodingGrenadeEventArgs ev)
        {
            base.OnExploding(ev);
            RLogger.Log("IMPACT GRENADE", "EXPLODED", $"Impact grenade exploded");
            foreach (var player in ev.TargetsToAffect)
                RLogger.Log("IMPACT GRENADE", "HURT", $"{player.PlayerToString()} was hurt by an {this.Name}");
        }

        /// <inheritdoc/>
        protected override void ShowSelectedMessage(Player player)
        {
        }
    }
}
