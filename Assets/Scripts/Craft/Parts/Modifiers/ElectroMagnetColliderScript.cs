namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using Assets.Scripts.Craft.Parts;
    using UnityEngine;

    public class ElectroMagnetColliderScript : MonoBehaviour
    {
        private void OnTriggerStay(Collider other)
        {
            ElectroMagnetScript thisModifier = GetComponentInParent<PartScript>().GetModifier<ElectroMagnetScript>();
            ElectroMagnetScript thatModifier = other.GetComponentInParent<PartScript>().GetModifier<ElectroMagnetScript>();
            if (thatModifier != null && thisModifier.PartScript.Data.Activated)
            {
                ElectroMagnetScript previousOne = thisModifier.OtherElectroMagnet;
                if (thatModifier != null && previousOne != null && thatModifier.GetInstanceID() != previousOne.GetInstanceID())
                {
                    float distance = Vector3.SqrMagnitude(gameObject.transform.position - thatModifier.gameObject.transform.position);
                    float previousOneDistance = Vector3.SqrMagnitude(gameObject.transform.position - previousOne.gameObject.transform.position);
                    if (distance < previousOneDistance) {thisModifier.DestroyMagneticJoint();}
                }
                if (previousOne == null && thisModifier.MagneticJoint == null) {thisModifier.OnTouchDockingPort(thatModifier);}
            }
        }
    }
}