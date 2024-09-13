using Godot;

namespace OverfortGames.SteamNetGodot
{
    public partial class PlayerMovement : CharacterBody3D
    {
        [Export] Pawn pawn;
        [Export] public CanvasLayer crosshair;
        [Export] public float WALK_SPEED = 5.0f;
        [Export] public float SPRINT_SPEED = 8.0f;
        [Export] public float JUMP_VELOCITY = 4.8f;
        [Export] public float SENSITIVITY = 0.004f;
        [Export] public float BOB_FREQ = 2.4f;
        [Export] public float BOB_AMP = 0.08f;
        [Export] public float BASE_FOV = 75.0f;
        [Export] public float FOV_CHANGE = 1.5f;

        private float speed;
        private float t_bob = 0.0f;
        private const float gravity = 9.8f;

        [Export]
        public Node3D head;
        private Camera3D camera;

        private Transform3D cameraTransform;

        private bool enabled = true;

        public override void _Ready()
        {
            crosshair.Visible = false;
            camera = head.GetNode<Camera3D>("Camera3D");

            if (pawn.HasOwnership())
            {
                Input.MouseMode = Input.MouseModeEnum.Captured;
                cameraTransform = camera.Transform;
                crosshair.Visible = true;

                ToggleGameMenu.OnToggleGameMenu += OnToggleGameMenu;
            }

            camera.Current = pawn.HasOwnership();
        }

        public override void _ExitTree()
        {
            if (pawn.HasOwnership())
            {
                ToggleGameMenu.OnToggleGameMenu -= OnToggleGameMenu;
                Input.MouseMode = Input.MouseModeEnum.Visible;
            }
        }

        private void OnToggleGameMenu(bool toggled)
        {
            if (pawn.HasOwnership() == false)
                return;

            SetEnabled(!toggled);
        }

        private void SetEnabled(bool newEnabled)
        {
            enabled = newEnabled;

            if (enabled)
            {
                Input.MouseMode = Input.MouseModeEnum.Captured;
                crosshair.Visible = true;
            }
            else
            {
                Input.MouseMode = Input.MouseModeEnum.Visible;
                crosshair.Visible = false;
            }
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (!enabled || pawn.HasOwnership() == false)
                return;

            if (@event is InputEventMouseMotion mouseEvent)
            {
                head.RotateY(-mouseEvent.Relative.X * SENSITIVITY);
                camera.RotateX(-mouseEvent.Relative.Y * SENSITIVITY);
                var cameraRotation = camera.Rotation;
                cameraRotation.X = Mathf.Clamp(cameraRotation.X, Mathf.DegToRad(-80), Mathf.DegToRad(80));
                camera.Rotation = cameraRotation;
            }
        }

        public override void _Process(double delta)
        {
            if (pawn.HasOwnership() == false)
            {
                if (camera.Current == true)
                    camera.Current = false;
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            if (pawn.HasOwnership() == false)
            {
                return;
            }

            float deltaFloat = (float)delta;

            Vector3 newVelocity = Velocity;

            // Add the gravity.
            if (!IsOnFloor())
            {
                newVelocity.Y -= gravity * deltaFloat;
            }

            // Handle Jump.
            if (Input.IsActionJustPressed("jump") && IsOnFloor())
            {
                newVelocity.Y = JUMP_VELOCITY;
            }

            // Handle Sprint.
            if (Input.IsActionPressed("sprint"))
            {
                speed = SPRINT_SPEED;
            }
            else
            {
                speed = WALK_SPEED;
            }

            if (enabled)
            {

                // Get the input direction and handle the movement/deceleration.
                var inputDir = Input.GetVector("left", "right", "up", "down");
                var direction = (head.Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
                if (IsOnFloor())
                {
                    if (direction != Vector3.Zero)
                    {
                        newVelocity.X = direction.X * speed;
                        newVelocity.Z = direction.Z * speed;
                    }
                    else
                    {
                        newVelocity.X = Mathf.Lerp(newVelocity.X, direction.X * speed, deltaFloat * 7.0f);
                        newVelocity.Z = Mathf.Lerp(newVelocity.Z, direction.Z * speed, deltaFloat * 7.0f);
                    }
                }
                else
                {
                    newVelocity.X = Mathf.Lerp(newVelocity.X, direction.X * speed, deltaFloat * 3.0f);
                    newVelocity.Z = Mathf.Lerp(newVelocity.Z, direction.Z * speed, deltaFloat * 3.0f);
                }
            }
            else
            {
                newVelocity.X = 0;
                newVelocity.Z = 0;
            }

            Velocity = newVelocity;

            // Head bob
            t_bob += deltaFloat * Velocity.Length() * (IsOnFloor() ? 1 : 0);
            cameraTransform.Origin = _Headbob(t_bob);

            // FOV
            var velocityClamped = Mathf.Clamp(Velocity.Length(), 0.5f, SPRINT_SPEED * 2);
            var targetFov = BASE_FOV + FOV_CHANGE * velocityClamped;
            camera.Fov = Mathf.Lerp(camera.Fov, targetFov, deltaFloat * 8.0f);

            MoveAndSlide();
        }

        private Vector3 _Headbob(float time)
        {
            var pos = Vector3.Zero;
            pos.Y = Mathf.Sin(time * BOB_FREQ) * BOB_AMP;
            pos.X = Mathf.Cos(time * BOB_FREQ / 2) * BOB_AMP;
            return pos;
        }
    }
}