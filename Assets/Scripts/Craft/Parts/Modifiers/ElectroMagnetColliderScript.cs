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
            if (thatModifier != null && thisModifier.PartScript.Data.Activated) // select the closest one for latch lock
            {
                ElectroMagnetScript previousOne = thisModifier.OtherElectroMagnet;
                float distanceSQR = Vector3.SqrMagnitude(gameObject.transform.position - thatModifier.gameObject.transform.position);
                if (previousOne != null && thatModifier.GetInstanceID() != previousOne.GetInstanceID())
                {
                    float previousOneDistanceSQR = Vector3.SqrMagnitude(gameObject.transform.position - previousOne.gameObject.transform.position);
                    if (distanceSQR < previousOneDistanceSQR) {thisModifier.DestroyMagneticJoint();}
                }
                
                if (previousOne == null && thisModifier.MagneticJoint == null)
                {
                    thisModifier.OnTouchDockingPort(thatModifier);
                }
                else if (previousOne != thatModifier)// N-body Magnetism for the others
                {
                    thisModifier.Magnetism(thatModifier, distanceSQR);
                }
            }
        }
    }
}