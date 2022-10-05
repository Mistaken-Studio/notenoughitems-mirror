// -----------------------------------------------------------------------
// <copyright file="ImpComponent.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Exiled.API.Features;
using InventorySystem.Items.ThrowableProjectiles;
using UnityEngine;

namespace Mistaken.NotEnoughItems.Components
{
    /// <summary>
    ///     Handles explosion on impact.
    /// </summary>
    public class ImpComponent : MonoBehaviour
    {
        private DateTime elapsedTime;
        private ExplosionGrenade grenade;

        private void Awake()
        {
            grenade = GetComponent<ExplosionGrenade>();
            elapsedTime = DateTime.Now;
        }

        private void OnCollisionEnter(Collision collider)
        {
            if ((DateTime.Now - elapsedTime).TotalSeconds < 0.15f)
                return;
            if (collider.gameObject.TryGetComponent<IDestructible>(out var component))
                if (ReferenceHub.TryGetHubNetID(component.NetworkId, out var referenceHub))
                {
                    Log.Debug("Previous owner's netid: " + grenade.PreviousOwner);
                    Log.Debug("component's netid: " + component.NetworkId);
                    if (grenade.PreviousOwner.PlayerId == referenceHub?.playerId)
                        return;
                }

            grenade.TargetTime = 0.1f;
        }
    }
}