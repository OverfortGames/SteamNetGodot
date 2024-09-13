using Steamworks;
using Godot;

namespace OverfortGames.SteamNetGodot
{
    public partial class ShowHideWithVOIP : Node
    {
        [Export]
        private VOIP voip;

        [Export]
        private Node3D[] visualsVoipRecording;

        [Export]
        private Node3D[] visualsVoipHasVoice;

        [Export]
        private CanvasItem[] visualsVoipRecordingCanvasItem;

        [Export]
        private CanvasItem[] visualsVoipHasVoiceCanvasItem;


        public override void _Process(double delta)
        {
            bool voipIsRecording = voip.IsRecording;
            bool voipHasVoiceData = SteamUser.HasVoiceData;

            foreach (var visual in visualsVoipRecording)
            {
                visual.Visible = voipIsRecording;
            }

            foreach (var visualCanvasItem in visualsVoipRecordingCanvasItem)
            {
                visualCanvasItem.Visible = voipIsRecording;
            }

            foreach (var visual in visualsVoipHasVoice)
            {
                visual.Visible = voipHasVoiceData;
            }

            foreach (var visualCanvasItem in visualsVoipHasVoiceCanvasItem)
            {
                visualCanvasItem.Visible = voipHasVoiceData;
            }
        }
    }

}