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
using PlayerStatsSystem;
using UnityEngine;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

namespace Mistaken.NotEnoughItems.Patches
{
    [HarmonyPatch(typeof(ExplosionGrenade), nameof(ExplosionGrenade.ExplodeDestructible))]
    internal static class ExplodeDestructiblesPatch
    {
        public static HashSet<uint> Grenades { get; set; } = new HashSet<uint>();

        private static bool Prefix(IDestructible dest, Footprinting.Footprint attacker, Vector3 pos, ExplosionGrenade setts, ref bool __result)
        {
            if (!Grenades.Contains(setts.netId))
            {
                return true;
            }

            if (Physics.Linecast(dest.CenterOfMass, pos, MicroHIDItem.WallMask))
            {
                __result = false;
                return false;
            }

            Vector3 vector = dest.CenterOfMass - pos;
            float magnitude = vector.magnitude;
            float num = setts._playerDamageOverDistance.Evaluate(magnitude);
            ReferenceHub hub;
            bool flag = ReferenceHub.TryGetHubNetID(dest.NetworkId, out hub);
            if (flag && hub.characterClassManager.CurRole.team == Team.SCP)
            {
                num *= setts._scpDamageMultiplier;
            }

            Vector3 force = ((1f - (magnitude / setts._maxRadius)) * (vector / magnitude) * setts._rigidbodyBaseForce) + (Vector3.up * setts._rigidbodyLiftForce);

            num /= 2.8f;

            if (num > 0f && dest.Damage(num, new ExplosionDamageHandler(attacker, force, num, 80), dest.CenterOfMass) && flag)
            {
                float num2 = setts._effectDurationOverDistance.Evaluate(magnitude);
                bool flag2 = attacker.Hub == hub;
                if (num2 > 0f && (flag2 || HitboxIdentity.CheckFriendlyFire(attacker.Role, hub.characterClassManager.CurClass)))
                {
                    float minimalDuration = setts._minimalDuration;
                    ExplosionGrenade.TriggerEffect<Burned>(hub, num2 * setts._burnedDuration, minimalDuration);
                    ExplosionGrenade.TriggerEffect<Deafened>(hub, num2 * setts._deafenedDuration, minimalDuration);
                    ExplosionGrenade.TriggerEffect<Concussed>(hub, num2 * setts._concussedDuration, minimalDuration);
                }

                if (!flag2 && attacker.Hub != null)
                {
                    Hitmarker.SendHitmarker(attacker.Hub, 1f);
                }

                hub.inventory.connectionToClient.Send(new GunHitMessage(drawBlood: false, num, pos));
            }

            __result = true;
            return false;
        }
    }
}
