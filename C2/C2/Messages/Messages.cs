using CommunityToolkit.Mvvm.Messaging.Messages;
using Newtonsoft.Json.Linq;
using System.Security.Policy;

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
    public class MissileLaunchMessage
    {
        public string Value {  get; set; }
        public MissileLaunchMessage(string value)
        {
            Value = value;
        }
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
    public class TargetCreatedMessage : ValueChangedMessage<char>
    {
        public TargetCreatedMessage(char id) : base(id) { }
    }

    public sealed class TargetRemovedMessage : ValueChangedMessage<char>
    {
        public TargetRemovedMessage(char removedId) : base(removedId) { }
    }
}