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
    ///     Handles freeze on impact with surfaces.
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
            rigidbody = GetComponent<Rigidbody>();
            owner = GetComponent<ExplosionGrenade>().PreviousOwner.PlayerId;
            ignoreOwner = true;
            elapsedTime = DateTime.Now;

            onEnter = player =>
            {
                if (ignoreOwner && player.Id == owner)
                    return;
                grenadePlayer = player;
                onPlayerUsed = true;
                rigidbody.constraints = RigidbodyConstraints.FreezeAll;
                positionDiff = grenadePlayer.Position - transform.position;
            };

            inRange = InRange.Spawn(transform, Vector3.zero, new Vector3(0.05f, 0.05f, 0.05f), onEnter);
            Timing.CallDelayed(0.5f, () => ignoreOwner = false);
        }

        private void FixedUpdate()
        {
            if (onPlayerUsed)
                transform.position = grenadePlayer.Position + positionDiff;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if ((DateTime.Now - elapsedTime).TotalSeconds < 0.15f)
                return;
            if (!onSurfaceUsed && !onPlayerUsed)
            {
                rigidbody.constraints = RigidbodyConstraints.FreezeAll;
                NetworkServer.Destroy(inRange.gameObject);
                onSurfaceUsed = true;
            }
        }
    }
}