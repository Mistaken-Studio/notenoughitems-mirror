// -----------------------------------------------------------------------
// <copyright file="ImpHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Linq;
using Exiled.API.Interfaces;
using Mistaken.API.Diagnostics;
using UnityEngine;

namespace Mistaken.NotEnoughItems.Handlers
{
    /// <inheritdoc/>
    public class ImpHandler : Module
    {
        /// <inheritdoc cref="Module.Module(IPlugin{IConfig})"/>
        public ImpHandler(IPlugin<IConfig> plugin)
            : base(plugin)
        {
            Instance = this;
            new Items.ImpItem().TryRegister();
        }

        /// <inheritdoc/>
        public override string Name => nameof(ImpHandler);

        /// <inheritdoc/>
        public override void OnEnable()
        {
            Exiled.Events.Handlers.Server.RoundStarted += this.Server_RoundStarted;
        }

        /// <inheritdoc/>
        public override void OnDisable()
        {
            Exiled.Events.Handlers.Server.RoundStarted -= this.Server_RoundStarted;
        }

        internal static readonly Vector3 Size = new Vector3(1f, 0.4f, 1f);

        internal static ImpHandler Instance { get; private set; }

        private void Server_RoundStarted()
        {
            Patches.ExplodeDestructiblesPatch.Grenades.Clear();
            Patches.ServerThrowPatch.ThrowedItems.Clear();
            var structureLockers = UnityEngine.Object.FindObjectsOfType<MapGeneration.Distributors.SpawnableStructure>().Where(x => x.StructureType == MapGeneration.Distributors.StructureType.LargeGunLocker);
            var lockers = structureLockers.Select(x => x as MapGeneration.Distributors.Locker).Where(x => x.Chambers.Length > 8).ToArray();
            var locker = lockers[UnityEngine.Random.Range(0, lockers.Length)];
            int toSpawn = 6;
            while (toSpawn > 0)
            {
                var chamber = locker.Chambers[UnityEngine.Random.Range(0, locker.Chambers.Length)];
                var pickup = Items.ImpItem.Instance.Spawn(chamber._spawnpoint.position + (Vector3.up / 10));
                chamber._content.Add(pickup.Base);
                toSpawn--;
            }
        }
    }
}
