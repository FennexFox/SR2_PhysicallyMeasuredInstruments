namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using Assets.Scripts;
    using ModApi;
    using ModApi.Craft;
    using ModApi.Craft.Parts;
    using ModApi.Design;
    using ModApi.GameLoop;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;

    public class BallJointScript : PartModifierScript<BallJointData>, IFlightStart, IGameLoopItem, IFlightFixedUpdate
    {
        private float _angle;

        private AudioSource _audio;

        private Rigidbody _connectedRigidBody;

        private ConfigurableJoint _joint;

        private Rigidbody _rigidBody;

        private float _springForce;

        private Transform _visualMesh;

        void IFlightFixedUpdate.FlightFixedUpdate(in FlightFrameData frame)
        {
            if (_joint = null)
            {
                return;
            }
            base.Data.CurrentAngle = base.Data.Angle;
            if (_rigidBody.IsSleeping())
            {
                _rigidBody.WakeUp();
            }
            if (_connectedRigidBody.IsSleeping())
            {
                _connectedRigidBody.WakeUp();
            }
        }

        void IFlightStart.FlightStart(in FlightFrameData frame)
        {
            SetupJoint();
        }

        public override void OnCraftLoaded(ICraftScript craftScript, bool movedToNewCraft)
        {
            base.OnCraftLoaded(craftScript, movedToNewCraft);
            CheckJoint();
        }

        public override void OnCraftStructureChanged(ICraftScript craftScript)
        {
            base.OnCraftStructureChanged(craftScript);
            CheckJoint();
        }

        public override void OnSymmetry(SymmetryMode mode, IPartScript originalPart, bool created)
        {
            SetBaseMeshesActiveByMode(base.Data.MeshBaseMode);
        }

        public void SetBaseMeshesActiveByMode(BallJointData.BaseMode baseMode)
        {
            GameObject gameObject = Utilities.FindFirstGameObjectMyselfOrChildren("RotatorBaseExtension", base.gameObject);
            GameObject gameObject2 = Utilities.FindFirstGameObjectMyselfOrChildren("RotatorBase", base.gameObject);
            switch (baseMode)
            {
            case BallJointData.BaseMode.Extended:
                gameObject2?.SetActive(value: true);
                gameObject?.SetActive(value: true);
                break;
            case BallJointData.BaseMode.Normal:
                gameObject2?.SetActive(value: true);
                gameObject.SetActive(value: false);
                break;
            case BallJointData.BaseMode.None:
                gameObject2?.SetActive(value: false);
                gameObject.SetActive(value: false);
                break;
            default:
                Debug.LogError("Unsupported BallJointData.BaseMode \"" + baseMode.ToString() + "\"");
                break;
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            Setup(Game.InFlightScene);
        }

        private void CheckJoint()
        {
            if (base.PartScript.BodyScript != null && (_rigidBody != base.PartScript.BodyScript.RigidBody || _joint == null || _joint.connectedBody != _connectedRigidBody))
            {
                SetupJoint();
            }
        }

        private void Setup(bool inFlight)
        {
            GameObject gameObject = Utilities.FindFirstGameObjectMyselfOrChildren("Hinge", base.gameObject);
            if (gameObject != null)
            {
                _visualMesh = gameObject.transform;
            }
            SetBaseMeshesActiveByMode(base.Data.MeshBaseMode);
        }

        private void SetupJoint()
        {
            int attachPointIndex = base.Data.AttachPointIndex;
            if (base.PartScript.Data.AttachPoints.Count > attachPointIndex)
            {
                AttachPoint attachPoint = base.PartScript.Data.AttachPoints[attachPointIndex];
                if (attachPoint.PartConnections.Count == 1)
                {
                    foreach (IBodyJoint joint in base.PartScript.BodyScript.Joints)
                    {
                        ConfigurableJoint jointForAttachPoint = joint.GetJointForAttachPoint(attachPoint);
                        if (jointForAttachPoint != null)
                        {
                            _joint = jointForAttachPoint;
                            _rigidBody = _joint.GetComponent<Rigidbody>();
                            _connectedRigidBody = _joint.connectedBody;
                        }
                    }
                    if (_joint == null)
                    {
                        Debug.LogError("Could not find joint for the rotator", this);
                    }
                }
            }
            _springForce = base.Data.SpringForce;
            if (_joint != null)
            {
                JointDrive angularXDrive = _joint.angularXDrive;
                angularXDrive.positionSpring = _springForce;
                angularXDrive.positionDamper = base.Data.DamperForce;

                _joint.angularXDrive = angularXDrive;
                _joint.angularXMotion = ConfigurableJointMotion.Limited;
                SoftJointLimit lowAngularXLimit = _joint.lowAngularXLimit;
                lowAngularXLimit.limit = 0f - base.Data.Range;
                _joint.lowAngularXLimit = lowAngularXLimit;
                lowAngularXLimit.limit = base.Data.Range;
                _joint.highAngularXLimit = lowAngularXLimit;

                _joint.angularYZDrive = angularXDrive;
                _joint.angularYMotion = _joint.angularZMotion = _joint.angularXMotion;
                _joint.angularYLimit = _joint.angularZLimit = lowAngularXLimit;

                if (_visualMesh != null && _joint != null)
                {
                    _visualMesh.parent = _joint.connectedBody.transform;
                }
            }
        }
    }
}