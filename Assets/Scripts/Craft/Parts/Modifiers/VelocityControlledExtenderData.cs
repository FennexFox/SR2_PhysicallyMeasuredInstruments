namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using Assets.Scripts.Design;
    using ModApi;
    using ModApi.Craft.Parts;
    using ModApi.Craft.Parts.Attributes;
    using ModApi.Design.PartProperties;
    using System;
    using System.Xml.Linq;
    using UnityEngine;

    [Serializable]
    [DesignerPartModifier("VelocityControlledExtender")]
    [PartModifierTypeId("InternationalDockingSystemStandard.VelocityControlledExtender")]
    public class VelocityControlledExtenderData : PartModifierData<VelocityControlledExtenderScript>
    {
        private const float DefaultRange = 0.5f;

        private const float DefaultCycleTime = 0.5f;

        private const float Density = 1550f;

        [SerializeField]
        [PartModifierProperty(true, false)]
        private float _currentPosition;

        [SerializeField]
        [DesignerPropertyToggleButton(Order = 3, Tooltip = "Enabling this will cause the piston to extend and retract, continuously while input is applied.")]
        private bool _cycle;

        [DesignerPropertyLabel(Order = 100, PreserveState = false, NeverSerialize = true)]
        private string _editMessage = string.Empty;

        [SerializeField]
        [DesignerPropertySpinner(new string[]
        {
            "Pull",
            "Push"
        }, Label = "Direction", Order = 3, Tooltip = "Determines the direction the piston will move when input is applied.")]
        private bool _extend = true;

        [SerializeField]
        [PartModifierProperty(true, false)]
        private bool _preventBreaking;

        [SerializeField]
        [DesignerPropertySlider(0.05f, 1.35f, 27, Label = "Range", Order = 1, PreserveStateMode = PartModifierPropertyStatePreservationMode.SaveAlways, Tooltip = "Changes the range of motion of the piston.")]
        private float _range = 0.5f;

        [SerializeField]
        [DesignerPropertySlider(0.5f, 2.5f, 21, Label = "Size", Order = 2, Tooltip = "Changes the overall size of the piston.")]
        private float _scale = 1f;

        [SerializeField]
        [DesignerPropertySlider(0.1f, 2f, 20, Label = "Cycle Time", Order = 0, Tooltip = "Changes the cycle time of the piston. If set to 1, the piston goes from an end to another in 1 second.")]
        private float _cycleTime = 0.5f;

        public int AttachPointIndex
        {
            get;
            set;
        }

        public float CurrentPosition
        {
            get
            {
                return _currentPosition;
            }
            set
            {
                _currentPosition = value;
            }
        }

        public bool Extend => _extend;

        public override float Mass => CalculateVolume() * 1550f * 0.01f;

        public float MaxRange
        {
            get;
            set;
        }

        public bool PreventBreaking => _preventBreaking;

        public override int Price => (int)(15000f * Scale);

        public float Range => _range;

        public float Scale
        {
            get
            {
                return _scale;
            }
            set
            {
                _scale = value;
                base.Script.UpdateScale();
            }
        }

        public float CycleTime => _cycleTime;

        public float CalculateVolume()
        {
            float num = 0.5f * _scale;
            float num2 = 0.090635f * _scale;
            return (float)Math.PI * (num2 * num2) * num;
        }

        public void UpdateAttachPoint()
        {
            if (AttachPointIndex < base.Part.AttachPoints.Count)
            {
                AttachPoint attachPoint = base.Part.AttachPoints[AttachPointIndex];
                if (_extend)
                {
                    attachPoint.Position = new Vector3(0f, 0.25f, 0f) * _scale - Vector3.up * (1f - Scale) / 4f;
                }
                else
                {
                    attachPoint.Position = new Vector3(0f, 0.25f + Range, 0f) * _scale - Vector3.up * (1f - Scale) / 4f;
                }
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
            MaxRange = 0.5f;
        }

        protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
        {
            d.OnValueLabelRequested(() => _cycleTime, (float x) => x.ToString("0.00") + "s");
            d.OnValueLabelRequested(() => _scale, (float x) => Utilities.FormatPercentage(x));
            d.OnValueLabelRequested(() => _range, (float x) => x.ToString("0.00"));
            d.OnPropertyChanged(() => _range, delegate
            {
                UpdateAttachPoint();
                Symmetry.SynchronizePartModifiers(base.Part.PartScript);
                base.Part.PartScript.CraftScript.RaiseDesignerCraftStructureChangedEvent();
            });
            d.OnPropertyChanged(() => _extend, delegate
            {
                UpdateAttachPoint();
                Symmetry.SynchronizePartModifiers(base.Part.PartScript);
                base.Part.PartScript.CraftScript.RaiseDesignerCraftStructureChangedEvent();
            });
            d.OnPropertyChanged(() => _scale, delegate
            {
                UpdateScale();
                Symmetry.SynchronizePartModifiers(base.Part.PartScript);
                base.Part.PartScript.CraftScript.RaiseDesignerCraftStructureChangedEvent();
            });
            d.OnVisibilityRequested(() => _scale, (bool x) => base.Part.AttachPoints[AttachPointIndex].IsAvailable);
            d.OnVisibilityRequested(() => _extend, (bool x) => base.Part.AttachPoints[AttachPointIndex].IsAvailable);
            d.OnVisibilityRequested(() => _editMessage, (bool x) => IsCakeALie());
            d.OnLabelActivated(() => _editMessage, delegate(ILabelProperty x)
            {
                x.SetPreferredHeight(60f);
            });
        }

        private bool IsCakeALie()
        {
            string empty;
            if (base.Part.AttachPoints[AttachPointIndex].IsAvailable)
            {
                empty = string.Empty;
                return false;
            }
            empty = ((!Extend) ? "Range, direction, and scale cannot be changed while a part is connected to the moving end of the retracting piston." : "Direction and scale cannot be changed while a part is connected to the moving end of the piston.");
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