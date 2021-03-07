using NebulaModel.DataStructures;
using NebulaModel.Logger;
using NebulaModel.Packets.Planet;
using NebulaModel.Packets.Players;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace NebulaWorld
{
    /// <summary>
    /// This class keeps track of our simulated world. It holds all temporary entities like remote player models 
    /// and also helps to execute some remote player actions that you would want to replicate on the local client.
    /// </summary>
    public static class SimulatedWorld
    {
        static Dictionary<ushort, RemotePlayerModel> remotePlayersModels;

        public static bool Initialized { get; private set; }

        public static void Initialize()
        {
            remotePlayersModels = new Dictionary<ushort, RemotePlayerModel>();
            Initialized = true;
        }

        /// <summary>
        /// Removes any simulated entities that was added to the scene for a game.
        /// </summary>
        public static void Clear()
        {
            foreach (var model in remotePlayersModels.Values)
            {
                model.Destroy();
            }

            remotePlayersModels.Clear();
            Initialized = false;
        }

        public static void UpdateGameState(GameState state)
        {
            // We allow for a small drift of 5 ticks since the tick offset using the ping is only an approximation
            if (GameMain.gameTick > 0 && Mathf.Abs(state.gameTick - GameMain.gameTick) > 5)
            {
                Log.Info($"Game Tick got updated since it was desynced, was {GameMain.gameTick}, received {state.gameTick}");
                GameMain.gameTick = state.gameTick;
            }
        }

        public static void SpawnRemotePlayerModel(PlayerData playerData)
        {
            RemotePlayerModel model = new RemotePlayerModel(playerData.PlayerId);
            remotePlayersModels.Add(playerData.PlayerId, model);
            UpdatePlayerColor(playerData.PlayerId, playerData.Color);
        }

        public static void DestroyRemotePlayerModel(ushort playerId)
        {
            if (remotePlayersModels.TryGetValue(playerId, out RemotePlayerModel player))
            {
                player.Destroy();
                remotePlayersModels.Remove(playerId);
            }
        }

        public static void UpdateRemotePlayerPosition(PlayerMovement packet)
        {
            if (remotePlayersModels.TryGetValue(packet.PlayerId, out RemotePlayerModel player))
            {
                player.Movement.UpdatePosition(packet);
            }
        }

        public static void UpdateRemotePlayerAnimation(PlayerAnimationUpdate packet)
        {
            if (remotePlayersModels.TryGetValue(packet.PlayerId, out RemotePlayerModel player))
            {
                player.Animator.UpdateState(packet);
            }
        }

        public static void UpdatePlayerColor(ushort playerId, Float3 color)
        {
            Transform transform;
            RemotePlayerModel remotePlayerModel;
            if (playerId == LocalPlayer.PlayerId)
            {
                transform = GameMain.data.mainPlayer.transform;
            }
            else if (remotePlayersModels.TryGetValue(playerId, out remotePlayerModel))
            {
                transform = remotePlayerModel.PlayerTransform;
            }
            else
            {
                Log.Error("Could not find the transform for player with ID " + playerId);
                return;
            }

            Log.Info($"Changing color of player {playerId} to {color}");

            // Apply new color to each part of the mecha
            SkinnedMeshRenderer[] componentsInChildren = transform.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (Renderer r in componentsInChildren)
            {
                if (r.material?.name.StartsWith("icarus-armor") ?? false)
                {
                    r.material.SetColor("_Color", color.ToColor());
                }
            }

            // We changed our own color, so we have to let others know
            if (LocalPlayer.PlayerId == playerId)
            {
                LocalPlayer.SendPacket(new PlayerColorChanged(playerId, color));
            }
        }

        public static void MineVegetable(VegeMined packet)
        {
            PlanetData planet = GameMain.galaxy?.PlanetById(packet.PlanetID);
            if (planet == null)
                return;

            if (packet.isVegetable) // Trees, rocks, leaves, etc
            {
                VegeData vData = (VegeData)planet.factory?.GetVegeData(packet.MiningID);
                VegeProto vProto = LDB.veges.Select((int)vData.protoId);
                if (vProto != null && planet.id == GameMain.localPlanet?.id)
                {
                    VFEffectEmitter.Emit(vProto.MiningEffect, vData.pos, vData.rot);
                    VFAudio.Create(vProto.MiningAudio, null, vData.pos, true);
                }
                planet.factory?.RemoveVegeWithComponents(vData.id);
            }
            else // veins
            {
                VeinData vData = (VeinData)planet.factory?.GetVeinData(packet.MiningID);
                VeinProto vProto = LDB.veins.Select((int)vData.type);
                if (vProto != null)
                {
                    if (planet.factory?.veinPool[packet.MiningID].amount > 0)
                    {
                        VeinData[] vPool = planet.factory?.veinPool;
                        PlanetData.VeinGroup[] vGroups = planet.factory?.planet.veinGroups;
                        long[] vAmounts = planet.veinAmounts;
                        vPool[packet.MiningID].amount -= 1;
                        vGroups[(int)vData.groupIndex].amount -= 1;
                        vAmounts[(int)vData.type] -= 1;

                        if (planet.id == GameMain.localPlanet?.id)
                        {
                            VFEffectEmitter.Emit(vProto.MiningEffect, vData.pos, Maths.SphericalRotation(vData.pos, 0f));
                            VFAudio.Create(vProto.MiningAudio, null, vData.pos, true);
                        }
                    }
                    else
                    {
                        PlanetData.VeinGroup[] vGroups = planet.factory?.planet.veinGroups;
                        vGroups[vData.groupIndex].count -= 1;

                        if (planet.id == GameMain.localPlanet?.id)
                        {
                            VFEffectEmitter.Emit(vProto.MiningEffect, vData.pos, Maths.SphericalRotation(vData.pos, 0f));
                            VFAudio.Create(vProto.MiningAudio, null, vData.pos, true);
                        }

                        planet.factory?.RemoveVeinWithComponents(vData.id);
                    }
                }
            }
        }

        public static void OnGameLoadCompleted()
        {
            if (Initialized == false)
                return;

            Log.Info("Game has finished loading");

            // Assign our own color
            UpdatePlayerColor(LocalPlayer.PlayerId, LocalPlayer.Data.Color);

            // TODO: Investigate where are the weird position coming from ?
            // GameMain.mainPlayer.transform.position = data.Position.ToUnity();
            // GameMain.mainPlayer.transform.eulerAngles = data.Rotation.ToUnity();

            LocalPlayer.SetReady();

            int EntitySize = 0;
            int CompressedEntitySize = 0;
            unsafe
            {
                EntitySize = sizeof(EntityData);
                CompressedEntitySize = sizeof(CompressedEntityData);
            }
            Log.Info("EntitySize = " + EntitySize);
            Log.Info("CompressedEntitySize = " + CompressedEntitySize);

            UnlockAllTech();
            WalkingAndCollectingSpeedUp();
            IncreasePlayerRobotLevel();
            GivePlayerAllItems();
            //LogEntityData();
        }

        //Gives the player every item in the game
        public static void GivePlayerAllItems()
        {
            Player player = GameMain.mainPlayer;
            for (int i = 0; i < LDB.items.dataArray.Length; i++)
            {
                ItemProto itemProto = LDB.items.dataArray[i];
                player.package.SetSize(120);
                player.package.AddItem(itemProto.ID, itemProto.StackSize);
            }
        }

        //Increase walking, mining, replicator, powergen and research speed
        public static void WalkingAndCollectingSpeedUp()
        {
            Player player = GameMain.mainPlayer;
            player.mecha.miningSpeed = 20f;
            player.mecha.walkSpeed = 16f;
            player.mecha.replicateSpeed = 5f;
            player.mecha.reactorPowerGen = 1500000000.0;
            GameMain.history.techSpeed = 20;
        }

        //Increase number of robots to 8 and speed to 80
        public static void IncreasePlayerRobotLevel()
        {
            Player player = GameMain.mainPlayer;
            player.mecha.droneMovement = 4;
            player.mecha.droneSpeed = 20f;
        }

        //Unlocks al the tech in the tech tree
        public static void UnlockAllTech()
        {
            var keys = new List<int>(GameMain.history.techStates.Keys);
            foreach (int key in keys)
            {
                if (GameMain.history.TechState(key).unlocked == false)
                    GameMain.history.UnlockTech(key);
            }
        }

        public static void ExportGameTest()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    GameMain.data.Export(bw);
                }
                Log.Info("Raw binary stream contains: " + ms.ToArray().Length + " bytes");
            }
            sw.Stop();
            Log.Info("Raw binary stream took: " + sw.Elapsed.TotalMilliseconds + " ms");
            sw.Reset();
            sw.Start();
            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(ms, CompressionMode.Compress))
                {
                    using (BinaryWriter bw = new BinaryWriter(gzip))
                    {
                        GameMain.data.Export(bw);
                    }
                }
                Log.Info("Gzip stream contains: " + ms.ToArray().Length + " bytes");
            }
            Log.Info("Gzip stream took: " + sw.Elapsed.TotalMilliseconds + " ms");

        }

        public static void LogEntityData()
        {
            ExportGameTest();

            return;
            EntityData[] entities = GameMain.mainPlayer.factory.entityPool;
            int entityCount = GameMain.mainPlayer.factory.entityCount;
            int entityCursor = GameMain.mainPlayer.factory.entityCursor;

            Log.Info("Entities length = " + entities.Length);
            Log.Info("entityCount = " + entityCount);
            Log.Info("entityCursor = " + entityCursor);
            for (int i = 0; i < entityCount + 2; i++)
            {
                EntityData d = entities[i];
                Log.Info($"(id, protoId, modelId)({d.id},{d.protoId},{d.modelId})");
                Log.Info($"(modelIndex, pos, rotation)({d.modelIndex},{d.pos},{d.rot})");
                Log.Info($"\t beltId {d.beltId}");
                Log.Info($"\t splitterId {d.splitterId}");
                Log.Info($"\t storageId {d.storageId}");
                Log.Info($"\t tankId {d.tankId}");
                Log.Info($"\t minerId {d.minerId}");
                Log.Info($"\t inserterId {d.inserterId}");
                Log.Info($"\t assemblerId {d.assemblerId}");
                Log.Info($"\t fractionateId {d.fractionateId}");
                Log.Info($"\t ejectorId {d.ejectorId}");
                Log.Info($"\t siloId {d.siloId}");
                Log.Info($"\t labId {d.labId}");
                Log.Info($"\t stationId {d.stationId}");
                Log.Info($"\t powerNodeId {d.powerNodeId}");
                Log.Info($"\t powerGenId {d.powerGenId}");
                Log.Info($"\t powerConId {d.powerConId}");
                Log.Info($"\t powerAccId {d.powerAccId}");
                Log.Info($"\t powerExcId {d.powerExcId}");
                Log.Info($"\t monsterId {d.monsterId}");
                Log.Info($"\t monsterId {d.monsterId}");
                Log.Info($"\t colliderId {d.colliderId}");
                Log.Info($"\t audioId {d.audioId}");
                CompressedEntityData.Compress(d);

                byte[] rawBytes = getEntityDataBytes(d);
                Log.Info($"rawBytes contains {rawBytes.Length} bytes");
                byte[] gzipRawBytes = GzipCompression(d);
                Log.Info($"GzipCompress contains {gzipRawBytes.Length} bytes");
            }
        }
        public static byte[] GzipCompression(EntityData data)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(ms, CompressionMode.Compress))
                {
                    using (BinaryWriter bw = new BinaryWriter(gzip))
                    {
                        data.Export(bw);
                    }
                }
                return ms.ToArray();
            }
        }

        public static byte[] getEntityDataBytes(EntityData data)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    data.Export(bw);
                }
                return ms.ToArray();
            }
        }
    }



    public struct CompressedEntityData
    {
        public int id;
        public short protoId;
        public short modelIndex;
        public Vector3 pos;
        public Quaternion rot;

        public EntityType entityType;
        public int entityId;

        public int powerNoderId;
        public EntityPowerType powerType;
        public int powerId;

        public int modelId;
        public int mmblockId;
        public int colliderId;
        public int audioId;

        public static CompressedEntityData Compress(EntityData data)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            CompressedEntityData c = new CompressedEntityData();
            c.id = data.id;
            c.protoId = data.protoId;
            c.modelIndex = data.modelIndex;
            c.pos = data.pos;
            c.rot = data.rot;

            if (data.beltId != 0)
            {
                c.entityType = EntityType.belt;
                c.entityId = data.beltId;
            }
            else if (data.splitterId != 0)
            {
                c.entityType = EntityType.splitter;
                c.entityId = data.splitterId;
            }
            else if (data.storageId != 0)
            {
                c.entityType = EntityType.storage;
                c.entityId = data.storageId;
            }
            else if (data.tankId != 0)
            {
                c.entityType = EntityType.tank;
                c.entityId = data.tankId;
            }
            else if (data.minerId != 0)
            {
                c.entityType = EntityType.miner;
                c.entityId = data.minerId;
            }
            else if (data.inserterId != 0)
            {
                c.entityType = EntityType.inserter;
                c.entityId = data.inserterId;
            }
            else if (data.assemblerId != 0)
            {
                c.entityType = EntityType.assembler;
                c.entityId = data.assemblerId;
            }
            else if (data.fractionateId != 0)
            {
                c.entityType = EntityType.fractionate;
                c.entityId = data.fractionateId;
            }
            else if (data.ejectorId != 0)
            {
                c.entityType = EntityType.ejector;
                c.entityId = data.ejectorId;
            }
            else if (data.siloId != 0)
            {
                c.entityType = EntityType.silo;
                c.entityId = data.siloId;
            }
            else if (data.labId != 0)
            {
                c.entityType = EntityType.lab;
                c.entityId = data.labId;
            }
            else if (data.stationId != 0)
            {
                c.entityType = EntityType.station;
                c.entityId = data.stationId;
            }
            else if (data.monsterId != 0)
            {
                c.entityType = EntityType.monster;
                c.entityId = data.monsterId;
            }
            else
            {
                c.entityType = EntityType.none;
            }

            c.powerNoderId = data.powerNodeId;

            if (data.powerGenId != 0)
            {
                c.powerType = EntityPowerType.generator;
                c.powerId = data.powerGenId;
            }
            else if (data.powerConId != 0)
            {
                c.powerType = EntityPowerType.consumer;
                c.powerId = data.powerConId;
            }
            else if (data.powerAccId != 0)
            {
                c.powerType = EntityPowerType.accumulator;
                c.powerId = data.powerAccId;
            }
            else if (data.powerExcId != 0)
            {
                c.powerType = EntityPowerType.exc;
                c.powerId = data.powerExcId;
            }
            else
            {
                c.powerType = EntityPowerType.none;
            }

            c.modelId = data.modelId;
            c.mmblockId = data.mmblockId;
            c.colliderId = data.colliderId;
            c.audioId = data.audioId;

            sw.Stop();
            Log.Info($"\t Manual compression time{sw.Elapsed.TotalMilliseconds}");
            return c;
        }
        public static EntityData Decompress(CompressedEntityData data)
        {
            //TODO
            return new EntityData();
        }
    }
    public enum EntityType : byte
    {
        none,
        belt,
        splitter,
        storage,
        tank,
        miner,
        inserter,
        assembler,
        fractionate,
        ejector,
        silo,
        lab,
        station,
        monster
    }
    public enum EntityPowerType : byte
    {
        none,
        generator,
        consumer,
        accumulator,
        exc
    }
}
