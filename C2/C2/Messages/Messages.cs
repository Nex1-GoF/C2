using CommunityToolkit.Mvvm.Messaging.Messages;

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
}