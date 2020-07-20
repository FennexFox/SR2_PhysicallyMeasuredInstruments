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
    [PartModifierTypeId("PhysicallyMeasuredInstruments.ElectroMagnet")]
    public class ElectroMagnetData : PartModifierData<ElectroMagnetScript>
    {
/*
        [SerializeField]
		[DesignerPropertySlider(800f, 2400f, 101, Label = "Magnetic Force", Order = 1, Tooltip = "Magnetic Force of the electro magnet at its surface(Diameter * 0.125m).")]
		private float _magneticPoleStrength = 1600f; // SR2 Force is 100 times stronger than real life, 1600f means 160kN
*/

        public enum HasLatch {Disabled, Enabled};
        public enum isNorthPole {North, South};

        [SerializeField]
        [DesignerPropertySpinner(Label = "Effective Pole", Order = 1, Tooltip = "The Magnetic Pole of the effective face.")]
        private isNorthPole _isNorthPole;

        [SerializeField]
		[DesignerPropertySlider(0.05f, 2f, 40, Label = "Diameter", Order = 1, Tooltip = "Changes the size of the magnet core material.")]
		private float _size = 1f;

        [SerializeField]
		[DesignerPropertySlider(20f, 240f, 12, Label = "Max Ampere", Order = 2, Tooltip = "Changes the max electric current you can put into the magnet.")]
		private float _maxAmpere = 120f;

        [SerializeField]
		[DesignerPropertySlider(500f, 10000f, 20, Label = "Turn Per Length", Order = 3, Tooltip = "Changes the number of turns of the coil per length.")]
		private float _turnPerLength = 1000f;

        [SerializeField]
        [DesignerPropertySpinner(Label = "Latch Locker", Order = 4, Tooltip = "Electoragnets with latch mechanism of same size can lock into each other, transfer fuel and electricity.")]
        private HasLatch _hasLatch;

        [SerializeField]
		[DesignerPropertySlider(0.05f, 2f, 40, Label = "Latch Diameter", Order = 5, Tooltip = "Changes the size of the locking mehchanism.")]
		private float _latchSize = 1f;

        public int pole
        {
            get {if (_isNorthPole == isNorthPole.North) {return 1;} else if (_isNorthPole == isNorthPole.South) {return -1;} else {return 0;}}
        }

        private double _area => Math.PI * Diameter * Diameter / 4f;

/*
        private double _coilResistance => (_coilResistivity * _turnPerLength / _coilCrossSection) * (_size * 0.25f) * (Math.PI * _size);

        private float _coilResistivity = 0.0000000168f; // from "shorturl.at/vxV18"

        private float _coilCrossSection = 0.0000002f; // from "shorturl.at/vxV18"
*/
        public float Volt = 120f; // from Orion Spacecraft; gonna be configurable soon

        public float MaxMagneticPoleStrength => Convert.ToSingle(_area) * MaxAmpere * TurnPerLength * 0.01f; // SR2 Forces are 100 times stronger

        public float Diameter {get{return _size;} private set{_size = value; base.Script.UpdateSize();}}

        public double Area => _area;
        
        public double Volume => _area * 0.25f * Diameter;

        public float MaxAmpere => _maxAmpere;

        public float TurnPerLength => _turnPerLength;

        public float LatchSize {get{return _latchSize;} private set{_latchSize = value; base.Script.UpdateSize();}}

        public bool DrawLatch => Convert.ToBoolean(_hasLatch);

        ISliderProperty forceSlider;

        public override float Mass => (8000f * Diameter * Diameter * Diameter + 25f * LatchSize * LatchSize * LatchSize * Convert.ToInt32(_hasLatch)) * 0.01f; // SR2 mass is 100 times lighter

		protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
		{
            d.OnValueLabelRequested(() => _size, (float NewVal) => $"{NewVal.ToString("N")} m");
            d.OnValueLabelRequested(() => _maxAmpere, (float NewVal) => $"{NewVal.ToString("N0")} A");
            d.OnValueLabelRequested(() => _turnPerLength, (float NewVal) => $"{NewVal.ToString("N0")} Turn / m");
            d.OnValueLabelRequested(() => _latchSize, (float NewVal) => $"{LatchSize.ToString("N")} m");

            d.OnPropertyChanged(() => _size, (float NewVal, float OldVal) => {Script.UpdateSize(); LatchSize = Math.Max(NewVal, LatchSize);});
            d.OnPropertyChanged(() => _hasLatch, (HasLatch NewVal, HasLatch OldVal) => {Script.SetLatchMode();});
            d.OnPropertyChanged(() => _latchSize, (float NewVal, float OldVal) => {Script.UpdateSize(); LatchSize = Math.Max(NewVal, Diameter);});

            d.OnVisibilityRequested(() => _latchSize, (bool NewVal) => Convert.ToBoolean(_hasLatch));

            d.OnAnyPropertyChanged(() => DesignerPropertyChagned());
        }

        private void DesignerPropertyChagned()
        {
            Symmetry.SynchronizePartModifiers(base.Part.PartScript);
			base.Script.PartScript.CraftScript.RaiseDesignerCraftStructureChangedEvent();
        }
    }
}