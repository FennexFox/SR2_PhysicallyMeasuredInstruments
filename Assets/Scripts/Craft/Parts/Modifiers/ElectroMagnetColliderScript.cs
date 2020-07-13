namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using Assets.Scripts.Craft.Parts;
    using UnityEngine;

    public class ElectroMagnetColliderScript : MonoBehaviour
    {
        private ElectroMagnetScript previousOne;
        private float previousOneDistance;

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
            ElectroMagnetScript thisModifier = GetComponentInParent<PartScript>().GetModifier<ElectroMagnetScript>();
            ElectroMagnetScript thatModifier = other.GetComponentInParent<PartScript>().GetModifier<ElectroMagnetScript>();
            if (thatModifier != null && thisModifier.PartScript.Data.Activated)
            {
                previousOne = thisModifier.OtherElectroMagnet;
                if (thatModifier != null && previousOne != null && thatModifier.GetInstanceID() != previousOne.GetInstanceID())
                {
                    float distance = Vector3.SqrMagnitude(gameObject.transform.position - thatModifier.gameObject.transform.position);
                    float previousOneDistance = Vector3.SqrMagnitude(gameObject.transform.position - previousOne.gameObject.transform.position);
                    if (distance < previousOneDistance) {thisModifier.DestroyMagneticJoint(true);}
                }
                if (previousOne == null && thisModifier.MagneticJoint == null) {thisModifier.OnTouchDockingPort(thatModifier);}
            }
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