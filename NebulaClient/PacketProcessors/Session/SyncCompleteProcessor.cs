﻿using NebulaModel.Attributes;
using NebulaModel.Networking;
using NebulaModel.Packets.Processors;
using NebulaModel.Packets.Session;
using NebulaWorld;

namespace NebulaClient.PacketProcessors.Session
{
    [RegisterPacketProcessor]
    public class SyncCompleteProcessor : IPacketProcessor<SyncComplete>
    {
        public void ProcessPacket(SyncComplete packet, NebulaConnection conn)
        {
            InGamePopup.FadeOut();
            GameMain.Resume();
        }
    }
}
