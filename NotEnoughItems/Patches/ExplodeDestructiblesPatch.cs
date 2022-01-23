﻿// -----------------------------------------------------------------------
// <copyright file="ExplodeDestructiblesPatch.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
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
    [HarmonyPatch(typeof(ExplosionGrenade), "ExplodeDestructible")]
    public static class ExplodeDestructiblesPatch
    {
        /// <summary>
        /// Gets or sets HashSet of impact grenades.
        /// </summary>
        public static HashSet<ThrownProjectile> Grenades { get; set; } = new HashSet<ThrownProjectile>();

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
        private static bool Prefix(ExplosionGrenade __instance, IDestructible dest, Footprinting.Footprint attacker, Vector3 pos, ExplosionGrenade setts, ref bool __result)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
        {
            if (!Grenades.Contains(__instance))
                return true;

            if (Physics.Linecast(dest.CenterOfMass, pos, MicroHIDItem.WallMask))
            {
                __result = false;
                return false;
            }

            Vector3 a = dest.CenterOfMass - pos;
            float magnitude = a.magnitude;
            float num = setts._playerDamageOverDistance.Evaluate(magnitude);
            ReferenceHub referenceHub;
            bool flag = ReferenceHub.TryGetHubNetID(dest.NetworkId, out referenceHub);
            if (flag && referenceHub.characterClassManager.CurRole.team == Team.SCP)
                num *= setts._scpDamageMultiplier;
            Vector3 force = ((1f - (magnitude / setts._maxRadius)) * (a / magnitude) * setts._rigidbodyBaseForce) + (Vector3.up * setts._rigidbodyLiftForce);
            if (num > 0f && dest.Damage(num, new PlayerStatsSystem.ExplosionDamageHandler(attacker, force, num, 15), dest.CenterOfMass) && flag)
            {
                float num2 = setts._effectDurationOverDistance.Evaluate(magnitude);
                bool flag2 = attacker.Hub == referenceHub;
                if (num2 > 0f && (flag2 || HitboxIdentity.CheckFriendlyFire(attacker.Role, referenceHub.characterClassManager.CurClass, false)))
                {
                    referenceHub.playerEffectsController.EnableEffect<CustomPlayerEffects.Burned>(num2 * setts._burnedDuration, true);
                    referenceHub.playerEffectsController.EnableEffect<CustomPlayerEffects.Deafened>(num2 * setts._deafenedDuration, true);
                    referenceHub.playerEffectsController.EnableEffect<CustomPlayerEffects.Concussed>(num2 * setts._concussedDuration, true);
                }

                if (!flag2 && attacker.Hub != null)
                    Hitmarker.SendHitmarker(attacker.Hub, 1f);
                referenceHub.inventory.connectionToClient.Send<GunHitMessage>(new GunHitMessage(false, num, pos), 0);
            }

            __result = true;
            return false;
        }
    }
}
