// -----------------------------------------------------------------------
// <copyright file="ExplodeDestructiblesPatch.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using CustomPlayerEffects;
using HarmonyLib;
using InventorySystem.Items.Firearms.BasicMessages;
using InventorySystem.Items.MicroHID;
using InventorySystem.Items.ThrowableProjectiles;
using UnityEngine;

namespace Mistaken.NotEnoughItems.Patches
{
    /// <summary>
    /// Patch for changing damage done from explosion to players.
    /// </summary>
    [HarmonyPatch(typeof(ExplosionGrenade), "ExplodeDestructible", typeof(IDestructible))]
    public static class ExplodeDestructiblesPatch
    {
        /// <summary>
        /// Gets or sets HashSet of impact grenades.
        /// </summary>
        public static HashSet<ThrownProjectile> Grenades { get; set; } = new HashSet<ThrownProjectile>();

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
        private static bool Prefix(ExplosionGrenade __instance, IDestructible dest, ref bool __result)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
        {
            if (Physics.Linecast(dest.CenterOfMass, __instance.transform.position, MicroHIDItem.WallMask))
            {
                __result = false;
                return false;
            }

            float time;
            if (Grenades.Contains(__instance))
                time = Vector3.Distance(dest.CenterOfMass, __instance.transform.position) * 3;
            else
                return true;
            float num = __instance._playerDamageOverDistance.Evaluate(time);
            ReferenceHub referenceHub;
            bool flag = ReferenceHub.TryGetHubNetID(dest.NetworkId, out referenceHub);
            if (flag && referenceHub.characterClassManager.CurRole.team == Team.SCP)
                num *= __instance._scpDamageMultiplier;

            num /= 1.5f;
            if (num > 0f && dest.Damage(num, __instance, __instance.PreviousOwner, dest.CenterOfMass) && flag)
            {
                float num2 = __instance._effectDurationOverDistance.Evaluate(time);
                bool flag2 = __instance.PreviousOwner.Hub == referenceHub;
                if (num2 > 0f && (flag2 || HitboxIdentity.CheckFriendlyFire(__instance.PreviousOwner.Role, referenceHub.characterClassManager.CurClass, false)))
                {
                    referenceHub.playerEffectsController.EnableEffect<Burned>(num2 * __instance._burnedDuration, true);
                    referenceHub.playerEffectsController.EnableEffect<Deafened>(num2 * __instance._deafenedDuration, true);
                    referenceHub.playerEffectsController.EnableEffect<Concussed>(num2 * __instance._concussedDuration, true);
                }

                if (!flag2 && __instance.PreviousOwner.Hub != null)
                    Hitmarker.SendHitmarker(__instance.PreviousOwner.Hub.networkIdentity.connectionToClient, 100);

                referenceHub.inventory.connectionToClient.Send<GunHitMessage>(
                    new GunHitMessage
                    {
                        Weapon = ItemType.None,
                        Damage = (byte)Mathf.Clamp(num * 2.5f, 0f, 255f),
                        DamagePosition = __instance.transform.position,
                    }, 0);
            }

            __result = true;
            return false;
        }
    }
}
