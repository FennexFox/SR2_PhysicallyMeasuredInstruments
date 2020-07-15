namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using Assets.Scripts.Craft.Parts;
    using UnityEngine;

    public class ElectroMagnetColliderScript : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PartScript>() != null)
            {
                ElectroMagnetScript thisModifier = GetComponentInParent<PartScript>().GetModifier<ElectroMagnetScript>();
                ElectroMagnetScript thatModifier = other.GetComponentInParent<PartScript>().GetModifier<ElectroMagnetScript>();
                if (thatModifier != null) {thatModifier.NearbyMagnets.Add(thisModifier.GetInstanceID(), thisModifier);}
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.GetComponentInParent<PartScript>() != null)
            {
                ElectroMagnetScript thisModifier = GetComponentInParent<PartScript>().GetModifier<ElectroMagnetScript>();
                ElectroMagnetScript thatModifier = other.GetComponentInParent<PartScript>().GetModifier<ElectroMagnetScript>();
                if (thatModifier != null && !thatModifier.NearbyMagnets.ContainsKey(thisModifier.GetInstanceID()))
                {
                    thatModifier.NearbyMagnets.Add(thisModifier.GetInstanceID(), thisModifier);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<PartScript>() != null)
            {
                ElectroMagnetScript thisModifier = GetComponentInParent<PartScript>().GetModifier<ElectroMagnetScript>();
                ElectroMagnetScript thatModifier = other.GetComponentInParent<PartScript>().GetModifier<ElectroMagnetScript>();
                if (thatModifier != null) {thatModifier.NearbyMagnets.Remove(thisModifier.GetInstanceID());}
            }
        }
    }
}