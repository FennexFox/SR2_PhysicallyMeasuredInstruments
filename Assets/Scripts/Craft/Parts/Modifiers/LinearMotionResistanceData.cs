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
	[DesignerPartModifier("LinearResistance", PanelOrder = 2000)]
	public class LinearMotionResistanceData : PartModifierData<LinearMotionResistanceScript>
	{
		private const float DefaultResistance = 1f;

		private const float Density = 1550f;

		[SerializeField]
		[DesignerPropertyTextInput(Label = "Resistance", Order = 1, Tooltip = "This sensor applies the resistance force to the attached part in Newton.")]
		private string _resistance = "1";

		[SerializeField]
		[DesignerPropertyTextInput(Label = "Dislocation", Order = 2, Tooltip = "This part doesn't resist linearly until the part attached moves this much in milimeters.")]
		private string _dislocation = "1";

		[SerializeField]
		[PartModifierProperty(true, false)]
		private bool _preventBreaking;

		[SerializeField]
		[DesignerPropertySlider(0.5f, 2.5f, 21, Label = "Size", Order = 3, Tooltip = "Changes the overall size of the part.")]
		private float _size = 1f;

		[SerializeField]
		[DesignerPropertySlider(0.5f, 2.5f, 21, Label = "Thickness", Order = 4, Tooltip = "Changes the thickness of the part.")]
		private float _thickness = 1f;

		public int AttachPointIndex
		{
			get;
			set;
		}

		public float Resistance => Convert.ToSingle(_resistance);

		public float Dislocation => Convert.ToSingle(_dislocation) / 1000f;

		public override float Mass => CalculateVolume() * 1550f * 0.01f;

		public bool PreventBreaking => _preventBreaking;

		public override int Price => (int)(7500f * _size * _thickness);

		public Vector3 Scale => new Vector3(_size * _thickness, _size, _size * _thickness);

		public float Size
		{
			get
			{
				return _size;
			}
			private set
			{
				_size = value;
				base.Script?.UpdateScale();
			}
		}

		public float Thickness
		{
			get
			{
				return _thickness;
			}
			private set
			{
				_thickness = value;
				base.Script?.UpdateScale();
			}
		}

		public float CalculateVolume()
		{
			float num = 0.5f * _size;
			float num2 = 0.05f * _size * Thickness;
			return (float)Math.PI * (num2 * num2) * num;
		}

		protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
		{
			d.OnValueLabelRequested(() => _size, (float x) => Utilities.FormatPercentage(x));
			d.OnValueLabelRequested(() => _thickness, (float x) => Utilities.FormatPercentage(x));
			d.OnPropertyChanged(() => _size, delegate
			{
				DesignerScaleChanged();
			});
			d.OnPropertyChanged(() => _thickness, delegate
			{
				DesignerScaleChanged();
			});
		}

		private void DesignerScaleChanged()
		{
			base.Script?.UpdateScale();
			Symmetry.SynchronizePartModifiers(base.Part.PartScript);
			base.Script.PartScript.CraftScript.RaiseDesignerCraftStructureChangedEvent();
		}
	}
}