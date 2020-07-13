namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using Assets.Scripts;
    using Assets.Scripts.Craft;
    using Assets.Scripts.Flight.Sim;
    using ModApi;
    using ModApi.Audio;
    using ModApi.Craft;
    using ModApi.Craft.Parts;
    using ModApi.Design;
    using ModApi.GameLoop;
    using ModApi.GameLoop.Interfaces;
    using ModApi.Math;
    using ModApi.Ui.Inspector;
    using System;
    using System.Collections;
    using UnityEngine;

    public class ElectroMagnetScript : PartModifierScript<ElectroMagnetData>, IDesignerStart, IFlightFixedUpdate, IGameLoopItem, IFlightUpdate
    {
        private float _alignmentTime;

        private float _maxAlignmentTime = 1F;

        private float _force;

        private float _distanceOffset => 0.125f * Data.Diameter;

        private ElectroMagnetColliderScript _electroMagnetCollider;

        private float _unLockingTimer;

        private ConfigurableJoint _magneticJoint;

        public ConfigurableJoint MagneticJoint => _magneticJoint;

        public ElectroMagnetScript _otherElectroMagnet;

        public AttachPoint DockingAttachPoint => base.PartScript.Data.AttachPoints[1];

        private Transform trigger;

        private Transform magnet;

        private Transform latchBase;

        private Transform latchPetal;

        static Vector3 LatchMove = new Vector3(0f, 0f, 0.075f);

        public bool IsColliderReadyForDocking
        {
            get
            {
                return _electroMagnetCollider.gameObject.activeSelf;
            }
            set
            {
                _electroMagnetCollider.gameObject.SetActive(value);
            }
        }

        public bool IsDocked => DockingAttachPoint.PartConnections.Count > 0;

        public bool IsDocking => _magneticJoint != null;

        public bool IsReadyForDocking
        {
            get
            {
                if (IsColliderReadyForDocking)
                {
                    return base.PartScript.Data.Activated;
                }
                return false;
            }
        }

        public bool IsUnlocking => _unLockingTimer > 0f;

        public ElectroMagnetScript OtherElectroMagnet => _otherElectroMagnet;

		void IDesignerStart.DesignerStart(in DesignerFrameData frame)
		{
			Update();
		}

        void IFlightFixedUpdate.FlightFixedUpdate(in FlightFrameData frame)
        {
            if (!IsDocking) {return;}
            
            if (_otherElectroMagnet.PartScript.Data.IsDestroyed || _otherElectroMagnet.IsReadyForDocking)
            {
                DestroyMagneticJoint(readyForDocking: true);
                return;
            }

            float distanceSQR = Vector3.SqrMagnitude(_otherElectroMagnet.GetJointWorldPosition() - transform.position);
            float distanceLimit = (_distanceOffset + 0.005f) * (_distanceOffset + 0.005f);
            bool rotation = false;

            if (Data.LatchSize == OtherElectroMagnet.Data.LatchSize)
            {
                if (_alignmentTime > _maxAlignmentTime)
                {
                    CompleteDockConnection();
                    _alignmentTime = 0f;
                    return;
                }

                float num = Vector3.Dot(-base.transform.up, _otherElectroMagnet.transform.up);
                if (num > 0.9999f && distanceSQR < distanceLimit)
                {
                    _alignmentTime += frame.DeltaTime;
                    LatchPetalControl(true, frame);
                    rotation = true;
                }
                else
                {
                    if (_alignmentTime > 0f)
                    {
                        _alignmentTime -= frame.DeltaTime;
                        rotation = true;
                    }
                    LatchPetalControl(false, frame);
                }
            }
            SetMagneticJointForces(distanceSQR, rotation);
        }

        void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
        {
            if (!IsDocking && latchPetal.transform.localPosition.z > 0f) {LatchPetalControl(false, frame);}
            if (_unLockingTimer > 0f) {_unLockingTimer -= frame.DeltaTime; LatchPetalControl(false, frame);}
            else if (!IsDocked && !IsDocking && !IsColliderReadyForDocking) {IsColliderReadyForDocking = true;}
            if (0f > _unLockingTimer)  {_unLockingTimer = 0f; Unlock();}
        }

        void LatchPetalControl(bool extension, in FlightFrameData frame)
        {
            if (extension)
            {
                latchPetal.transform.localPosition += LatchMove * frame.DeltaTime;
                latchPetal.transform.localPosition = Vector3.Min(latchPetal.transform.localPosition, LatchMove);
            }
            else
            {
                latchPetal.transform.localPosition -= LatchMove * frame.DeltaTime;
                latchPetal.transform.localPosition = Vector3.Max(latchPetal.transform.localPosition, Vector3.zero);                    
            }
        }

        private void SetMagneticJointForcesAndRotation(float distanceSQR)
        {
            Vector3 latchPetalDirection = latchPetal.transform.InverseTransformDirection(latchPetal.transform.up);
            Vector3 dockingPortDirection = latchPetal.transform.InverseTransformDirection(OtherElectroMagnet.latchPetal.transform.up);
            
            float rotationAngle = Quaternion.FromToRotation(latchPetalDirection, dockingPortDirection).eulerAngles.z;
            float rotationAngleMod = rotationAngle % 120;
            if (rotationAngleMod >= 60f) {rotationAngle += rotationAngleMod;} else {rotationAngle -= rotationAngleMod;}
            _magneticJoint.targetRotation = Quaternion.Euler(rotationAngle, 0f, 0f);
            SetMagneticJointForces(distanceSQR, true);
        }

        public string GetText(string Label)
        {
            string result = null;
            if (IsUnlocking) {if (Label == "Status")
                {
                    result = $"Unlocking ({Units.GetPercentageString(_maxAlignmentTime - _unLockingTimer, _maxAlignmentTime)})";
                }
                else{result = "Unlocking";}}
            else if (!base.PartScript.Data.Activated) {result = "Turned Off";}
            else if (IsDocking){if (Label == "Status")
                {
                    if (_alignmentTime <= Time.deltaTime) {result = "Attracting";}
                    else {result = $"Locking ({Units.GetPercentageString(_alignmentTime, _maxAlignmentTime)})";}
                }
                else if (Label == "Force") {result = Units.GetForceString(_force);}
                else {result = null;}
            }
            else if (IsDocked) {result = "Locked";}
            else if (IsReadyForDocking) {result = "Standby";}
            return result;
        }

        public override void OnDeactivated()
        {
            base.OnDeactivated();
            if (IsDocked) {Unlocking();}
            if (_otherElectroMagnet != null) {DestroyMagneticJoint(readyForDocking: true);}
        }

        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            model.Add(new TextModel("Status", () => GetText("Status")));
            model.Add(new TextModel("Force", () => GetText("Force")));
            IconButtonModel iconButtonModel = new IconButtonModel("Ui/Sprites/Flight/IconPartInspectorUndock", delegate
            {
                Unlocking();
            }, "Unlock");
            iconButtonModel.UpdateAction = delegate(ItemModel x)
            {
                x.Visible = IsDocked;
            };
            model.IconButtonRow.Add(iconButtonModel);
        }

        public override void OnPhysicsChanged(bool enabled)
        {
            base.OnPhysicsChanged(enabled);
            if (!enabled && _magneticJoint != null)
            {
                DestroyMagneticJoint(readyForDocking: true);
            }
        }

        public void OnTouchDockingPort(ElectroMagnetScript otherDockingPort)
        {
            if (IsReadyForDocking && !otherDockingPort.IsReadyForDocking)
            {
                Dock(otherDockingPort);
            }
        }

        public void Unlocking()
        {
            if (!base.PartScript.CraftScript.IsPhysicsEnabled || DockingAttachPoint.PartConnections.Count != 1)
            {
                return;
            }
            PartConnection partConnection = DockingAttachPoint.PartConnections[0];
            foreach (IBodyJoint joint in base.PartScript.BodyScript.Joints)
            {
                if (joint.PartConnection != partConnection)
                {
                    continue;
                }
                ElectroMagnetScript modifier = partConnection.GetOtherPart(base.PartScript.Data).PartScript.GetModifier<ElectroMagnetScript>();
                if (modifier != null)
                {
                    modifier._unLockingTimer = _maxAlignmentTime;
                }
                _unLockingTimer = _maxAlignmentTime;
            }
        }
        
        public void Unlock()
        {
            if (!base.PartScript.CraftScript.IsPhysicsEnabled || DockingAttachPoint.PartConnections.Count != 1)
            {
                return;
            }
            PartConnection partConnection = DockingAttachPoint.PartConnections[0];
            foreach (IBodyJoint joint in base.PartScript.BodyScript.Joints)
            {
                if (joint.PartConnection != partConnection)
                {
                    continue;
                }
                Collider[] componentsInChildren = GetComponentsInChildren<Collider>();
                foreach (Collider collider in componentsInChildren)
                {
                    if (collider.enabled)
                    {
                        collider.enabled = false;
                        collider.enabled = true;
                    }
                }
                joint.Destroy();
                Vector3 force = -base.transform.up;
                float num = 10f;
                base.PartScript.BodyScript.RigidBody.WakeUp();
                force *= num;
                base.PartScript.BodyScript.RigidBody.AddForceAtPosition(force, base.PartScript.Transform.position, ForceMode.Impulse);
                Game.Instance.AudioPlayer.PlaySound(AudioLibrary.Flight.DockDisconnect, base.transform.position);
                break;
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            _electroMagnetCollider = GetComponentInChildren<ElectroMagnetColliderScript>();
            magnet = Utilities.FindFirstGameObjectMyselfOrChildren("ElectroMagnet", base.PartScript.GameObject).transform;;
            trigger = Utilities.FindFirstGameObjectMyselfOrChildren("Trigger", base.PartScript.GameObject).transform;
            latchBase = Utilities.FindFirstGameObjectMyselfOrChildren("LatchBase", base.PartScript.GameObject).transform;
            latchPetal = Utilities.FindFirstGameObjectMyselfOrChildren("LatchPetal", base.PartScript.GameObject).transform;
            IsColliderReadyForDocking = false;
            Update();
        }

        public void Update()
        {
            UpdateForce();
            UpdateSize();
        }

        public void UpdateForce()
        {
            trigger.transform.localScale = Vector3.one * Data.MagneticForce;
        }

        public void UpdateSize()
        {
            magnet.transform.localScale = Vector3.one * Data.Diameter;
            latchBase.transform.localScale = Vector3.one * Data.LatchSize;
            latchBase.transform.localPosition = new Vector3(0f, (Data.Diameter - Data.LatchSize) * 0.125f, 0f);
            trigger.transform.localScale = trigger.transform.localScale / Data.Diameter;
            if (Game.InDesignerScene) {
                Vector3 position = new Vector3(0f, 0.125f, 0f) * Data.Diameter;
                foreach (AttachPoint attachPoint in base.PartScript.Data.AttachPoints) {
                    if (attachPoint.Tag == "Body") {
                        attachPoint.AttachPointScript.transform.localPosition = -position;
                    } else if (attachPoint.Tag == "Magnet") {
                        attachPoint.AttachPointScript.transform.localPosition = position;
                    }
                }
            }
        }

        public override void OnSymmetry(SymmetryMode mode, IPartScript originalPart, bool created)
        {
            Update();
        }

        private static ConfigurableJoint CreateJoint(IBodyScript jointBody, Vector3 jointPosition, Vector3 jointAxis, Vector3 secondaryAxis, Rigidbody connectedBody, Vector3 connectedPosition)
        {
            ConfigurableJoint configurableJoint = jointBody.GameObject.AddComponent<ConfigurableJoint>();
            configurableJoint.connectedBody = connectedBody;
            configurableJoint.autoConfigureConnectedAnchor = false;
            configurableJoint.axis = jointAxis;
            configurableJoint.secondaryAxis = secondaryAxis;
            configurableJoint.anchor = jointPosition;
            configurableJoint.connectedAnchor = connectedPosition;
            configurableJoint.xMotion = ConfigurableJointMotion.Free;
            configurableJoint.yMotion = ConfigurableJointMotion.Free;
            configurableJoint.zMotion = ConfigurableJointMotion.Free;
            configurableJoint.angularXMotion = ConfigurableJointMotion.Free;
            configurableJoint.angularYMotion = ConfigurableJointMotion.Free;
            configurableJoint.angularZMotion = ConfigurableJointMotion.Free;
            configurableJoint.enableCollision = true;
            return configurableJoint;
        }

        private void CompleteDockConnection()
        {
            SetMagneticJointForces(float.MaxValue, false);
            ICraftScript craftScript = base.PartScript.CraftScript;
            ICraftScript craftScript2 = _otherElectroMagnet.PartScript.CraftScript;
            if (craftScript2.CraftNode.IsPlayer && !craftScript.CraftNode.IsPlayer)
            {
                ICraftScript craftScript3 = craftScript;
                craftScript = craftScript2;
                craftScript2 = craftScript3;
            }
            StartCoroutine(OnDockingCompleteNextFrame(craftScript.CraftNode.Name, craftScript2.CraftNode.Name));
            CraftSplitter.MergeCraftNode(craftScript2.CraftNode as CraftNode, craftScript.CraftNode as CraftNode);
            CraftBuilder.CreateBodyJoint(CreateDockingPartConnection(_otherElectroMagnet, craftScript));
            base.PartScript.PrimaryCollider.enabled = false;
            base.PartScript.PrimaryCollider.enabled = true;
            DestroyMagneticJoint(readyForDocking: false);
            Game.Instance.AudioPlayer.PlaySound(AudioLibrary.Flight.DockConnect, base.transform.position);
        }

        private PartConnection CreateDockingPartConnection(ElectroMagnetScript otherPort, ICraftScript craftScript)
        {
            PartConnection partConnection = new PartConnection(base.PartScript.Data, otherPort.PartScript.Data);
            partConnection.AddAttachment(DockingAttachPoint, otherPort.DockingAttachPoint);
            craftScript.Data.Assembly.AddPartConnection(partConnection);
            IBodyScript bodyScript = base.PartScript.BodyScript;
            IBodyScript bodyScript2 = otherPort.PartScript.BodyScript;
            partConnection.BodyJointData = new BodyJointData(partConnection);
            partConnection.BodyJointData.Axis = Vector3.right;
            partConnection.BodyJointData.SecondaryAxis = Vector3.up;
            partConnection.BodyJointData.Position = bodyScript.Transform.InverseTransformPoint(base.PartScript.Transform.TransformPoint(DockingAttachPoint.Position * Data.Diameter));
            partConnection.BodyJointData.ConnectedPosition = bodyScript2.Transform.InverseTransformPoint(otherPort.PartScript.Transform.TransformPoint(otherPort.DockingAttachPoint.Position * otherPort.Data.Diameter));
            partConnection.BodyJointData.BreakTorque = 100000f;
            partConnection.BodyJointData.JointType = BodyJointData.BodyJointType.Docking;
            partConnection.BodyJointData.Body = bodyScript.Data;
            partConnection.BodyJointData.ConnectedBody = bodyScript2.Data;
            return partConnection;
        }

        public void DestroyMagneticJoint(bool readyForDocking)
        {
            IsColliderReadyForDocking = readyForDocking;
            _otherElectroMagnet.IsColliderReadyForDocking = readyForDocking;
            SetMagneticJointForces(float.MaxValue, false);
            UnityEngine.Object.Destroy(_magneticJoint);
            _magneticJoint = null;
            _otherElectroMagnet = null;
        }

        private void Dock(ElectroMagnetScript otherPort)
        {
            CraftScript obj = base.PartScript.CraftScript as CraftScript;
            CraftScript craftScript = otherPort.PartScript.CraftScript as CraftScript;
            _otherElectroMagnet = otherPort;
            _alignmentTime = 0f;
            IBodyScript bodyScript = base.PartScript.BodyScript;
            IBodyScript bodyScript2 = otherPort.PartScript.BodyScript;
            Vector3 jointPosition = GetJointPosition();
            Vector3 jointPosition2 = otherPort.GetJointPosition();
            float distance = (jointPosition - jointPosition2).magnitude;
            _magneticJoint = CreateJoint(bodyScript, jointPosition, bodyScript.Transform.InverseTransformDirection(base.transform.up), bodyScript.Transform.InverseTransformDirection(base.transform.right), bodyScript2.RigidBody, jointPosition2);
            SetMagneticJointForces(distance, false);
            Quaternion targetBodyLocalRotation = Quaternion.FromToRotation(bodyScript.Transform.InverseTransformDirection(otherPort.transform.up), bodyScript.Transform.InverseTransformDirection(-base.transform.up));
            CraftBuilder.SetJointTargetRotation(_magneticJoint, targetBodyLocalRotation);
        }

        private Vector3 GetJointPosition()
        {
            return base.PartScript.BodyScript.Transform.InverseTransformPoint(GetJointWorldPosition());
        }

        private Vector3 GetJointWorldPosition()
        {
            return base.PartScript.Transform.TransformPoint(DockingAttachPoint.Position * Data.Diameter);
        }

        private IEnumerator OnDockingCompleteNextFrame(string playerCraftName, string otherCraftName)
        {
            yield return null;
            (base.PartScript.CraftScript as CraftScript).OnDockComplete(playerCraftName, otherCraftName);
        }

        public void SetMagneticJointForces(float distanceSQR, bool rotation)
        {
            float distanceMultiplier = distanceSQR  / ( _distanceOffset * _distanceOffset );
            JointDrive jointDrive = default(JointDrive);
            jointDrive.maximumForce = Math.Min( Data.MagneticForce / distanceMultiplier, float.MaxValue);
            _force = jointDrive.maximumForce;
            jointDrive.positionSpring = float.MaxValue;
            jointDrive.positionDamper = 0f;
            _magneticJoint.xDrive = jointDrive;
            _magneticJoint.yDrive = jointDrive;
            _magneticJoint.zDrive = jointDrive;
            _magneticJoint.rotationDriveMode = RotationDriveMode.XYAndZ;

            if (rotation)
            {
                _magneticJoint.angularXDrive = jointDrive;
                Vector3 latchPetalDirection = latchPetal.transform.InverseTransformDirection(latchPetal.transform.up);
                Vector3 dockingPortDirection = latchPetal.transform.InverseTransformDirection(OtherElectroMagnet.latchPetal.transform.up);
                
                float rotationAngle = Quaternion.FromToRotation(latchPetalDirection, dockingPortDirection).eulerAngles.z;
                float rotationAngleMod = rotationAngle % 120;
                if (rotationAngleMod >= 60f) {rotationAngle += rotationAngleMod;} else {rotationAngle -= rotationAngleMod;}
                _magneticJoint.targetRotation = Quaternion.Euler(rotationAngle, 0f, 0f);
            }
        }
    }
}