﻿using NebulaModel.DataStructures;
using NebulaModel.Packets.Players;
using NebulaModel.Utils;
using NebulaWorld.MonoBehaviours.Local;
using UnityEngine;

namespace NebulaWorld.MonoBehaviours.Remote
{
    public class RemotePlayerMovement : MonoBehaviour
    {
        private const int BUFFERED_SNAPSHOT_COUNT = 4;

        struct Snapshot
        {
            public long Timestamp { get; set; }
            public Vector3 Position { get; set; }
            public Quaternion Rotation { get; set; }
            public Quaternion BodyRotation { get; set; }
        }

        private Transform rootTransform;
        private Transform bodyTransform;

        // To have a smooth transition between position updates, we keep a buffer of states received 
        // and once the buffer is full, we start replaying the states from the oldest to the newest state.
        // This will make sure player movement is still smooth in high latency cases and even if there are dropped packets.
        private readonly Snapshot[] snapshotBuffer = new Snapshot[BUFFERED_SNAPSHOT_COUNT];

        private void Awake()
        {
            rootTransform = GetComponent<Transform>();
            bodyTransform = rootTransform.Find("Model");
        }

        private void Update()
        {
            // Wait for the entire buffer to be full before starting to interpolate the player position
            if (snapshotBuffer[0].Timestamp == 0)
                return;

            double past = (1000 / (double)LocalPlayerMovement.SEND_RATE) * (snapshotBuffer.Length - 1);
            double now = TimeUtils.CurrentUnixTimestampMilliseconds();
            double renderTime = now - past;

            for (int i = 0; i < snapshotBuffer.Length - 1; ++i)
            {
                var t1 = snapshotBuffer[i].Timestamp;
                var t2 = snapshotBuffer[i + 1].Timestamp;

                if (renderTime <= t2 && renderTime >= t1)
                {
                    var total = t2 - t1;
                    var reminder = renderTime - t1;
                    var ratio = total > 0 ? reminder / total : 1;

                    // We interpolate to the appropriate position between our 2 known snapshot
                    MoveInterpolated(snapshotBuffer[i], snapshotBuffer[i + 1], (float)ratio);
                    break;
                }
                else if (i == snapshotBuffer.Length - 2 && renderTime > t2)
                {
                    // This will skip interpolation and will snap to the most recent position.
                    MoveInterpolated(snapshotBuffer[i], snapshotBuffer[i + 1], 1);
                }
            }
        }

        public void UpdatePosition(PlayerMovement movement)
        {
            if (!rootTransform)
                return;

            for (int i = 0; i < snapshotBuffer.Length - 1; ++i)
            {
                snapshotBuffer[i] = snapshotBuffer[i + 1];
            }

            snapshotBuffer[snapshotBuffer.Length - 1] = new Snapshot()
            {
                Timestamp = TimeUtils.CurrentUnixTimestampMilliseconds(),
                Position = movement.Position.ToUnity(),
                Rotation = Quaternion.Euler(movement.Rotation.ToUnity()),
                BodyRotation = Quaternion.Euler(movement.BodyRotation.ToUnity()),
            };
        }

        private void MoveInterpolated(Snapshot previous, Snapshot current, float ratio)
        {
            rootTransform.position = Vector3.Lerp(previous.Position, current.Position, ratio);
            rootTransform.rotation = Quaternion.Slerp(previous.Rotation, current.Rotation, ratio);
            bodyTransform.rotation = Quaternion.Slerp(previous.BodyRotation, current.BodyRotation, ratio);
        }
    }
}
