namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using Assets.Scripts.Design;
    using ModApi;
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
		[DesignerPropertySlider(0.01f, 2f, 200, Label = "Magnetic Force", Order = 1, Tooltip = "Magnetic Force of the electro magnet at its surface.")]
		private float _magneticForce = 0.01f;

        [SerializeField]
		[DesignerPropertySlider(0.05f, 2f, 40, Label = "Size", Order = 2, Tooltip = "Changes the size of the magnet.")]
		private float _size = 1f;

        public float MagneticForce => _magneticForce;

        public float Size => _size;

		protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
		{
			d.OnValueLabelRequested(() => _magneticForce, (float x) => x.ToString("0.00") + "N");
            d.OnValueLabelRequested(() => _size, (float x) => Utilities.FormatPercentage(x));
            d.OnPropertyChanged(() => _magneticForce, delegate
            {
                Script.UpdateForce();
            });
            d.OnPropertyChanged(() => _size, delegate
            {
                Script.UpdateSize();
            });
            d.OnAnyPropertyChanged(() => Symmetry.SynchronizePartModifiers(base.Part.PartScript));
        }


    }
}