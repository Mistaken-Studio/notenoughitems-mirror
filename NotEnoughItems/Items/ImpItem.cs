// -----------------------------------------------------------------------
// <copyright file="ImpItem.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Spawn;
using Exiled.CustomItems.API.Features;
using Exiled.Events.EventArgs;
using MEC;
using Mistaken.API.CustomItems;
using Mistaken.API.Extensions;
using Mistaken.API.GUI;
using Mistaken.RoundLogger;
using UnityEngine;

namespace Mistaken.NotEnoughItems.Items
{
    /// <summary>
    /// Grenade that explodes on impact.
    /// </summary>
    public class ImpItem : MistakenCustomGrenade
    {
        /// <inheritdoc/>
        public override MistakenCustomItems CustomItem => MistakenCustomItems.IMPACT_GRENADE;

        /// <inheritdoc/>
        public override ItemType Type { get; set; } = ItemType.GrenadeHE;

        /// <inheritdoc/>
        public override string Name { get; set; } = "Impact Grenade";

        /// <inheritdoc/>
        public override string Description { get; set; } = "Grenade that explodes on impact";

        /// <inheritdoc/>
        public override float Weight { get; set; } = 0.01f;

        /// <inheritdoc/>
        public override SpawnProperties SpawnProperties { get; set; }

        /// <inheritdoc/>
        public override bool ExplodeOnCollision { get; set; }

        /// <inheritdoc/>
        public override float FuseTime { get; set; } = 3;

        /// <inheritdoc/>
        public override void Give(Player player, bool displayMessage)
        {
            RLogger.Log("IMPACT GRENADE", "GIVE", $"{this.Name} given to {player.PlayerToString()}");
            base.Give(player, displayMessage);
        }

        /// <inheritdoc/>
        public override Pickup Spawn(Vector3 position)
        {
            var pickup = base.Spawn(position);
            pickup.Scale = Handlers.ImpHandler.Size;
            pickup.Base.Info.Serial = pickup.Serial;
            RLogger.Log("IMPACT GRENADE", "SPAWN", $"{this.Name} spawned");
            return pickup;
        }

        /// <inheritdoc/>
        public override Pickup Spawn(Vector3 position, Item item)
        {
            var pickup = base.Spawn(position, item);
            pickup.Scale = Handlers.ImpHandler.Size;
            pickup.Base.Info.Serial = pickup.Serial;
            return pickup;
        }

        /// <inheritdoc/>
        protected override void ShowPickedUpMessage(Player player)
        {
            RLogger.Log("IMPACT GRENADE", "PICKUP", $"{player.PlayerToString()} Picked up an {this.Name}");
            player.SetGUI("imppickedupmessage", PseudoGUIPosition.MIDDLE, string.Format(PluginHandler.Instance.Translation.ItemPickedUpMessage, PluginHandler.Instance.Translation.ImpactGrenade), 2f);
        }

        /// <inheritdoc/>
        protected override void OnThrowing(ThrowingItemEventArgs ev)
        {
            if (ev.RequestType != ThrowRequest.BeginThrow)
            {
                RLogger.Log("IMPACT GRENADE", "THROW", $"{ev.Player.PlayerToString()} threw an {this.Name}");
                Patches.ServerThrowPatch.ThrowedItems.Add(ev.Item.Base);
                ev.Player.RemoveItem(ev.Item);
            }
        }

        /// <inheritdoc/>
        protected override void OnExploding(ExplodingGrenadeEventArgs ev)
        {
            RLogger.Log("IMPACT GRENADE", "EXPLODED", $"Impact grenade exploded");
            foreach (var player in ev.TargetsToAffect)
                RLogger.Log("IMPACT GRENADE", "HURT", $"{player.PlayerToString()} was hurt by an {this.Name}");
        }

        /// <inheritdoc/>
        protected override void ShowSelectedMessage(Player player)
        {
            Handlers.ImpHandler.Instance.RunCoroutine(this.UpdateInterface(player));
        }

        private IEnumerator<float> UpdateInterface(Player player)
        {
            yield return Timing.WaitForSeconds(0.1f);
            while (this.Check(player.CurrentItem))
            {
                player.SetGUI("impact", PseudoGUIPosition.BOTTOM, string.Format(PluginHandler.Instance.Translation.ItemHoldingMessage, PluginHandler.Instance.Translation.ImpactGrenade));
                yield return Timing.WaitForSeconds(1);
            }

            player.SetGUI("impact", PseudoGUIPosition.BOTTOM, null);
        }
    }
}
