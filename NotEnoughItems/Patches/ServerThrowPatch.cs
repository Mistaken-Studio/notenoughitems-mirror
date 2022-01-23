﻿// -----------------------------------------------------------------------
// <copyright file="ServerThrowPatch.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using HarmonyLib;
using InventorySystem.Items.ThrowableProjectiles;
using Mirror;
using UnityEngine;

namespace Mistaken.NotEnoughItems.Patches
{
    /// <summary>
    /// Patch for adding ImpComponent to thrown grenades.
    /// </summary>
    [HarmonyPatch(typeof(ThrowableItem), "ServerThrow", new Type[] { typeof(float), typeof(float), typeof(Vector3), typeof(Vector3) })]
    public static class ServerThrowPatch
    {
        /// <summary>
        /// Gets or sets players who threw an impact grenade.
        /// </summary>
        public static HashSet<ThrowableItem> ThrowedItems { get; set; } = new HashSet<ThrowableItem>();

        /// <summary>
        /// Patch for adding ImpComponent to thrown grenades.
        /// </summary>
        /// <param name="__instance">Instance.</param>
        /// <param name="forceAmount">Force Amount.</param>
        /// <param name="upwardFactor">UpwardFactor.</param>
        /// <param name="torque">Torque.</param>
        /// <returns>whether basegame code should get executed.</returns>
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
        public static bool Prefix(ThrowableItem __instance, float forceAmount, float upwardFactor, Vector3 torque, Vector3 startVel)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
        {
            if (!ThrowedItems.Contains(__instance))
                return true;
            Respawning.GameplayTickets.Singleton.HandleItemTickets(__instance);

            ThrownProjectile thrownProjectile = UnityEngine.Object.Instantiate<ThrownProjectile>(__instance.Projectile, __instance.Owner.PlayerCameraReference.position, __instance.Owner.PlayerCameraReference.rotation);
            InventorySystem.Items.Pickups.PickupSyncInfo pickupSyncInfo = new InventorySystem.Items.Pickups.PickupSyncInfo
            {
                ItemId = __instance.ItemTypeId,
                Locked = !__instance._repickupable,
                Serial = __instance.ItemSerial,
                Weight = __instance.Weight,
                Position = thrownProjectile.transform.position,
                Rotation = new LowPrecisionQuaternion(thrownProjectile.transform.rotation),
            };

            thrownProjectile.NetworkInfo = pickupSyncInfo;
            thrownProjectile.PreviousOwner = new Footprinting.Footprint(__instance.Owner);
            NetworkServer.Spawn(thrownProjectile.gameObject, ownerConnection: null);
            ExplodeDestructiblesPatch.Grenades.Add(thrownProjectile);
            thrownProjectile.InfoReceived(default(InventorySystem.Items.Pickups.PickupSyncInfo), pickupSyncInfo);
            if (thrownProjectile.TryGetComponent<Rigidbody>(out var rb))
                __instance.PropelBody(rb, torque, startVel, forceAmount * 2f, upwardFactor / 1.1f);

            if (Items.ImpItem.Instance.TrackedSerials.Contains(__instance.ItemSerial))
                thrownProjectile.gameObject.AddComponent<Components.ImpComponent>();
            if (Items.StickyGrenadeItem.Instance.TrackedSerials.Contains(__instance.ItemSerial))
                thrownProjectile.gameObject.AddComponent<Components.StickyComponent>();

            thrownProjectile.ServerActivate();

            return false;
        }
    }
}
