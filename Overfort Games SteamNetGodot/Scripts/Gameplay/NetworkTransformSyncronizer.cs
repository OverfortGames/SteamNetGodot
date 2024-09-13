using Godot;
using Steamworks.Data;

namespace OverfortGames.SteamNetGodot
{
    public partial class NetworkTransformSyncronizer : Node
    {
        [Export]
        private uint internalId = 0;

        [Export]
        private RigidBody3D rigidbodyTarget;

        [Export]
        private Node3D positionTarget;

        [Export]
        private Node3D rotationTarget;

        [Export]
        private bool rotationUseOnlyForward;

        [Export]
        private Pawn pawn;

        [Export]
        private float interpolationSpeed = 15f;

        [Export]
        private float positionThreshold = 0.01f;

        [Export]
        private float rotationThreshold = 0.01f;

        private TransformPacket? lastReceivedTransformPacket;

        private Vector3 _previousPosition;
        private Vector3 _previousRotation;

        public override void _Ready()
        {
            NetworkManager.Instance.Server_SubscribeRPC<TransformPacket, Connection>(Server_OnTransformPacketReceived, () => this.IsValid() == false);
            NetworkManager.Instance.Server_SubscribeRPC<TransformRequestPacket, Connection>(Server_OnTransformRequestPacketReceived, () => this.IsValid() == false);
            NetworkManager.Instance.Client_SubscribeRPC<TransformPacket>(Client_OnTransformPacketReceived, () => this.IsValid() == false);
            NetworkManager.Instance.OnTick += OnTick;

            Client_SendTransformRequest();
        }

        public override void _Process(double delta)
        {
            if (pawn.HasOwnership() == false)
            {
                if (lastReceivedTransformPacket.HasValue)
                {
                    // Handle physics
                    if (rigidbodyTarget.IsValid())
                    {
                        var physicsFreeze = lastReceivedTransformPacket.Value.physicsFreeze;
                        var physicsLinearVelocity = lastReceivedTransformPacket.Value.physicsLinearVelocity;
                        var physicsAngularVelocity = lastReceivedTransformPacket.Value.physicsAngularVelocity;

                        rigidbodyTarget.Freeze = physicsFreeze;
                        rigidbodyTarget.LinearVelocity = physicsLinearVelocity;
                        rigidbodyTarget.AngularVelocity = physicsAngularVelocity;
                    }

                    // Handle parenting
                    string packetParentPath = lastReceivedTransformPacket.Value.parentPath;
                    var packetParent = GetNode(packetParentPath);
                    if (packetParent != pawn.GetParent())
                        pawn.Reparent(packetParent);

                    // Handle position
                    if (positionTarget.IsValid())
                    {
                        Vector3 packetPosition = lastReceivedTransformPacket.Value.position;
                        positionTarget.GlobalPosition = positionTarget.GlobalPosition.Lerp(packetPosition, (float)delta * interpolationSpeed);
                    }

                    // Handle rotation
                    if (rotationTarget.IsValid())
                    {
                        Quaternion packetRotation = lastReceivedTransformPacket.Value.rotation;

                        if (rotationUseOnlyForward == false)
                        {
                            rotationTarget.Quaternion = rotationTarget.Quaternion.Slerp(packetRotation, (float)delta * interpolationSpeed);
                        }
                        else
                        {
                            SetGlobalForward(rotationTarget, lastReceivedTransformPacket.Value.forward);
                        }
                    }
                }
            }
        }

        private void OnTick(ulong tick)
        {
            // if ((tick % 2) == 0)
            {
                bool positionChanged = false;
                bool rotationChanged = false;

                Vector3 currentPosition = Vector3.Zero;
                Vector3 currentRotation = Vector3.Zero;

                if (positionTarget.IsValid())
                {
                    currentPosition = positionTarget.GlobalPosition;
                    positionChanged = currentPosition.DistanceTo(_previousPosition) > positionThreshold;
                }

                if (rotationTarget.IsValid())
                {
                    currentRotation = rotationTarget.GlobalRotation; // Get rotation as Euler angles
                    rotationChanged = currentRotation.DistanceTo(_previousRotation) > rotationThreshold;
                }

                // Check if the position or rotation has changed
                if (positionChanged || rotationChanged)
                {
                    // Server
                    if (NetworkManager.Instance.IsServer())
                    {
                        if (pawn.HasOwnership())
                        {
                            var transformPacket = CreateTransformPacket();
                            NetworkManager.Instance.Server.BroadcastExceptLocalhost(transformPacket, Steamworks.Data.SendType.Reliable);
                        }
                    }
                    else // Client
                    {
                        if (pawn.HasOwnership())
                        {
                            var transformPacket = CreateTransformPacket();
                            NetworkManager.Instance.Client.Send(transformPacket, Steamworks.Data.SendType.Reliable);
                        }
                    }

                    // Update previous values
                    _previousPosition = currentPosition;
                    _previousRotation = currentRotation;
                }
            }
        }

        private TransformPacket CreateTransformPacket()
        {
            TransformPacket transformPacket = new TransformPacket();
            transformPacket.networkId = pawn.networkId;
            transformPacket.internalId = internalId;
            transformPacket.parentPath = pawn.GetParent().GetPath();

            if (rigidbodyTarget.IsValid())
            {
                transformPacket.physicsFreeze = rigidbodyTarget.Freeze;
                transformPacket.physicsLinearVelocity = rigidbodyTarget.LinearVelocity;
                transformPacket.physicsAngularVelocity = rigidbodyTarget.AngularVelocity;
            }

            if (positionTarget.IsValid())
            {
                transformPacket.position = positionTarget.GlobalPosition;
            }

            if (rotationTarget.IsValid())
            {
                transformPacket.rotation = rotationTarget.Quaternion;
                transformPacket.forward = rotationTarget.GlobalBasis.Z;
            }

            return transformPacket;
        }

        public void SetGlobalForward(Node3D target, Vector3 targetDirection)
        {
            // Normalize the target direction to avoid scaling issues
            targetDirection = targetDirection.Normalized();

            // Create a new Basis where the Z-axis (forward) is aligned with the target direction
            Basis newGlobalBasis = target.GlobalBasis;
            newGlobalBasis.Z = targetDirection;

            // Optionally, adjust other axes to avoid skewing (can be omitted if you want default alignment)
            newGlobalBasis.X = newGlobalBasis.Z.Cross(Vector3.Up).Normalized();  // Create a new X-axis perpendicular to the forward direction
            newGlobalBasis.Y = newGlobalBasis.X.Cross(newGlobalBasis.Z).Normalized();  // Ensure Y-axis is perpendicular to both X and Z

            // Apply the new global basis to the object's global transform
            Transform3D globalTransform = target.GlobalTransform;
            globalTransform.Basis = newGlobalBasis;
            target.GlobalTransform = globalTransform;
        }

        #region CLIENT

        private void Client_SendTransformRequest()
        {
            if (NetworkManager.Instance.IsServer() == false)
            {
                if (NetworkManager.Instance.IsClientConnected())
                {
                    if (pawn.HasOwnership() == false)
                    {
                        var transformRequestPacket = new TransformRequestPacket();
                        transformRequestPacket.networkId = pawn.networkId;
                        transformRequestPacket.internalId = internalId;

                        NetworkManager.Instance.Client.Send(transformRequestPacket, SendType.Reliable);

                    }
                }
            }
        }

        // The client sending this TransformPacket data will not receive this RPC by design
        private void Client_OnTransformPacketReceived(TransformPacket packet)
        {
            if (packet.networkId != pawn.networkId || packet.internalId != internalId)
                return;
            lastReceivedTransformPacket = packet;
        }

        #endregion

        #region SERVER
        private void Server_OnTransformPacketReceived(TransformPacket packet, Connection from)
        {
            if (packet.networkId != pawn.networkId || packet.internalId != internalId)
                return;

            ulong fromSteamId = (ulong)from.UserData;

            if (pawn.networkOwner != fromSteamId)
                return;

            NetworkManager.Instance.Server.Broadcast(packet, Steamworks.Data.SendType.Reliable, from);
        }

        private void Server_OnTransformRequestPacketReceived(TransformRequestPacket packet, Connection connection)
        {
            if (pawn.networkId != packet.networkId || packet.internalId != internalId)
                return;

            var transformPacket = CreateTransformPacket();
            NetworkManager.Instance.Server.Send(transformPacket, connection, Steamworks.Data.SendType.Reliable);
        }

        #endregion
    }
}