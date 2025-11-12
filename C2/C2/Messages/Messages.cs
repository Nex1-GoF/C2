using CommunityToolkit.Mvvm.Messaging.Messages;
using Newtonsoft.Json.Linq;
using C2.Models;

namespace C2.Messages
{
    // SelectTarget(true) / ClearTarget(false)
    public class TargetSelectedMessage : ValueChangedMessage<char?>
    {
        public TargetSelectedMessage(char? value) : base(value) { }
    }

    public class MissileSelectedMessage : ValueChangedMessage<string?>
    {
        public MissileSelectedMessage(string? value) : base(value) { }
    }
    public class ButtonDeactivateMessage : ValueChangedMessage<bool>
    {
        public ButtonDeactivateMessage(bool value) : base(value) { }
    }
    public class LaunchEndMessage : ValueChangedMessage<bool>
    {
        public LaunchEndMessage(bool value) : base(value) { }
    }

    public class EngagementAssignedMessage : ValueChangedMessage<(string MissileId, string TargetId)>
    {
        public EngagementAssignedMessage(string missileId, string targetId)
            : base((missileId, targetId)) { }
    }

    public class PipCalculatedMessage : ValueChangedMessage<(string MissileId, double Lat, double Lon, short Alt)>
    {
        public PipCalculatedMessage(string missileId, double lat, double lon, short alt)
            : base((missileId, lat, lon, alt)) { }
    }
    public class TargetCreatedMessage : ValueChangedMessage<Target target>
    {
        public TargetCreatedMessage(char targetId) : base(targetId) { }
    }



    public class LaunchProgressMessage
    {
        public double Progress { get; }
        public bool IsIrreversible { get; }

        public LaunchProgressMessage(
            double progress,
            bool isIrreversible = false
           )
        {
            Progress = progress;
            IsIrreversible = isIrreversible;
        }
    }
}