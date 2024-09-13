using Godot;

namespace OverfortGames.SteamNetGodot
{
    public partial class ScaleOnRMS : Node3D
    {
        [Export]
        private VOIP voip;

        [Export]
        private Vector3 minScale;

        [Export]
        private Vector3 maxScale;

        [Export]
        private float smoothingSpeed = 1f;

        private float currentRMSValue;

        public override void _Process(double delta)
        {
            base._Process(delta);

            // Smoothly damp the current RMS value towards the target value (voip.CurrentRMS)
            currentRMSValue = Mathf.Lerp(currentRMSValue, voip.CurrentRMS, smoothingSpeed * (float)delta);

            // Interpolate between minScale and maxScale using the smoothed RMS value
            Scale = minScale.Lerp(maxScale, currentRMSValue * 4);
        }
    }
}