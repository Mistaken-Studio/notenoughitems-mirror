// -----------------------------------------------------------------------
// <copyright file="ImpComponent.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using InventorySystem.Items.ThrowableProjectiles;
using UnityEngine;

namespace Mistaken.NotEnoughItems.Components
{
    /// <summary>
    /// Handles explosion on impact.
    /// </summary>
    public class ImpComponent : MonoBehaviour
    {
        private ExplosionGrenade grenade;
        private DateTime elapsedTime;

        private void Awake()
        {
            this.grenade = this.GetComponent<ExplosionGrenade>();
            this.elapsedTime = DateTime.Now;
        }

        private void OnCollisionEnter(Collision collider)
        {
            if ((DateTime.Now - this.elapsedTime).TotalSeconds < 0.15f) return;
            if (collider.gameObject.TryGetComponent<IDestructible>(out var component))
            {
                if (ReferenceHub.TryGetHubNetID(component.NetworkId, out var referenceHub))
                {
                    if (this.grenade.PreviousOwner.Hub != referenceHub)
                        return;
                }
            }

            this.grenade.TargetTime = 0.1f;
        }
    }
}
