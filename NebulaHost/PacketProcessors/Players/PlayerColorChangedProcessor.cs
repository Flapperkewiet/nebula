﻿using LiteNetLib;
using NebulaModel.Attributes;
using NebulaModel.Networking;
using NebulaModel.Packets.Players;
using NebulaModel.Packets.Processors;
using NebulaWorld;

namespace NebulaHost.PacketProcessors.Players
{
    [RegisterPacketProcessor]
    public class PlayerColorChangedProcessor : IPacketProcessor<PlayerColorChanged>
    {
        private PlayerManager playerManager;

        public PlayerColorChangedProcessor()
        {
            playerManager = MultiplayerHostSession.Instance.PlayerManager;
        }

        public void ProcessPacket(PlayerColorChanged packet, NebulaConnection conn)
        {
            Player player = playerManager.GetPlayer(conn);
            NebulaModel.Logger.Log.Info(conn.Id);
            if(player == null)
            {
                NebulaModel.Logger.Log.Warn($"Received PlayerColorChanged packet from unknown player {conn.Id}");
                return;
            }
            player.Data.Color = packet.Color;
            playerManager.SendPacketToOtherPlayers(packet, player, DeliveryMethod.ReliableUnordered);

            SimulatedWorld.UpdatePlayerColor(packet.PlayerId, packet.Color);
        }
    }
}