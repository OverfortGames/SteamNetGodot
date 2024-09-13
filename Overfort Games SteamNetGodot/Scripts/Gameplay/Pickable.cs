using Godot;
using Steamworks.Data;
using System.Threading.Tasks;

namespace OverfortGames.SteamNetGodot
{
    public partial class Pickable : Interactable
    {
        public const Key THROWKEY = Key.T;

        public enum PickableState
        {
            NotPicked,
            Picked,
            Throwing
        }

        [Export]
        private Pawn pawn;

        [Export]
        private Node3D target;

        [Export]
        private RigidBody3D targetRigidbody;

        [Export]
        private float throwForSeconds = 0;

        [Export]
        public Vector3 anchorOffset = new Vector3(0, 0, 0);

        [Export]
        public Vector3 forceOffset;

        [Export]
        public Vector3 torque;

        [Export]
        public float forceStrenght = 5f;

        [Export]
        public float pickupPositionInterpolationSpeed = 15;

        private WorldInteractor lastWorldInteractor;

        private PickableState currentState;

        public override void _Ready()
        {
            base._Ready();

            NetworkManager.Instance.Server_SubscribeRPC<PickupRequestPacket, Connection>(Server_OnPickupRequestPacketReceived, () => this.IsValid() == false);
            NetworkManager.Instance.Server_SubscribeRPC<ThrowRequestPacket, Connection>(Server_OnThrowRequestPacketReceived, () => this.IsValid() == false);

            NetworkManager.Instance.Client_SubscribeRPC<PickableStatePacket>(Client_OnPickableStatePacketReceived, () => this.IsValid() == false);
        }

        public override void _Process(double delta)
        {
            if (pawn.HasOwnership() == false)
                return;

            if (currentState == PickableState.Picked && lastWorldInteractor.IsValid())
            {
                target.GlobalPosition = target.GlobalPosition.Lerp(lastWorldInteractor.GetInteractObjectAnchor(anchorOffset), pickupPositionInterpolationSpeed * (float)delta);

                //Get the forward direction of the target node(its +Z axis)
                Vector3 targetDirection = lastWorldInteractor.GlobalBasis.Z;

                // Calculate the target rotation to look at the target's forward direction
                Basis targetBasis = Basis.LookingAt(targetDirection, Vector3.Up);
                Quaternion targetRotation = new Quaternion(targetBasis);

                // Interpolate the current rotation towards the target rotation
                Quaternion currentRotation = target.GlobalBasis.GetRotationQuaternion();
                Quaternion interpolatedRotation = currentRotation.Slerp(targetRotation, pickupPositionInterpolationSpeed * (float)delta);

                // Apply the new rotation to the object
                target.GlobalTransform = new Transform3D(new Basis(interpolatedRotation), target.GlobalPosition);
            }

            if (Input.IsActionJustPressed(InputSettings.INTERACT_ACTION))
            {
                TryThrow();
            }
        }

        private void TryThrow()
        {
            if (currentState != PickableState.Picked)
                return;

            if (NetworkManager.Instance.IsServer())
            {
                Server_Throw(GameServer.SteamId);
            }
            else
            {
                Client_Throw();
            }
        }

        public override void TryInteract(WorldInteractor interactor)
        {
            if (NetworkManager.Instance.IsServer())
            {
                Server_Interact(interactor, GameServer.SteamId);
            }
            else
            {
                Client_TryInteract(interactor);
            }
        }

        public override string GetInteractionName()
        {
            switch (currentState)
            {
                case PickableState.NotPicked:
                    return $"Pickup {pawn.Name}";
                case PickableState.Picked:
                    return pawn.HasOwnership() ? "" : "Occupied";
                case PickableState.Throwing:
                    return "";
                default:
                    break;
            }

            return "";
        }

        #region SERVER

        private void Server_Interact(WorldInteractor interactor, ulong from)
        {
            if (currentState != PickableState.NotPicked)
            {
                return;
            }

            Server_Pickup(interactor, from);
            lastWorldInteractor = interactor;
        }

        private void Server_OnPickupRequestPacketReceived(PickupRequestPacket packet, Connection from)
        {
            if (pawn.networkId != packet.networkId)
                return;

            var worldInteractorAsNode = GetNode(packet.interactorPath);

            if (worldInteractorAsNode == null)
            {
                GD.Print($"Received {typeof(PickupRequestPacket).Name} but can't find worldInteractor node with path {packet.interactorPath}");
                return;
            }

            var worldInteractor = worldInteractorAsNode as WorldInteractor;

            if (worldInteractorAsNode == null)
            {
                GD.Print($"Received {typeof(PickupRequestPacket).Name} but can't find worldInteractor node can't be casted to {typeof(WorldInteractor).Name}");
                return;
            }

            Server_Interact(worldInteractor, (ulong)from.UserData);
        }

        private void Server_OnThrowRequestPacketReceived(ThrowRequestPacket packet, Connection from)
        {
            if (pawn.networkId != packet.networkId)
                return;

            Server_Throw((ulong)from.UserData);
        }

        private async void Server_Throw(ulong from)
        {
            if (from != GameServer.SteamId)
            {
                NetworkManager.Instance.GetGameState().SetOwnership(pawn.networkId, GameServer.SteamId);
            }

            Server_SetState(PickableState.Throwing);

            pawn.Reparent(SceneLoader.Instance.GameplayScene);

            if (targetRigidbody.IsValid())
            {
                targetRigidbody.Freeze = false;

                Vector3 force = lastWorldInteractor.GlobalBasis.Z;
                force += Vector3.Up * forceOffset.Y;
                force += Vector3.Right * forceOffset.X;
                force += Vector3.Forward * forceOffset.Z;

                force *= forceStrenght;

                targetRigidbody.ApplyCentralImpulse(force);
                targetRigidbody.ApplyTorque(torque);
            }

            await Task.Delay((int)(throwForSeconds * 1000));

            Server_SetState(PickableState.NotPicked);
        }

        private void Server_Pickup(Node newParent, ulong from)
        {
            if (currentState != PickableState.NotPicked)
                return;

            Server_SetState(PickableState.Picked);

            if (targetRigidbody.IsValid())
            {
                targetRigidbody.LinearVelocity = Vector3.Zero;
                targetRigidbody.AngularVelocity = Vector3.Zero;
                targetRigidbody.Freeze = true;
            }

            pawn.Reparent(newParent);

            if (pawn.networkOwner != from)
            {
                // Delay because of transform interpolation time
                NetworkManager.Instance.GetGameState().SetOwnership(pawn.networkId, from, 0.1f);
            }
        }

        private void Server_SetState(PickableState newState)
        {
            currentState = newState;

            PickableStatePacket packet = new PickableStatePacket();
            packet.networkId = pawn.networkId;
            packet.state = currentState;
            NetworkManager.Instance.Server.BroadcastExceptLocalhost(packet, Steamworks.Data.SendType.Reliable);
        }

        #endregion

        #region CLIENT

        private void Client_TryInteract(WorldInteractor interactor)
        {
            lastWorldInteractor = interactor;

            var pickupPacket = new PickupRequestPacket();
            pickupPacket.interactorPath = interactor.GetPath();
            pickupPacket.networkId = pawn.networkId;

            NetworkManager.Instance.Client.Send(pickupPacket, Steamworks.Data.SendType.Reliable);
        }

        private void Client_OnPickableStatePacketReceived(PickableStatePacket packet)
        {
            if (pawn.networkId != packet.networkId)
                return;

            currentState = packet.state;
        }

        private void Client_Throw()
        {
            NetworkManager.Instance.Client.Send(new ThrowRequestPacket() { networkId = pawn.networkId }, Steamworks.Data.SendType.Reliable);
        }

        #endregion
    }
}