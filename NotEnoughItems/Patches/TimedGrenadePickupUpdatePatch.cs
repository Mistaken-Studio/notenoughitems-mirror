// -----------------------------------------------------------------------
// <copyright file="TimedGrenadePickupUpdatePatch.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Exiled.CustomItems.API.Features;
using HarmonyLib;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.ThrowableProjectiles;
using Mirror;
using Mistaken.API.CustomItems;
using UnityEngine;

namespace Mistaken.NotEnoughItems.Patches
{
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
    [HarmonyPatch(typeof(TimedGrenadePickup), "Update")]
    internal static class TimedGrenadePickupUpdatePatch
    {
        private static bool Prefix(TimedGrenadePickup __instance)
        {
            if (!__instance._replaceNextFrame)
            {
                return false;
            }

            ItemBase itemBase;
            if (!InventoryItemLoader.AvailableItems.TryGetValue(__instance.Info.ItemId, out itemBase))
            {
                return false;
            }

            ThrowableItem throwableItem = (ThrowableItem)itemBase;
            if (throwableItem == null)
            {
                return false;
            }

            ThrownProjectile thrownProjectile = UnityEngine.Object.Instantiate<ThrownProjectile>(throwableItem.Projectile);
            Rigidbody rigidbody;
            if (thrownProjectile.TryGetComponent<Rigidbody>(out rigidbody))
            {
                rigidbody.position = __instance.Rb.position;
                rigidbody.rotation = __instance.Rb.rotation;
                rigidbody.velocity = __instance.Rb.velocity;
                rigidbody.angularVelocity = rigidbody.angularVelocity;
            }

            __instance.Info.Locked = true;
            thrownProjectile.NetworkInfo = __instance.Info;
            thrownProjectile.PreviousOwner = __instance._attacker;
            NetworkServer.Spawn(thrownProjectile.gameObject, ownerConnection: null);
            CustomItem item;
            if (MistakenCustomItems.IMPACT_GRENADE.TryGet(out item) && !(item is null))
            {
                if (item.TrackedSerials.Contains(__instance.Info.Serial))
                {
                    ExplodeDestructiblesPatch.Grenades.Add(thrownProjectile);
                    thrownProjectile.gameObject.AddComponent<Components.ImpComponent>();
                }
            }

            if (MistakenCustomItems.STICKY_GRENADE.TryGet(out item) && !(item is null))
            {
                if (item.TrackedSerials.Contains(__instance.Info.Serial))
                {
                    ExplodeDestructiblesPatch.Grenades.Add(thrownProjectile);
                    thrownProjectile.gameObject.AddComponent<Components.StickyComponent>();
                }
            }

            thrownProjectile.InfoReceived(default(InventorySystem.Items.Pickups.PickupSyncInfo), __instance.Info);
            thrownProjectile.ServerActivate();
            __instance.DestroySelf();
            __instance._replaceNextFrame = false;

            return false;
        }
    }
}
