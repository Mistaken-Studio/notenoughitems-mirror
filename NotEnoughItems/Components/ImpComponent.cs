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
    /// Handles explosion on impact.
    /// </summary>
    public class ImpComponent : MonoBehaviour
    {
        private DateTime elapsedTime;
        private ExplosionGrenade grenade;

        private void Awake()
        {
            this.grenade = this.GetComponent<ExplosionGrenade>();
            this.elapsedTime = DateTime.Now;
        }

        private void OnCollisionEnter(Collision collider)
        {
            if ((DateTime.Now - this.elapsedTime).TotalSeconds < 0.15f)
                return;

            if (collider.gameObject.TryGetComponent<IDestructible>(out var component))
            {
                if (ReferenceHub.TryGetHubNetID(component.NetworkId, out var referenceHub))
                {
                    Log.Debug("Previous owner's netid: " + this.grenade.PreviousOwner);
                    Log.Debug("component's netid: " + component.NetworkId);
                    if (this.grenade.PreviousOwner.PlayerId == referenceHub?.playerId)
                        return;
                }
            }

            this.grenade.TargetTime = 0.1f;
        }
    }
}