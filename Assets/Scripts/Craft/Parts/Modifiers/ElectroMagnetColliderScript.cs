namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using Assets.Scripts.Craft.Parts;
    using UnityEngine;

    public class ElectroMagnetColliderScript : MonoBehaviour
    {
        private int PreviousOne;
        private float PreviousOneDistance;

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
            PartScript thisComponent = GetComponentInParent<PartScript>();
            PartScript thatComponent = other.GetComponentInParent<PartScript>();
            if (thatComponent != null && thisComponent.Data.Activated)
            {
                ElectroMagnetScript thatModifier = thatComponent.GetModifier<ElectroMagnetScript>();
                if (thatModifier != null)
                {
                    float distance = Vector3.SqrMagnitude(gameObject.transform.position - thatModifier.gameObject.transform.position);
                    if (distance <= PreviousOneDistance || PreviousOneDistance == 0f)
                    {
                        ElectroMagnetScript thisModifier = GetComponentInParent<PartScript>().GetModifier<ElectroMagnetScript>();
                        if (thatModifier != null)
                        {
                            thisModifier.OnTouchDockingPort(thatModifier);
                        }
                        PreviousOneDistance = distance;
                    }
                }
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