namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using Assets.Scripts.Design;
    using ModApi;
    using ModApi.Craft.Parts;
    using ModApi.Craft.Parts.Attributes;
    using ModApi.Design.PartProperties;
    using ModApi.Math;
    using System;
    using System.Xml.Linq;
    using UnityEngine;


    [Serializable]
    [DesignerPartModifier("ForceControlledExtender")]
    [PartModifierTypeId("PhysicallyMeasuredInstruments.ForceControlledExtender")]
    public class ForceControlledExtenderData : PartModifierData<ForceControlledExtenderScript>
    {
        private const float DefaultRange = 0.5f;

        private const float DefaultAcceleration = 0.5f;

        private const float Density = 1550f;

        [SerializeField]
        [PartModifierProperty(true, false)]
        private float _currentPosition;

        [DesignerPropertyLabel(Order = 100, PreserveState = false, NeverSerialize = true)]
        private string _editMessage = string.Empty;

        [SerializeField]
        [PartModifierProperty(true, false)]
        private bool _preventBreaking = true;

        [SerializeField]
        [DesignerPropertySlider(1f, 200f, 200, Label = "Force", Order = 0, Tooltip = "Change the force of the extender.")]
        private float _force = 0.5f;

        [SerializeField]
        [DesignerPropertySlider(0.5f, 4f, 36, Label = "Length", Order = 1, PreserveStateMode = PartModifierPropertyStatePreservationMode.SaveAlways, Tooltip = "Changes the length of the extender.")]
        private float _length = 0.5f;

        [SerializeField]
        [DesignerPropertySlider(0.5f, 2.5f, 21, Label = "Width", Order = 2, Tooltip = "Changes the width of the piston.")]
        private float _width = 1f; // change it to _baseSize

        public int AttachPointIndex {get; set;}

        public float CurrentPosition {get{return _currentPosition;} set{_currentPosition = value;}}

        public override float Mass => CalculateVolume() * 1550f * 0.01f;

        public bool PreventBreaking => _preventBreaking;

        public override int Price => (int)(15000f * Width);

        public float Length => _length;

        public float Width {get{return _width;} set{_width = value; base.Script.UpdateScale();}}

        public float Force => _force;

        public float CalculateVolume()
        {
            float num = 0.090635f * _width;
            return (float)Math.PI * (num * num);
        }

        public void UpdateAttachPoint()
        {
            if (AttachPointIndex < base.Part.AttachPoints.Count)
            {
                AttachPoint attachPoint = base.Part.AttachPoints[AttachPointIndex];
                attachPoint.Position = new Vector3(0f, 0.25f, 0f);
                if (base.Part.PartScript != null && attachPoint.AttachPointScript != null)
                {
                    attachPoint.AttachPointScript.transform.localPosition = attachPoint.Position;
                }
            }
        }

        public void UpdateScale()
        {
            base.Script.UpdateScale();
            UpdateAttachPoint();
        }

        protected override void OnCreated(XElement partModifierXml)
        {
            base.OnCreated(partModifierXml);
            AttachPointIndex = 0;
        }

        protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
        {
            d.OnValueLabelRequested(() => _force, (float x) => Units.GetForceString(x));
            d.OnValueLabelRequested(() => _width, (float x) => Utilities.FormatPercentage(x));
            d.OnValueLabelRequested(() => _length, (float x) => x.ToString("0.00") + "m");
            d.OnPropertyChanged(() => _length, (x, y) =>
            {
                UpdateAttachPoint();
                Symmetry.SynchronizePartModifiers(base.Part.PartScript);
                base.Part.PartScript.CraftScript.RaiseDesignerCraftStructureChangedEvent();
            });
            d.OnPropertyChanged(() => _width, (x, y) =>
            {
                UpdateScale();
                Symmetry.SynchronizePartModifiers(base.Part.PartScript);
                base.Part.PartScript.CraftScript.RaiseDesignerCraftStructureChangedEvent();
            });
            d.OnVisibilityRequested(() => _width, (bool x) => base.Part.AttachPoints[AttachPointIndex].IsAvailable);
            d.OnVisibilityRequested(() => _length, (bool x) => base.Part.AttachPoints[AttachPointIndex].IsAvailable);
            d.OnVisibilityRequested(() => _editMessage, (bool x) => IsCakeALie());
            d.OnLabelActivated(() => _editMessage, (ILabelProperty x) => x.SetPreferredHeight(60f));
            //d.OnSliderActivated(() => _length, (ISliderProperty x) => x.UpdateSliderSettings(0, 4f * BaseSize, BaseSize * 35 + 1));
        }

        private bool IsCakeALie()
        {
            string empty;
            if (base.Part.AttachPoints[AttachPointIndex].IsAvailable)
            {
                empty = string.Empty;
                return false;
            }
            empty = ("Direction and scale cannot be changed while a part is connected to the moving end of the piston.");
            if (_editMessage != empty)
            {
                _editMessage = empty;
                if (base.DesignerPartProperties.Manager != null)
                {
                    base.DesignerPartProperties.Manager.RefreshUI();
                }
            }
            return true;
        }
    }
}