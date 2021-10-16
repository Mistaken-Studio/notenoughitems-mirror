// -----------------------------------------------------------------------
// <copyright file="ImpComponent.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

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

        private void Awake()
        {
            this.grenade = this.GetComponent<ExplosionGrenade>();
        }

        private void OnCollisionEnter(Collision collider)
        {
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
