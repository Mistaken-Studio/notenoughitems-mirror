// -----------------------------------------------------------------------
// <copyright file="StickyComponent.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Exiled.API.Features;
using InventorySystem.Items.ThrowableProjectiles;
using MEC;
using Mirror;
using Mistaken.API.Components;
using UnityEngine;

namespace Mistaken.NotEnoughItems.Components
{
    /// <summary>
    /// Handles freeze on impact with surfaces.
    /// </summary>
    public class StickyComponent : MonoBehaviour
    {
        private DateTime elapsedTime;
        private Player grenadePlayer;
        private bool ignoreOwner;
        private InRange inRange;
        private Action<Player> onEnter;
        private bool onPlayerUsed;
        private bool onSurfaceUsed;
        private int owner;
        private Vector3 positionDiff;
        private Rigidbody rigidbody;

        private void Awake()
        {
            this.rigidbody = this.GetComponent<Rigidbody>();
            this.owner = this.GetComponent<ExplosionGrenade>().PreviousOwner.PlayerId;
            this.ignoreOwner = true;
            this.elapsedTime = DateTime.Now;

            this.onEnter = player =>
            {
                if (this.ignoreOwner && player.Id == this.owner)
                    return;

                this.grenadePlayer = player;
                this.onPlayerUsed = true;
                this.rigidbody.constraints = RigidbodyConstraints.FreezeAll;
                this.positionDiff = this.grenadePlayer.Position - this.transform.position;
            };

            this.inRange = InRange.Spawn(this.transform, Vector3.zero, new Vector3(0.05f, 0.05f, 0.05f), this.onEnter);
            Timing.CallDelayed(0.5f, () => this.ignoreOwner = false);
        }

        private void FixedUpdate()
        {
            if (this.onPlayerUsed)
                this.transform.position = this.grenadePlayer.Position + this.positionDiff;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if ((DateTime.Now - this.elapsedTime).TotalSeconds < 0.15f)
                return;

            if (!this.onSurfaceUsed && !this.onPlayerUsed)
            {
                this.rigidbody.constraints = RigidbodyConstraints.FreezeAll;
                NetworkServer.Destroy(this.inRange.gameObject);
                this.onSurfaceUsed = true;
            }
        }
    }
}