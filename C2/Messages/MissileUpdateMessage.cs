using CommunityToolkit.Mvvm.Messaging.Messages;

namespace C2.Messages
{
    public class MissileUpdateMessage : ValueChangedMessage<MissileUpdateData>
    {
        public MissileUpdateMessage(MissileUpdateData value) : base(value) { }
    }

    public class MissileUpdateData
    {
        public string Id { get; }
        public double Latitude { get; }
        public double Longitude { get; }

        public MissileUpdateData(string id, double lat, double lon)
        {
            Id = id;
            Latitude = lat;
            Longitude = lon;
        }
    }
}
