using Game.Interaction;

namespace Game.Logistics
{
    public class Parcel
    {
        private DeliveryTarget _target;

        public Parcel(DeliveryTarget target)
        {
            _target = target;
            target.AllocateParcel();
        }
    }
}