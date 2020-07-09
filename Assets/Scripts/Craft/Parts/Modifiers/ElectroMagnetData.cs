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

        public float Size
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
            d.OnAnyPropertyChanged(() => DesignerPropertyChagned());
        }

        private void DesignerPropertyChagned()
        {
            Symmetry.SynchronizePartModifiers(base.Part.PartScript);
			base.Script.PartScript.CraftScript.RaiseDesignerCraftStructureChangedEvent();
        }
    }
}