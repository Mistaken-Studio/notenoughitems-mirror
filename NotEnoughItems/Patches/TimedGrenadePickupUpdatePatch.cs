// -----------------------------------------------------------------------
// <copyright file="TimedGrenadePickupUpdatePatch.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Exiled.CustomItems.API.Features;
using HarmonyLib;
using InventorySystem;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.ThrowableProjectiles;
using Mirror;
using Mistaken.API.CustomItems;
using UnityEngine;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

namespace Mistaken.NotEnoughItems.Patches
{
    [HarmonyPatch(typeof(TimedGrenadePickup), nameof(TimedGrenadePickup.Update))]
    internal static class TimedGrenadePickupUpdatePatch
    {
        private static bool Prefix(TimedGrenadePickup __instance)
        {
            ThrowableItem throwableItem;
            if (__instance._replaceNextFrame && InventoryItemLoader.AvailableItems.TryGetValue(__instance.Info.ItemId, out var value) && (object)(throwableItem = value as ThrowableItem) != null)
            {
                ThrownProjectile thrownProjectile = Object.Instantiate(throwableItem.Projectile);
                if (thrownProjectile.TryGetComponent<Rigidbody>(out var component))
                {
                    component.position = __instance.Rb.position;
                    component.rotation = __instance.Rb.rotation;
                    component.velocity = __instance.Rb.velocity;
                    component.angularVelocity = component.angularVelocity;
                }

                __instance.Info.Locked = true;
                thrownProjectile.NetworkInfo = __instance.Info;
                thrownProjectile.PreviousOwner = __instance._attacker;
                NetworkServer.Spawn(thrownProjectile.gameObject);
                CustomItem item;
                if (MistakenCustomItems.IMPACT_GRENADE.TryGet(out item) && !(item is null))
                {
                    if (item.TrackedSerials.Contains(__instance.Info.Serial))
                    {
                        ExplodeDestructiblesPatch.Grenades.Add(thrownProjectile.netId);
                        thrownProjectile.gameObject.AddComponent<Components.ImpComponent>();
                    }
                }

                if (MistakenCustomItems.STICKY_GRENADE.TryGet(out item) && !(item is null))
                {
                    if (item.TrackedSerials.Contains(__instance.Info.Serial))
                    {
                        ExplodeDestructiblesPatch.Grenades.Add(thrownProjectile.netId);
                        thrownProjectile.gameObject.AddComponent<Components.StickyComponent>();
                    }
                }

                thrownProjectile.InfoReceived(default(PickupSyncInfo), __instance.Info);
                thrownProjectile.ServerActivate();
                __instance.DestroySelf();
                __instance._replaceNextFrame = false;
            }

            return false;
        }
    }
}
