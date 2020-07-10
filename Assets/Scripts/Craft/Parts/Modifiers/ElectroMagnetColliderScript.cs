namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using Assets.Scripts.Craft.Parts;
    using UnityEngine;

    public class ElectroMagnetColliderScript : MonoBehaviour
    {
        private int PreviousOne;

        private void OnTriggerEnter(Collider other)
        {
            PartScript componentInParent = other.GetComponentInParent<PartScript>();
            if (componentInParent != null)
            {
                ElectroMagnetScript modifier = GetComponentInParent<PartScript>().GetModifier<ElectroMagnetScript>();
                ElectroMagnetScript modifier2 = componentInParent.GetModifier<ElectroMagnetScript>();
                if (modifier2 != null)
                {
                    modifier.OnTouchDockingPort(modifier2);
                }
            }
        }

        private void OnTriggerStay(Collider other)
        {
            PartScript componentInParent = other.GetComponentInParent<PartScript>();
            if (other.GetInstanceID() == PreviousOne) {return;}
            else if (componentInParent != null)
            {
                ElectroMagnetScript modifier = GetComponentInParent<PartScript>().GetModifier<ElectroMagnetScript>();
                ElectroMagnetScript modifier2 = componentInParent.GetModifier<ElectroMagnetScript>();
                if (modifier2 != null) {modifier.OnTouchDockingPort(modifier2);}
            }
            PreviousOne = other.GetInstanceID();
        }

        /*private void OnTriggerExit(Collider other)
        {
            PartScript componentInParent = other.GetComponentInParent<PartScript>();
            if (componentInParent != null)
            {
                ElectroMagnetScript modifier = GetComponentInParent<PartScript>().GetModifier<ElectroMagnetScript>();
                ElectroMagnetScript modifier2 = componentInParent.GetModifier<ElectroMagnetScript>();
                if (modifier2 != null)
                {
                    modifier.OtherElectroMagnet = null;
                }
            }
        }*/
    }
}