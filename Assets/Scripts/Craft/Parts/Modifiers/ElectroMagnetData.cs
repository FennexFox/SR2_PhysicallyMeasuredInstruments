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
		[DesignerPropertySlider(800f, 2400f, 101, Label = "Magnetic Force", Order = 1, Tooltip = "Magnetic Force of the electro magnet at its surface(Diameter * 0.125m).")]
		private float _magneticPoleStrength = 1600f; // SR2 Force is 100 times stronger than real life, 1600f means 160kN

        [SerializeField]
		[DesignerPropertySlider(0.05f, 2f, 40, Label = "Diameter", Order = 2, Tooltip = "Changes the size of the magnet.")]
		private float _size = 1f;

        [SerializeField]
		[DesignerPropertySlider(0.05f, 2f, 40, Label = "Latch Diameter", Order = 3, Tooltip = "Changes the size of the locking mehchanism.")]
		private float _latchSize = 1f;

/*
        private float _magneticPoleStrength = Area * Max Ampare * Turn per Length;
        private float _area = Math.Pi * Diameter / 4f;
        private float _turnPerLength;
*/

        public float MagneticPoleStrength => _magneticPoleStrength;

        public float Diameter {get{return _size;} private set{_size = value; base.Script.UpdateSize();}}

        public float LatchSize {get{return _latchSize;} private set{_latchSize = value; base.Script.UpdateSize();}}

        ISliderProperty forceSlider;

        public override float Mass => (8000f * Diameter * Diameter * Diameter + 25f * LatchSize * LatchSize * LatchSize) * 0.01f; // SR2 mass is 100 times lighter

		protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
		{
			d.OnValueLabelRequested(() => _magneticPoleStrength, (float x) => Units.GetForceString(x));
            d.OnValueLabelRequested(() => _size, (float x) => $"{x.ToString("F")}m");
            d.OnValueLabelRequested(() => _latchSize, (float x) => $"{LatchSize.ToString("F")}m");

            d.OnPropertyChanged(() => _magneticPoleStrength, (x, y) => {Script.UpdateForce();});
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

            d.OnSliderActivated(() => _magneticPoleStrength, (ISliderProperty x) => {x.UpdateSliderSettings(800f * _size * _size, 2400f * _size * _size, 101); forceSlider = x;});
        }

        private void DesignerPropertyChagned()
        {
            Symmetry.SynchronizePartModifiers(base.Part.PartScript);
			base.Script.PartScript.CraftScript.RaiseDesignerCraftStructureChangedEvent();
        }
    }
}