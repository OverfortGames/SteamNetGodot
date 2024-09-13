using Godot;

namespace OverfortGames.SteamNetGodot
{
    public partial class UISliderChangeBusVolume : HSlider
    {
        [Export]
        public string bus;

        [Export]
        public Label percentageLabel;

        private bool isDragging;

        private int busIndex;

        public override void _Ready()
        {
            base._Ready();

            DragStarted += OnDragStarted;
            DragEnded += OnDragEnded;

            busIndex = AudioServer.GetBusIndex(bus);
        }

        private void OnDragEnded(bool valueChanged)
        {
            isDragging = false;

            AudioManager.Instance.Save(busIndex, Value);
        }

        private void OnDragStarted()
        {
            isDragging = true;
        }

        public override void _Process(double delta)
        {
            base._Process(delta);

            if (isDragging)
            {
                AudioServer.SetBusVolumeDb(busIndex, Mathf.LinearToDb((float)Value));
            }
            else
            {
                var linear = Mathf.DbToLinear(AudioServer.GetBusVolumeDb(busIndex));
                Value = linear;
            }

            percentageLabel.Text = (int)(Value * 100) + "%";
        }
    }
}