namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using Assets.Scripts;
    using ModApi;
    using ModApi.Craft;
    using ModApi.Craft.Parts;
    using ModApi.Craft.Parts.Input;
    using ModApi.Design;
    using ModApi.GameLoop;
    using ModApi.GameLoop.Interfaces;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public class ForceControlledExtenderScript : PartModifierScript<ForceControlledExtenderData>, IDesignerUpdate, IGameLoopItem, IFlightStart, IFlightUpdate, IFlightFixedUpdate
    {
        private class PistonShaft
        {
            public float Height {get; private set;}
            public Vector3 LocalRetractedPosition {get; private set;}
            public Vector3 Offset {get; private set;}
            public Transform Transform {get; private set;}

            public PistonShaft(Transform shaft, Vector3 offset, Vector3? localRetractedPosition = null)
            {
                Transform = shaft;
                Offset = offset;
                LocalRetractedPosition = (localRetractedPosition ?? shaft.localPosition);
                MeshRenderer component = shaft.GetComponent<MeshRenderer>();
                if (component != null)
                {
                    Quaternion localRotation = shaft.localRotation;
                    shaft.rotation = Quaternion.identity;
                    Height = component.bounds.size.y;
                    shaft.localRotation = localRotation;
                }
            }
        }

        private float _priorLength;
        
        private AudioSource _audio;

        private IBodyJoint _bodyJoint;

        private Transform _expectedJointPosition;

        private bool _initializationComplete;

        private IInputController _input;

        private ConfigurableJoint _joint;

        private Rigidbody _jointRigidbody;

        private bool _moving;

        private float _partScale = 1f;

        private List<PistonShaft> _pistonExtenders = new List<PistonShaft>();

        private PistonShaft _pistonShaft;

        private float _pitch;

        private bool _updatePistonShaft;

        private float _volume;

        void IDesignerUpdate.DesignerUpdate(in DesignerFrameData frame)
        {
            if (_initializationComplete)
            {
                float currentPosition = 0f;
                base.Data.CurrentPosition = currentPosition;
                UpdateShaftExtension();
            }
        }

        void IFlightFixedUpdate.FlightFixedUpdate(in FlightFrameData frame)
        {
            if (!_initializationComplete) { return; }
            if (base.PartScript.CommandPod == null || _input == null) { return; }

            if (_joint != null && !_bodyJoint.PartConnection.IsDestroyed)
            {
                _joint.connectedBody.WakeUp();
                _jointRigidbody.WakeUp();

                if (_input.Value > 0) {_joint.targetPosition = new Vector3(Data.Length, 0f, 0f);}
                else if (_input.Value < 0) {_joint.targetPosition = Vector3.zero;}
                else {_joint.targetPosition = new Vector3(Data.CurrentPosition, 0f, 0f);}

                JointDrive jointDrive = default(JointDrive);
                jointDrive.positionSpring = float.MaxValue;
                jointDrive.maximumForce = Math.Abs(_input.Value * Data.Force);
                _joint.xDrive = jointDrive;
            }
            Vector3 position = _joint.connectedBody.transform.TransformPoint(_joint.connectedAnchor);
            Vector3 vector = base.PartScript.BodyScript.Transform.InverseTransformPoint(position);
            Data.CurrentPosition = vector.y - 0.5f;

            if (_updatePistonShaft) { UpdateShaftExtension(); }
        }

        void IFlightStart.FlightStart(in FlightFrameData frame)
        {
            _audio = base.PartScript.GameObject.GetComponent<AudioSource>();
            _input = GetInputController("Extender");
            FindAndSetupConnectionJoint();
            UpdateScale();
            base.Data.UpdateAttachPoint();
            _initializationComplete = true;
            UpdateShaftExtension();
        }

        void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
        {
            if (!_initializationComplete)
            {
                return;
            }
            if (!(_audio != null))
            {
                return;
            }
            if (_moving)
            {
                if (!_audio.isPlaying)
                {
                    _audio.Play();
                }
                _audio.pitch = _pitch;
                _audio.volume = _volume;
            }
            else if (_audio.isPlaying)
            {
                _audio.Stop();
            }
        }

        public override void OnCraftStructureChanged(ICraftScript craftScript)
        {
            base.OnCraftStructureChanged(craftScript);
            if (Game.InFlightScene && (_jointRigidbody != base.PartScript.BodyScript.RigidBody || _joint == null))
            {
                FindAndSetupConnectionJoint();
            }
        }

        public override void OnSymmetry(SymmetryMode mode, IPartScript originalPart, bool created)
        {
            base.Data.UpdateScale();
        }

        public void UpdateScale()
        {
            _pistonShaft.Transform.parent.parent.localScale = new Vector3(base.Data.Width, 1, base.Data.Width);
            UpdateExtenderPositions();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            _partScale = base.PartScript.Data.Config.PartScale.y;
            _expectedJointPosition = Utilities.FindFirstGameObjectMyselfOrChildren("ExpectedJointPosition", base.PartScript.GameObject).transform;
            _pistonShaft = new PistonShaft(Utilities.FindFirstGameObjectMyselfOrChildren("PistonShaft-0", base.PartScript.GameObject).transform, Vector3.zero);
            int num = 1;
            Transform transform = Utilities.FindFirstGameObjectMyselfOrChildren("PistonShaft-" + num, base.PartScript.GameObject)?.transform;
            while (transform != null)
            {
                _pistonExtenders.Add(new PistonShaft(transform, (num == 1) ? (Vector3.up * 0.04f) : Vector3.zero));
                num++;
                transform = Utilities.FindFirstGameObjectMyselfOrChildren("PistonShaft-" + num, base.PartScript.GameObject)?.transform;
            }
            if (!Game.InFlightScene)
            {
                UpdateScale();
                base.Data.UpdateAttachPoint();
                _initializationComplete = true;
                UpdateShaftExtension();
            }
        }

        private void FindAndSetupConnectionJoint()
        {
            int attachPointIndex = base.Data.AttachPointIndex;
            if (base.PartScript.Data.AttachPoints.Count <= attachPointIndex)
            {
                return;
            }
            AttachPoint attachPoint = base.PartScript.Data.AttachPoints[attachPointIndex];
            if (attachPoint.PartConnections.Count == 1)
            {
                foreach (IBodyJoint joint in base.PartScript.BodyScript.Joints)
                {
                    ConfigurableJoint jointForAttachPoint = joint.GetJointForAttachPoint(attachPoint);
                    if (jointForAttachPoint != null)
                    {
                        Rigidbody component = jointForAttachPoint.GetComponent<Rigidbody>();
                        if (base.PartScript.BodyScript.RigidBody == component)
                        {
                            _bodyJoint = joint;
                            _updatePistonShaft = true;

                            SoftJointLimit _linearlimit = new SoftJointLimit();
                            _linearlimit.limit = Data.Length;
                            _linearlimit.bounciness = 0;
                            _linearlimit.contactDistance = 0.001f;

                            jointForAttachPoint.xMotion = ConfigurableJointMotion.Limited;
                            jointForAttachPoint.yMotion = ConfigurableJointMotion.Locked;
                            jointForAttachPoint.zMotion = ConfigurableJointMotion.Locked;

                            JointDrive jointDrive = default(JointDrive);
                            jointDrive.positionSpring = float.MaxValue;
                            jointDrive.positionDamper = 0f;
                            jointDrive.maximumForce = float.MaxValue;
                            jointForAttachPoint.xDrive = jointDrive;

                            _joint = jointForAttachPoint;
                            _joint.linearLimit = _linearlimit;
                            _jointRigidbody = component;
                            break;
                        }
                    }
                }
            }
            else if (attachPoint.PartConnections.Count == 0)
            {
                _updatePistonShaft = true;
            }
        }

        private void UpdateExtenderPositions()
        {
            if (_pistonExtenders.Count != 0)
            {
                _pistonExtenders[0].Transform.localPosition = Vector3.Max(_pistonExtenders[0].LocalRetractedPosition, _pistonShaft.Transform.localPosition - Vector3.up * _pistonShaft.Height / 2f + _pistonExtenders[0].Offset);
                for (int i = 1; i < _pistonExtenders.Count; i++)
                {
                    _pistonExtenders[i].Transform.localPosition = Vector3.Max(_pistonExtenders[i].LocalRetractedPosition, _pistonExtenders[i - 1].Transform.localPosition - Vector3.up * _pistonExtenders[i - 1].Height + _pistonExtenders[i].Offset);
                }
            }
        }

        private void UpdateShaftExtension()
        {
            if (_initializationComplete)
            {
                _pistonShaft.Transform.localPosition = new Vector3(0f, base.Data.CurrentPosition / _partScale, 0f);
                UpdateExtenderPositions();
            }
        }
    }
}