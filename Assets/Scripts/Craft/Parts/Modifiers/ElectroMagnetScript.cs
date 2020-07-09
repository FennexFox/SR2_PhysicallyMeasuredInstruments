namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using Assets.Scripts;
    using Assets.Scripts.Craft;
    using Assets.Scripts.Flight.Sim;
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
        private const float ResetTime = 10f;

        private float _alignmentTime;

        private ElectroMagnetColliderScript _electroMagnetCollider;

        private float _dockResetTimer;

        private float _inspectorDockingStatusPercentage;

        private ConfigurableJoint _magneticJoint;

        private float _magneticJointForce;

        private ElectroMagnetScript _otherElectroMagnet;

        public AttachPoint DockingAttachPoint => base.PartScript.Data.AttachPoints[1];

        private GameObject trigger;
        private GameObject magnet;

        public float DockingTime
        {
            get;
            private set;
        }

        public float InspectorDockingStatusPercentage => _inspectorDockingStatusPercentage;

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

        public bool IsUndocking => _dockResetTimer > 0f;

        public ElectroMagnetScript OtherElectroMagnet => _otherElectroMagnet;

		void IDesignerStart.DesignerStart(in DesignerFrameData frame)
		{
			Update();
		}

        void IFlightFixedUpdate.FlightFixedUpdate(in FlightFrameData frame)
        {
            if (!(_otherElectroMagnet != null))
            {
                return;
            }
            float magnitude = (_otherElectroMagnet.GetJointWorldPosition() - GetJointWorldPosition()).magnitude;
            if (magnitude > 1.5f || _otherElectroMagnet.PartScript.Data.IsDestroyed)
            {
                DestroyMagneticJoint(readyForDocking: true);
                return;
            }
            float num = Vector3.Dot(-base.transform.up, _otherElectroMagnet.transform.up);
            if (num > 0.9999f && magnitude <= 0.01f)
            {
                _alignmentTime += frame.DeltaTime;
            }
            else
            {
                _alignmentTime = 0f;
            }
            float value = (num - 0.99f) / 0.01f * Mathf.Lerp(1f, 0f, Mathf.Clamp01(magnitude * 20f));
            _inspectorDockingStatusPercentage = Mathf.Clamp01(value);
            if (_alignmentTime > 1f)
            {
                CompleteDockConnection();
                return;
            }
            SetMagneticJointForces(magnitude);
        }

        void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
        {
            if (_dockResetTimer > 0f)
            {
                _dockResetTimer -= frame.DeltaTime;
            }
            else if (!IsDocked && !IsDocking && !IsColliderReadyForDocking)
            {
                IsColliderReadyForDocking = true;
            }
        }

        public string GetStatus()
        {
            string result = null;
            if (!base.PartScript.Data.Activated)
            {
                result = "Disabled";
            }
            else if (IsColliderReadyForDocking)
            {
                result = "Ready";
            }
            else if (IsDocking)
            {
                result = $"Docking ({Units.GetPercentageString(_inspectorDockingStatusPercentage)})";
            }
            else if (IsDocked)
            {
                result = "Docked";
            }
            else if (_dockResetTimer > 0f)
            {
                result = "Undocking";
            }
            return result;
        }

        public override void OnDeactivated()
        {
            base.OnDeactivated();
            if (IsDocked)
            {
                Undock();
            }
        }

        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            model.Add(new TextModel("Status", () => GetStatus()));
            IconButtonModel iconButtonModel = new IconButtonModel("Ui/Sprites/Flight/IconPartInspectorUndock", delegate
            {
                Undock();
            }, "Undock");
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
            if (IsReadyForDocking && otherDockingPort.IsReadyForDocking)
            {
                Dock(otherDockingPort);
            }
        }

        public void Undock()
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
                    modifier._dockResetTimer = 10f;
                    modifier.IsColliderReadyForDocking = false;
                    modifier.DockingTime = 0f;
                }
                _dockResetTimer = 10f;
                IsColliderReadyForDocking = false;
                DockingTime = 0f;
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
            magnet = GameObject.Find("ElectroMagnet");
            trigger = GameObject.Find("Trigger");
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
            trigger.transform.localScale = Vector3.up * Data.MagneticForce;
        }

        public void UpdateSize()
        {
            magnet.transform.localScale = Vector3.one * Data.Size;
            trigger.transform.localScale = Vector3.one / Data.Size;
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
            ICraftScript craftScript = base.PartScript.CraftScript;
            ICraftScript craftScript2 = _otherElectroMagnet.PartScript.CraftScript;
            if (craftScript2.CraftNode.IsPlayer && !craftScript.CraftNode.IsPlayer)
            {
                ICraftScript craftScript3 = craftScript;
                craftScript = craftScript2;
                craftScript2 = craftScript3;
            }
            if (_otherElectroMagnet.PartScript.CraftScript != base.PartScript.CraftScript)
            {
                StartCoroutine(OnDockingCompleteNextFrame(craftScript.CraftNode.Name, craftScript2.CraftNode.Name));
                CraftSplitter.MergeCraftNode(craftScript2.CraftNode as CraftNode, craftScript.CraftNode as CraftNode);
            }
            else
            {
                Debug.LogFormat("Could not merge because they were the same craft node");
            }
            CraftBuilder.CreateBodyJoint(CreateDockingPartConnection(_otherElectroMagnet, craftScript));
            base.PartScript.PrimaryCollider.enabled = false;
            base.PartScript.PrimaryCollider.enabled = true;
            float dockingTime = DockingTime = Time.time;
            _otherElectroMagnet.DockingTime = dockingTime;
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
            partConnection.BodyJointData.Position = bodyScript.Transform.InverseTransformPoint(base.PartScript.Transform.TransformPoint(DockingAttachPoint.Position));
            partConnection.BodyJointData.ConnectedPosition = bodyScript2.Transform.InverseTransformPoint(otherPort.PartScript.Transform.TransformPoint(otherPort.DockingAttachPoint.Position));
            partConnection.BodyJointData.BreakTorque = 100000f;
            partConnection.BodyJointData.JointType = BodyJointData.BodyJointType.Docking;
            partConnection.BodyJointData.Body = bodyScript.Data;
            partConnection.BodyJointData.ConnectedBody = bodyScript2.Data;
            return partConnection;
        }

        private void DestroyMagneticJoint(bool readyForDocking)
        {
            IsColliderReadyForDocking = readyForDocking;
            _otherElectroMagnet.IsColliderReadyForDocking = readyForDocking;
            UnityEngine.Object.DestroyImmediate(_magneticJoint);
            _magneticJoint = null;
            _otherElectroMagnet = null;
        }

        private void Dock(ElectroMagnetScript otherPort)
        {
            CraftScript obj = base.PartScript.CraftScript as CraftScript;
            CraftScript craftScript = otherPort.PartScript.CraftScript as CraftScript;
            _otherElectroMagnet = otherPort;
            IsColliderReadyForDocking = false;
            otherPort.IsColliderReadyForDocking = false;
            _alignmentTime = 0f;
            IBodyScript bodyScript = base.PartScript.BodyScript;
            IBodyScript bodyScript2 = otherPort.PartScript.BodyScript;
            Vector3 jointPosition = GetJointPosition();
            Vector3 jointPosition2 = otherPort.GetJointPosition();
            float distance = (jointPosition - jointPosition2).magnitude;
            _magneticJoint = CreateJoint(bodyScript, jointPosition, bodyScript.Transform.InverseTransformDirection(base.transform.up), bodyScript.Transform.InverseTransformDirection(base.transform.right), bodyScript2.RigidBody, jointPosition2);
            SetMagneticJointForces(distance);
            Quaternion targetBodyLocalRotation = Quaternion.FromToRotation(bodyScript.Transform.InverseTransformDirection(otherPort.transform.up), bodyScript.Transform.InverseTransformDirection(-base.transform.up));
            CraftBuilder.SetJointTargetRotation(_magneticJoint, targetBodyLocalRotation);
        }

        private Vector3 GetJointPosition()
        {
            return base.PartScript.BodyScript.Transform.InverseTransformPoint(GetJointWorldPosition());
        }

        private Vector3 GetJointWorldPosition()
        {
            return base.PartScript.Transform.TransformPoint(DockingAttachPoint.Position);
        }

        private IEnumerator OnDockingCompleteNextFrame(string playerCraftName, string otherCraftName)
        {
            yield return null;
            (base.PartScript.CraftScript as CraftScript).OnDockComplete(playerCraftName, otherCraftName);
        }

        private void SetMagneticJointForces(float distance)
        {
            float distanceMultiplier = ( distance + 0.125f ) / 0.125f;
            JointDrive jointDrive = default(JointDrive);
            jointDrive.maximumForce = Math.Min( Data.MagneticForce / ( distanceMultiplier * distanceMultiplier ), float.MaxValue) ;
            jointDrive.positionSpring = float.MaxValue;
            jointDrive.positionDamper = 0f;
            _magneticJoint.xDrive = jointDrive;
            _magneticJoint.yDrive = jointDrive;
            _magneticJoint.zDrive = jointDrive;
            _magneticJoint.rotationDriveMode = RotationDriveMode.XYAndZ;
        }
    }
}