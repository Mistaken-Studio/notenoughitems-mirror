// -----------------------------------------------------------------------
// <copyright file="StickyComponent.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Exiled.API.Features;
using InventorySystem.Items.ThrowableProjectiles;
using MEC;
using UnityEngine;

namespace Mistaken.NotEnoughItems.Components
{
    /// <summary>
    /// Handles freeze on impact with surfaces.
    /// </summary>
    public class StickyComponent : MonoBehaviour
    {
        private bool onPlayerUsed;
        private bool onSurfaceUsed;
        private Rigidbody rigidbody;
        private Player grenadePlayer;
        private Action<Player> onEnter;
        private Vector3 positionDiff;
        private int owner;
        private bool ignoreOwner;
        private DateTime elapsedTime;
        private Mistaken.API.Components.InRange inRange;

        private void Awake()
        {
            this.rigidbody = this.GetComponent<Rigidbody>();
            this.owner = this.GetComponent<ExplosionGrenade>().PreviousOwner.PlayerId;
            this.ignoreOwner = true;
            this.elapsedTime = DateTime.Now;

            this.onEnter = (player) =>
            {
                if (this.ignoreOwner && player.Id == this.owner) return;
                this.grenadePlayer = player;
                this.onPlayerUsed = true;
                this.rigidbody.constraints = RigidbodyConstraints.FreezeAll;
                this.positionDiff = this.grenadePlayer.Position - this.transform.position;
            };

            this.inRange = Mistaken.API.Components.InRange.Spawn(this.transform, Vector3.zero, new Vector3(0.05f, 0.05f, 0.05f), this.onEnter);
            Timing.CallDelayed(0.5f, () => this.ignoreOwner = false);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if ((DateTime.Now - this.elapsedTime).TotalSeconds < 0.15f) return;
            if (!this.onSurfaceUsed && !this.onPlayerUsed)
            {
                this.rigidbody.constraints = RigidbodyConstraints.FreezeAll;
                Destroy(this.inRange);
                this.onSurfaceUsed = true;
            }
        }

        private void FixedUpdate()
        {
            if (this.onPlayerUsed)
                this.transform.position = this.grenadePlayer.Position + this.positionDiff;
        }
    }
}
