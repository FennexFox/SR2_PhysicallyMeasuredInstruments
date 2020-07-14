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
/*
        [SerializeField]
		[DesignerPropertySlider(800f, 2400f, 101, Label = "Magnetic Force", Order = 1, Tooltip = "Magnetic Force of the electro magnet at its surface(Diameter * 0.125m).")]
		private float _magneticPoleStrength = 1600f; // SR2 Force is 100 times stronger than real life, 1600f means 160kN
*/
        [SerializeField]
		[DesignerPropertySlider(0.05f, 2f, 40, Label = "Diameter", Order = 0, Tooltip = "Changes the size of the magnet.")]
		private float _size = 1f;

        [SerializeField]
		[DesignerPropertySlider(12f, 240f, 20, Label = "Max Ampere", Order = 1, Tooltip = "Changes the max electric current you can put into the magnet.")]
		private float _maxAmpere = 12f;

        [SerializeField]
		[DesignerPropertySlider(0.05f, 2f, 40, Label = "Turn Per Length", Order = 2, Tooltip = "Changes the number of turns of the coil per length.")]
		private float _turnPerLength = 1f;

        [SerializeField]
		[DesignerPropertySlider(0.05f, 2f, 40, Label = "Latch Diameter", Order = 5, Tooltip = "Changes the size of the locking mehchanism.")]
		private float _latchSize = 1f;

        private double _area => Math.PI * Diameter * Diameter / 4f;

/*
        private double _coilResistance => (_coilResistivity * _turnPerLength / _coilCrossSection) * (_size * 0.25f) * (Math.PI * _size);

        private float _coilResistivity = 0.0000000168f; // from "shorturl.at/vxV18"

        private float _coilCrossSection = 0.0000002f; // from "shorturl.at/vxV18"
*/
        public float Volt;

        public float MaxMagneticPoleStrength => Convert.ToSingle(_area) * MaxAmpere * TurnPerLength;

        public float Diameter {get{return _size;} private set{_size = value; base.Script.UpdateSize();}}

        public float Volume => 0.19635f * Diameter * Diameter * Diameter;

        public float MaxAmpere => _maxAmpere;

        public float TurnPerLength => _turnPerLength;

        public float LatchSize {get{return _latchSize;} private set{_latchSize = value; base.Script.UpdateSize();}}

        ISliderProperty forceSlider;

        public override float Mass => (8000f * Diameter * Diameter * Diameter + 25f * LatchSize * LatchSize * LatchSize) * 0.01f; // SR2 mass is 100 times lighter

		protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
		{
            d.OnValueLabelRequested(() => _size, (float x) => $"{x.ToString("F")}m");
            d.OnValueLabelRequested(() => _latchSize, (float x) => $"{LatchSize.ToString("F")}m");

            d.OnPropertyChanged
            (
                () => _size,
                (x, y) =>
                {
                    Script.UpdateSize();
                    float minVal = 800f * x * x;
                    float maxVal = 2400f * x * x;
                    forceSlider.UpdateSliderSettings(minVal, maxVal, 101);
                    //MagneticPoleStrength = Mathf.Clamp(MagneticPoleStrength, minVal, maxVal);
                    LatchSize = Math.Max(x, LatchSize);
                }
            );
            d.OnPropertyChanged(() => _latchSize, (x, y) => {Script.UpdateSize(); LatchSize = Math.Max(x, Diameter);});
            d.OnAnyPropertyChanged(() => DesignerPropertyChagned());
        }

        private void DesignerPropertyChagned()
        {
            Symmetry.SynchronizePartModifiers(base.Part.PartScript);
			base.Script.PartScript.CraftScript.RaiseDesignerCraftStructureChangedEvent();
        }
    }
}