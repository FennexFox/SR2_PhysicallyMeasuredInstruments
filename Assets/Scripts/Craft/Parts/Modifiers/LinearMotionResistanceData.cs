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
	[DesignerPartModifier("LinearResistance", PanelOrder = 2000)]
	public class LinearMotionResistanceData : PartModifierData<LinearMotionResistanceScript>
	{
		private const float DefaultResistance = 1f;

		private const float Density = 1550f;

		[SerializeField]
		[DesignerPropertySlider(0f, 2.5f, 251, Label = "Resistance", Order = 1, Tooltip = "The resistance force to the attached part in Newton.")]
		private float _resistance = 0.01f;

		[SerializeField]
		[DesignerPropertySlider(0, 100f, 101, Label = "Dislocation", Order = 2, Tooltip = "This part doesn't resist linearly until the part attached moves this much in milimeters.")]
		private float _dislocation = 0f;

		[SerializeField]
		[PartModifierProperty(true, false)]
		private bool _preventBreaking;

		[SerializeField]
		[DesignerPropertySlider(0.25f, 1.25f, 21, Label = "Size", Order = 3, Tooltip = "Changes the overall size of the part.")]
		private float _height = 0.5f;

		[SerializeField]
		[DesignerPropertySlider(0.5f, 2.5f, 21, Label = "Thickness", Order = 4, Tooltip = "Changes the thickness of the part.")]
		private float _thickness = 1f;

		public int AttachPointIndex {get; set;}

		public float Resistance => _resistance;

		public float Dislocation => _dislocation / 1000f;

		public override float Mass => CalculateVolume() * 1550f * 0.01f;

		public bool PreventBreaking => _preventBreaking;

		public override int Price => (int)(7500f * _height * _thickness);

		public Vector3 Scale => new Vector3(2 * _height * _thickness, 2 * _height, 2 * _height * _thickness);

		public float Height {get{return _height;} private set{_height = value; base.Script?.UpdateScale();}}

		public float Thickness {get{return _thickness;} private set{_thickness = value;	 base.Script?.UpdateScale();}}

		public float CalculateVolume()
		{
			float num = _height;
			float num2 = 0.1f * _height * Thickness;
			return (float)Math.PI * (num2 * num2) * num;
		}

		protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
		{
			d.OnValueLabelRequested(() => _resistance, (float x) => Units.GetForceString(x));
			d.OnValueLabelRequested(() => _dislocation, (float x) => $"{x.ToString("N0")}mm");
			d.OnValueLabelRequested(() => _height, (float x) => $"{x.ToString("N2")}m");
			d.OnValueLabelRequested(() => _thickness, (float x) => Utilities.FormatPercentage(x));
			d.OnAnyPropertyChanged(() => {DesignerPropertyChanged();});
		}

		private void DesignerPropertyChanged()
		{
			base.Script?.UpdateScale();
			Symmetry.SynchronizePartModifiers(base.Part.PartScript);
			base.Script.PartScript.CraftScript.RaiseDesignerCraftStructureChangedEvent();
		}
	}
}