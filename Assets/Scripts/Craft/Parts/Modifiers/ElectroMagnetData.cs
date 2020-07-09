namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using ModApi.Craft.Parts;
    using ModApi.Craft.Parts.Attributes;
    using ModApi.Design.PartProperties;
    using System;
    using UnityEngine;

    [Serializable]
    [DesignerPartModifier("ElectroMagnet")]
    [PartModifierTypeId("InternationalDockingSystemStandard.ElectroMagnet")]
    public class ElectroMagnetData : PartModifierData<ElectroMagnetScript>
    {
        [SerializeField]
		[DesignerPropertySlider(0.01f, 1f, 100, Label = "Magnetic Force", Order = 1, Tooltip = "Magnetic Force of the electro magnet at 1m distance.")]
		private float _magneticForce = 0.01f;

        public float MagneticForce => _magneticForce;

		protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
		{
			d.OnValueLabelRequested(() => _magneticForce, (float x) => x.ToString("0.00") + "N");
            d.OnAnyPropertyChanged (() => Script.UpdateForce ());
        }


    }
}