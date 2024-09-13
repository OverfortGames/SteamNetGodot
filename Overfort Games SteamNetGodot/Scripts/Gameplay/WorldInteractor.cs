using Godot;

namespace OverfortGames.SteamNetGodot
{
    public partial class WorldInteractor : Node3D
    {
        [Export]
        private Pawn pawn;

        [Export]
        private RayCast3D raycast3D;

        [Export]
        private float rayLenght = 5f;

        [Export]
        private Node3D interactObjectAnchor;

        public Interactable HoverInteractable { get; private set; }

        public override void _Ready()
        {
            base._Ready();

            if (pawn.HasOwnership() == false)
            {
                SetProcess(false);
                SetProcessInput(false);
                SetPhysicsProcess(false);
                return;
            }

            raycast3D.Enabled = true;
            raycast3D.TargetPosition = new Vector3(0, 0, rayLenght);
        }

        public override void _Input(InputEvent @event)
        {
            base._Input(@event);

            if (@event is InputEventKey eventKey)
            {
                if (eventKey.IsPressed())
                {
                    if (eventKey.Keycode == Key.O)
                    {
                        if (NetworkManager.Instance.IsServer())
                        {
                            var coin = NetworkManager.Instance.GetGameState().Server_CreateObjectStateAndPawn(ObjectState.GetRandomNetworkId(), ResourceId.PawnApple, "", GameServer.SteamId, false);
                            var coinPawn = coin.GetPawn();

                            coinPawn.GlobalPosition = GlobalPosition + GlobalBasis.Z * 1;
                        }
                    }

                }
            }
        }

        public override void _Process(double delta)
        {
            if (Input.IsActionJustPressed(InputSettings.INTERACT_ACTION))
            {
                if (HoverInteractable != null)
                    HoverInteractable.TryInteract(this);
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            HoverInteractable = null;

            if (raycast3D.IsColliding())
            {
                InteractableCollider collider = raycast3D.GetCollider() as InteractableCollider;

                if (collider != null)
                {
                    HoverInteractable = collider.GetInteractable();
                }
            }
        }

        public Vector3 GetInteractObjectAnchor(Vector3 offset)
        {
            return interactObjectAnchor.GlobalPosition + (GlobalBasis.X * offset.X) + (GlobalBasis.Y * offset.Y) + (GlobalBasis.Z * offset.Z);
        }
    }

}
