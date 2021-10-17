﻿// -----------------------------------------------------------------------
// <copyright file="TaserHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Interfaces;
using Mistaken.API;
using Mistaken.API.CustomItems;
using Mistaken.API.Diagnostics;
using Mistaken.API.Extensions;
using UnityEngine;

namespace Mistaken.NotEnoughItems.Handlers
{
    /// <inheritdoc/>
    public class TaserHandler : Module
    {
        /// <inheritdoc cref="Module.Module(IPlugin{IConfig})"/>
        public TaserHandler(IPlugin<IConfig> plugin)
            : base(plugin)
        {
            Instance = this;
            new Items.TaserItem().TryRegister();
        }

        /// <inheritdoc/>
        public override string Name => nameof(TaserHandler);

        /// <inheritdoc/>
        public override void OnEnable()
        {
            Exiled.Events.Handlers.Server.RoundStarted += this.Handle(() => this.Server_RoundStarted(), "RoundStart");
            Exiled.Events.Handlers.Player.ChangingRole += this.Handle<Exiled.Events.EventArgs.ChangingRoleEventArgs>((ev) => this.Player_ChangingRole(ev));
        }

        /// <inheritdoc/>
        public override void OnDisable()
        {
            Exiled.Events.Handlers.Server.RoundStarted -= this.Handle(() => this.Server_RoundStarted(), "RoundStart");
            Exiled.Events.Handlers.Player.ChangingRole -= this.Handle<Exiled.Events.EventArgs.ChangingRoleEventArgs>((ev) => this.Player_ChangingRole(ev));
        }

        internal static readonly Dictionary<GameObject, Door> Doors = new Dictionary<GameObject, Door>();

        internal static readonly HashSet<ItemType> UsableItems = new HashSet<ItemType>()
        {
            ItemType.MicroHID,
            ItemType.Medkit,
            ItemType.Painkillers,
            ItemType.SCP018,
            ItemType.SCP207,
            ItemType.SCP268,
            ItemType.SCP500,
            ItemType.GrenadeHE,
            ItemType.GrenadeFlash,
            ItemType.Adrenaline,
        };

        internal static TaserHandler Instance { get; set; }

        private void Server_RoundStarted()
        {
            foreach (var door in Map.Doors)
            {
                foreach (var child in door.Base.GetComponentsInChildren<BoxCollider>())
                    Doors[child.gameObject] = door;
            }

            var structureLockers = UnityEngine.Object.FindObjectsOfType<MapGeneration.Distributors.SpawnableStructure>().Where(x => x.StructureType == MapGeneration.Distributors.StructureType.LargeGunLocker);
            var lockers = structureLockers.Select(x => x as MapGeneration.Distributors.Locker).Where(x => x.Chambers.Length > 8).ToArray();
            var locker = lockers[UnityEngine.Random.Range(0, lockers.Length)];
            int toSpawn = 1;
            while (toSpawn > 0)
            {
                var chamber = locker.Chambers[UnityEngine.Random.Range(0, locker.Chambers.Length)];
                MistakenCustomWeapon.TrySpawn(MistakenCustomItems.TASER, chamber._spawnpoint.position + (Vector3.up / 10), out var pickup);
                chamber._content.Add(pickup.Base);
                toSpawn--;
            }
        }

        private void Player_ChangingRole(Exiled.Events.EventArgs.ChangingRoleEventArgs ev)
        {
            if (ev.Player.GetSessionVar<bool>(SessionVarType.ITEM_LESS_CLSSS_CHANGE))
                return;
            if (ev.NewRole == RoleType.FacilityGuard)
            {
                ev.Items.Remove(ItemType.GunCOM18);
                this.CallDelayed(
                    0.25f,
                    () =>
                    {
                        if (ev.Player.Items.Count >= 8)
                            MistakenCustomWeapon.TrySpawn(MistakenCustomItems.TASER, ev.Player.Position, out _);
                        else
                            MistakenCustomWeapon.TryGive(ev.Player, MistakenCustomItems.TASER);
                    },
                    "ChangingRole");
            }
        }
    }
}