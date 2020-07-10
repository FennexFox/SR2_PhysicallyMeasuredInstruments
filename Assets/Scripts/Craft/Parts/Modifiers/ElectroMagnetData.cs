namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using Assets.Scripts.Design;
    using ModApi;
    using ModApi.Craft.Parts;
    using ModApi.Craft.Parts.Attributes;
    using ModApi.Design.PartProperties;
    using ModApi.Math;
    using System;
    using UnityEngine;

    [Serializable]
    [DesignerPartModifier("ElectroMagnet")]
    [PartModifierTypeId("InternationalDockingSystemStandard.ElectroMagnet")]
    public class ElectroMagnetData : PartModifierData<ElectroMagnetScript>
    {
        [SerializeField]
		[DesignerPropertySlider(80000f, 240000f, 101, Label = "Magnetic Force", Order = 1, Tooltip = "Magnetic Force of the electro magnet at its surface(Diameter * 0.125m).")]
		private float _magneticForce = 160000f;

        [SerializeField]
		[DesignerPropertySlider(0.05f, 2f, 40, Label = "Diameter", Order = 2, Tooltip = "Changes the size of the magnet.")]
		private float _size = 1f;

        public float MagneticForce
        {
            get
            {
                return _magneticForce;
            }
            private set
            {
                _magneticForce = value;
                base.Script.UpdateForce();
            }
        }

        public float Diameter
        {
            get
            {
                return _size;
            }
            private set
            {
                _size = value;
                base.Script.UpdateSize();
            }
        }

        ISliderProperty forceSlider;
        public override float Mass => 100f * Diameter * Diameter * Diameter * 0.01f;
        public float minVal;
        public float maxVal;

		protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
		{
            minVal = 80000f * Diameter * Diameter;
            maxVal = 240000f * Diameter * Diameter;

			d.OnValueLabelRequested(() => _magneticForce, (float x) => Units.GetForceString(x));
            d.OnValueLabelRequested(() => _size, (float x) => x.ToString("F"));
            d.OnPropertyChanged(() => _magneticForce, (x, y) => {Script.UpdateForce();});
            d.OnPropertyChanged
            (
                () => _size,
                (x, y) =>
                {
                    Script.UpdateSize();
                    minVal = 80000f * x * x;
                    maxVal = 240000f * x * x;
                    forceSlider.UpdateSliderSettings(minVal, maxVal, 101);
                    MagneticForce = Mathf.Clamp(MagneticForce, minVal, maxVal);
                }
            );
            d.OnAnyPropertyChanged(() => DesignerPropertyChagned());
            d.OnSliderActivated(() => _magneticForce, (ISliderProperty x) => {x.UpdateSliderSettings(minVal, maxVal, 101); forceSlider = x;});
        }

        private void DesignerPropertyChagned()
        {
            Symmetry.SynchronizePartModifiers(base.Part.PartScript);
			base.Script.PartScript.CraftScript.RaiseDesignerCraftStructureChangedEvent();
        }
    }
}