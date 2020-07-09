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
    [DesignerPartModifier("BallJoint")]
    public class BallJointData : PartModifierData<BallJointScript>
    {
        public enum BaseMode
        {
            Normal,
            Extended,
            None
        }

        [SerializeField]
        [PartModifierProperty(true, false)]
        private float _angle;

        [SerializeField]
        [PartModifierProperty(true, false)]
        private int _attachPointIndex;

        [SerializeField]
        [DesignerPropertySpinner(Label = "Base Style", Tooltip = "Changes the visual style of the base plate. Purely for cosmetic purposes.")]
        private BaseMode _baseMode;

        [SerializeField]
        [PartModifierProperty(true, false)]
        private float _currentAngle;

        private float _lastRange = 90f;

        [SerializeField]
        [PartModifierProperty(true, false)]
        private int _maxRange = 90;

        [SerializeField]
        [PartModifierProperty(true, false)]
        private int _minRange;

        [SerializeField]
        [DesignerPropertySlider(0f, 180f, 37, Tooltip = "Changes the range of rotation.")]
        private float _range = 30f;

        [SerializeField]
        [DesignerPropertySlider(0f, 1000f, 1001, Tooltip = "Changes the force of spring of the ball joint.")]
        private float _springForce = 0f;

        [SerializeField]
        [DesignerPropertySlider(0f, 1000f, 1001, Tooltip = "Changes the retarding force of spring of the ball joint.")]
        private float _damperForce = 0f;

        public float Angle
        {
            get
            {
                return _angle;
            }
            set
            {
                _angle = value;
            }
        }

        public int AttachPointIndex
        {
            get
            {
                return _attachPointIndex;
            }
            set
            {
                _attachPointIndex = value;
            }
        }

        public float CurrentAngle
        {
            get
            {
                return _currentAngle;
            }
            set
            {
                _currentAngle = value;
            }
        }

        public BaseMode MeshBaseMode => _baseMode;

        public float Range
        {
            get
            {
                return _range;
            }
            set
            {
                _range = value;
            }
        }

        public float SpringForce
        {
            get
            {
                return _springForce;
            }
            set
            {
                _springForce = value;
            }
        }

        public float DamperForce
        {
            get
            {
                return _damperForce;
            }
            set
            {
                _damperForce = value;
            }
        }

        static string FormatNewton (float x)
        {
            return x.ToString() + "N";
        }
        protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
        {
            d.OnValueLabelRequested(() => _range, (float x) => (x.ToString() + "°"));
            d.OnValueLabelRequested(() => _springForce, (float x) => FormatNewton(x));
            d.OnValueLabelRequested(() => _damperForce, (float x) => FormatNewton(x));
            d.OnPropertyChanged(() => _baseMode, delegate(BaseMode newVal, BaseMode oldVal)
            {
                base.Script.SetBaseMeshesActiveByMode(newVal);
                Symmetry.SynchronizePartModifiers(base.Part.PartScript);
            });
            d.OnSliderActivated(() => _range, delegate(ISliderProperty x)
            {
                x.UpdateSliderSettings(_minRange, _maxRange, (_maxRange - _minRange) / 5 + 1);
            });
         }
    }
}