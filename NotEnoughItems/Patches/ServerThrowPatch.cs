// -----------------------------------------------------------------------
// <copyright file="ServerThrowPatch.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using Footprinting;
using HarmonyLib;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.ThrowableProjectiles;
using Mirror;
using Mistaken.NotEnoughItems.Components;
using Mistaken.NotEnoughItems.Items;
using Respawning;
using UnityEngine;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

namespace Mistaken.NotEnoughItems.Patches
{
    [HarmonyPatch(typeof(ThrowableItem), nameof(ThrowableItem.ServerThrow), typeof(float), typeof(float),
        typeof(Vector3), typeof(Vector3))]
    internal static class ServerThrowPatch
    {
        public static HashSet<ThrowableItem> ThrowedItems { get; set; } = new();

        public static bool Prefix(ThrowableItem __instance, float forceAmount, float upwardFactor, Vector3 torque,
            Vector3 startVel)
        {
            if (!ThrowedItems.Contains(__instance)) return true;

            if (!__instance._alreadyFired || __instance.IsLocalPlayer)
            {
                __instance._destroyTime = Time.timeSinceLevelLoad + __instance._postThrownAnimationTime;
                __instance._alreadyFired = true;
                GameplayTickets.Singleton.HandleItemTickets(__instance);
                var thrownProjectile = Object.Instantiate(__instance.Projectile,
                    __instance.Owner.PlayerCameraReference.position, __instance.Owner.PlayerCameraReference.rotation);
                var pickupSyncInfo = default(PickupSyncInfo);
                pickupSyncInfo.ItemId = __instance.ItemTypeId;
                pickupSyncInfo.Locked = !__instance._repickupable;
                pickupSyncInfo.Serial = __instance.ItemSerial;
                pickupSyncInfo.Weight = __instance.Weight;
                pickupSyncInfo.Position = thrownProjectile.transform.position;
                pickupSyncInfo.Rotation = new LowPrecisionQuaternion(thrownProjectile.transform.rotation);
                var newInfo = thrownProjectile.NetworkInfo = pickupSyncInfo;
                thrownProjectile.PreviousOwner = new Footprint(__instance.Owner);
                NetworkServer.Spawn(thrownProjectile.gameObject);
                pickupSyncInfo = default;
                thrownProjectile.InfoReceived(pickupSyncInfo, newInfo);
                if (thrownProjectile.TryGetComponent<Rigidbody>(out var component))
                    __instance.PropelBody(component, torque, startVel, forceAmount * 2f, upwardFactor / 1.1f);

                if (ImpItem.Instance.TrackedSerials.Contains(__instance.ItemSerial))
                {
                    ExplodeDestructiblesPatch.Grenades.Add(thrownProjectile.netId);
                    thrownProjectile.gameObject.AddComponent<ImpComponent>();
                }
                else if (StickyGrenadeItem.Instance.TrackedSerials.Contains(__instance.ItemSerial))
                {
                    ExplodeDestructiblesPatch.Grenades.Add(thrownProjectile.netId);
                    thrownProjectile.gameObject.AddComponent<StickyComponent>();
                }

                thrownProjectile.ServerActivate();
            }

            return false;
        }
    }
}