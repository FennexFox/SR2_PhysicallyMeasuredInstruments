namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using Assets.Scripts;
    using Assets.Scripts.Craft;
    using Assets.Scripts.Flight.Sim;
    using ModApi;
    using ModApi.Audio;
    using ModApi.Craft;
    using ModApi.Craft.Parts;
    using ModApi.Craft.Parts.Input;
    using ModApi.Design;
    using ModApi.GameLoop;
    using ModApi.GameLoop.Interfaces;
    using ModApi.Math;
    using ModApi.Ui.Inspector;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    public class ElectroMagnetScript : PartModifierScript<ElectroMagnetData>, IFlightStart, IDesignerStart, IFlightFixedUpdate, IGameLoopItem, IFlightUpdate
    {
        public Dictionary<int, ElectroMagnetScript> NearbyMagnets = new Dictionary<int, ElectroMagnetScript>();

        private Dictionary<float, int> NearbyLatchesKeys = new Dictionary<float, int>();

        private Dictionary<int, Vector3> NearbyForces = new Dictionary<int, Vector3>();

        private float _alignmentTime;

        private float _maxAlignmentTime = 1F;

        private int pole = 1; // This means MagneticEffectPoint is N pole

        public int Pole => pole;

        public string PoleString {get{if (pole == 1) {return "N";} else if (pole == -1) {return "S";}else {return "what";}}}

        private double vacuumPermeability = 0.0000004f * Math.PI;

        private ElectroMagnetColliderScript _electroMagnetCollider;

        private float _unLockingTimer;

        private ConfigurableJoint _magneticJoint;

        public ConfigurableJoint MagneticJoint => _magneticJoint;

        private ElectroMagnetScript _otherElectroMagnet;

        private float _force;

        private Vector3 _magneticForceAtCurrentPosition;

        private Vector3 _BFieldAtCurrentPoistion;

        public AttachPoint MagneticEffectPoint => base.PartScript.Data.AttachPoints[1];

        public Vector3 MagneticEffectPointPosition => GetJointWorldPosition(base.PartScript.Data.AttachPoints[1]);

        public Vector3 BodyAttachPointPosition => GetJointWorldPosition(base.PartScript.Data.AttachPoints[0]);

        private Transform trigger; 

        private Transform magnet;

        private Transform latchBase;

        private Transform latchPetal;

        private GameObject latch;

        private IFuelSource _battery;

        private IInputController _input;

        public float MagneticPoleStrength => Data.MaxMagneticPoleStrength * _input.Value * Convert.ToInt32(!_battery.IsEmpty);

        public Vector3 DirectionFromStoN => Vector3.Normalize(MagneticEffectPointPosition - BodyAttachPointPosition) * pole;

        public Vector3 MagneticDipoleMoment => MagneticPoleStrength * Data.Diameter * 0.25f * DirectionFromStoN;

        public Vector3 Magnetization => MagneticDipoleMoment / Convert.ToSingle(Data.Volume);

        private float _inputAmpere => _input.Value * Data.MaxAmpere;
        
        public float PowerConsumption => _inputAmpere * Data.Volt;

        static Vector3 LatchMove = new Vector3(0f, 0f, 0.0375f);

        public bool IsDocked => MagneticEffectPoint.PartConnections.Count > 0;

        public bool IsDocking => _magneticJoint != null;

        public bool IsUnlocking => _unLockingTimer > 0f;

		void IDesignerStart.DesignerStart(in DesignerFrameData frame)
		{
			UpdateSize();
            SetLatchMode();
		}

        void IFlightStart.FlightStart(in FlightFrameData frame)
        {
            _input = GetInputController("PowerInput");
        }

        void IFlightFixedUpdate.FlightFixedUpdate(in FlightFrameData frame)
        {
            UpdateForce();
            
            Debug.Log($"Craft ID: {GetComponentInParent<CraftScript>().Data.Name} Part ID: {GetComponentInParent<PartScript>().Data.Id} Count: {NearbyMagnets.Count} Input: {_input.Value}");

            if (!_battery.IsEmpty) {_battery.RemoveFuel(PowerConsumption * frame.DeltaTime);}
            else {base.PartScript.Data.Activated = false;}

            if (NearbyMagnets.Count > 0)
            {
                _magneticForceAtCurrentPosition = Vector3.zero;
                NearbyLatchesKeys.Clear(); NearbyForces.Clear();

                if (_otherElectroMagnet != null && (_otherElectroMagnet.PartScript.Data.IsDestroyed || Pole == _otherElectroMagnet.Pole)) {DestroyMagneticJoint();}

                foreach(KeyValuePair<int, ElectroMagnetScript> items in NearbyMagnets)
                {
                    ElectroMagnetScript ThatMagnet = items.Value;
                    float distanceSQR = Vector3.SqrMagnitude(ThatMagnet.MagneticEffectPointPosition - MagneticEffectPointPosition);
                    float magneticForceMagnitude = Math.Min(MagneticForceMagnitude(distanceSQR, ThatMagnet), float.MaxValue);
                    Vector3 direction = (MagneticEffectPointPosition - ThatMagnet.MagneticEffectPointPosition).normalized * Pole * ThatMagnet.Pole;
                    Vector3 magneticForce = magneticForceMagnitude * direction;
                    _magneticForceAtCurrentPosition += magneticForce;
                    NearbyForces.Add(items.Key, magneticForce);

                    if (Data.DrawLatch && ThatMagnet.Data.DrawLatch)
                    {
                        if (Data.LatchSize == ThatMagnet.Data.LatchSize && Pole != ThatMagnet.Pole && !ThatMagnet.IsDocked)
                        {
                            NearbyLatchesKeys.Add(distanceSQR, items.Key);
                        }
                    }
                }
                
                if (NearbyLatchesKeys.Count > 0 && !IsDocked)
                {
                    List<float> NearbyLatchesDistanceSQR = NearbyLatchesKeys.Keys.ToList();
                    NearbyLatchesDistanceSQR.Sort();
                    float ClosestLatchDistanceSQR = NearbyLatchesDistanceSQR[0];
                    int ClosestLatchKey = NearbyLatchesKeys[ClosestLatchDistanceSQR];

                    if (_otherElectroMagnet != null)
                    {
                        if (_otherElectroMagnet.GetInstanceID() != ClosestLatchKey) {DestroyMagneticJoint();}
                        else {_magneticForceAtCurrentPosition -= NearbyForces[_otherElectroMagnet.GetInstanceID()];}
                    }
                    else if (NearbyMagnets[ClosestLatchKey]._otherElectroMagnet == null || NearbyMagnets[ClosestLatchKey]._otherElectroMagnet == this)
                    {
                        _magneticForceAtCurrentPosition -= NearbyForces[ClosestLatchKey];
                        Dock(NearbyMagnets[ClosestLatchKey]);
                    }
                }

                Magnetism(_magneticForceAtCurrentPosition, MagneticEffectPointPosition);
                Magnetism(_magneticForceAtCurrentPosition, BodyAttachPointPosition);
                //_BFieldAtCurrentPoistion = 2 * _magneticForceAtCurrentPosition * Convert.ToSingle(vacuumPermeability / Data.Area);
            }
            
            if (_otherElectroMagnet != null)
            {
                bool IsRotate = false;
                float distanceSQR = Vector3.SqrMagnitude(MagneticEffectPointPosition - _otherElectroMagnet.MagneticEffectPointPosition);

                if (_alignmentTime > _maxAlignmentTime)
                {
                    CompleteDockConnection();
                    _alignmentTime = 0f;
                    return;
                }

                float distanceSQRLimit = 0.0001f;
                float num = Vector3.Dot(-base.transform.up, _otherElectroMagnet.transform.up);
                if (num > 0.9999f && distanceSQR <= distanceSQRLimit)
                {
                    _alignmentTime += frame.DeltaTime;
                    LatchPetalControl(true, frame);
                    IsRotate = true;
                }
                else
                {
                    if (_alignmentTime > 0f)
                    {
                        _alignmentTime -= frame.DeltaTime;
                        IsRotate = true;
                    }
                    LatchPetalControl(false, frame);
                }
                SetMagneticJointForces(distanceSQR, IsRotate);
            }
        }

        void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
        {
            if (!IsDocking && !IsDocked && latchPetal.transform.localPosition.z > 0f) {LatchPetalControl(false, frame);}
            if (_unLockingTimer > 0f) {_unLockingTimer -= frame.DeltaTime; LatchPetalControl(false, frame);}
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

        public String GetText1(string Label)
        {
            string result = null;
            switch (Label)
            {
                case "Volt":
                    result = $"{Data.Volt} V" ; break;
                case "Ampere":
                    result = $"{_inputAmpere.ToString("F")} A" ; break;
                case "Watt":
                    result = $"{PowerConsumption:n} W" ; break;
                case "PoleStrength":
                    result = $"{MagneticPoleStrength:n} Am" ; break;
            }
            return result;
        }
        
        public string GetText2(string Label)
        {
            string result = null;
            if (IsUnlocking) {if (Label == "Status")
                {
                    result = $"Unlocking ({Units.GetPercentageString(_maxAlignmentTime - _unLockingTimer, _maxAlignmentTime)})";
                }
                else{result = "Unlocking";}}
            else if (IsDocked) {result = "Locked";}
            else if (IsDocking){if (Label == "Status")
                {
                    if (_alignmentTime <= Time.deltaTime) {result = "Attracting";}
                    else {result = $"Locking ({Units.GetPercentageString(_alignmentTime, _maxAlignmentTime)})";}
                }
                else if (Label == "Target") {result = $"{_otherElectroMagnet.PartScript.Data.Name} ({_otherElectroMagnet.PartScript.Data.Id.ToString()})";}
                else if (Label == "Force") {result = Units.GetForceString(_force + _otherElectroMagnet._force);}
                else if (Label == "Distance") {result = Units.GetDistanceString(Vector3.Magnitude(MagneticEffectPointPosition - _otherElectroMagnet.MagneticEffectPointPosition));}
                else {result = null;}
            }
            return result;
        }

        public override void OnDeactivated()
        {
            base.OnDeactivated();
            if (_otherElectroMagnet != null) {DestroyMagneticJoint();}
        }

        private void ChangePole() {pole *= -1;}

        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            var poleChanger = new LabelButtonModel("Effective Pole", b => ChangePole());
            poleChanger.ButtonLabel = PoleString;

            var electroMagnetInfo = new GroupModel("Electroagnet Info");
            electroMagnetInfo.Add(new TextModel("Input Volt", () => GetText1("Volt")));
            electroMagnetInfo.Add(new TextModel("Ampere", () => GetText1("Ampere")));
            electroMagnetInfo.Add(new TextModel("Watt", () => GetText1("Watt")));
            electroMagnetInfo.Add(new TextModel("Pole Strength", () => GetText1("PoleStrength")));
            electroMagnetInfo.Add(poleChanger);
            model.AddGroup(electroMagnetInfo);

            var latchApproachInfo = new GroupModel("Latch Approach Info");
            latchApproachInfo.Add(new TextModel("Status", () => GetText2("Status")));
            latchApproachInfo.Add(new TextModel("Target", () => GetText2("Target")));
            latchApproachInfo.Add(new TextModel("Distance", () => GetText2("Distance")));
            latchApproachInfo.Add(new TextModel("Force", () => GetText2("Force")));
            latchApproachInfo.Visible = IsDocking || IsDocked || IsUnlocking;
            model.AddGroup(latchApproachInfo);  

            IconButtonModel iconButtonModel = new IconButtonModel("Ui/Sprites/Flight/IconPartInspectorUndock", delegate{Unlocking();}, "Unlock");
            iconButtonModel.UpdateAction = delegate(ItemModel x)
            {
                x.Visible = IsDocked;
            };
            model.IconButtonRow.Add(iconButtonModel);
        }

        public override void OnPhysicsChanged(bool enabled)
        {
            base.OnPhysicsChanged(enabled);
            if (!enabled && _magneticJoint != null) {DestroyMagneticJoint();}
        }

        public override void OnCraftLoaded(ICraftScript craftScript, bool movedToNewCraft)
        {
            base.OnCraftLoaded(craftScript, movedToNewCraft);
            OnCraftStructureChanged(craftScript);
        }

        public override void OnCraftStructureChanged(ICraftScript craftScript)
        {
            base.OnCraftStructureChanged(craftScript);
            _battery = base.PartScript.BatteryFuelSource;
            _input = GetInputController("PowerInput");
        }


        public void Unlocking()
        {
            if (!base.PartScript.CraftScript.IsPhysicsEnabled || !IsDocked) {return;}
            PartConnection partConnection = MagneticEffectPoint.PartConnections[0];
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
            PartConnection partConnection = MagneticEffectPoint.PartConnections[0];
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
                Game.Instance.AudioPlayer.PlaySound(AudioLibrary.Flight.DockDisconnect, base.transform.position);
                latchPetal.gameObject.SetActive(true);
                break;
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            _electroMagnetCollider = GetComponentInChildren<ElectroMagnetColliderScript>();
            magnet = Utilities.FindFirstGameObjectMyselfOrChildren("ElectroMagnet", base.PartScript.GameObject).transform;
            GameObject Latch = Utilities.FindFirstGameObjectMyselfOrChildren("LatchBase", base.gameObject);
            trigger = Utilities.FindFirstGameObjectMyselfOrChildren("Trigger", base.PartScript.GameObject).transform;
            latchBase = Utilities.FindFirstGameObjectMyselfOrChildren("LatchBase", base.PartScript.GameObject).transform;
            latchPetal = Utilities.FindFirstGameObjectMyselfOrChildren("LatchPetal", base.PartScript.GameObject).transform;
            latch = Utilities.FindFirstGameObjectMyselfOrChildren("LatchBase", base.gameObject);
            _electroMagnetCollider.gameObject.SetActive(!Game.InDesignerScene);
            UpdateSize();
            SetLatchMode();
        }

        public void UpdateForce()
        {
            trigger.transform.localScale = Vector3.one * 50f * MagneticPoleStrength / 1600f;
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

        public void SetLatchMode()
        {
            if (Data.DrawLatch) {latch.SetActive(true);} else {latch.SetActive(false);}
        }

        public override void OnSymmetry(SymmetryMode mode, IPartScript originalPart, bool created)
        {
            UpdateSize();
            SetLatchMode();
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
            CraftBuilder.CreateBodyJoint(CreateDockingPartConnection(_otherElectroMagnet, craftScript));
            base.PartScript.PrimaryCollider.enabled = false;
            base.PartScript.PrimaryCollider.enabled = true;
            Game.Instance.AudioPlayer.PlaySound(AudioLibrary.Flight.DockConnect, base.transform.position);
            latchPetal.gameObject.SetActive(false);
            DestroyMagneticJoint();
            base.PartScript.Data.Activated = false;
        }

        private PartConnection CreateDockingPartConnection(ElectroMagnetScript otherPort, ICraftScript craftScript)
        {
            PartConnection partConnection = new PartConnection(base.PartScript.Data, otherPort.PartScript.Data);
            partConnection.AddAttachment(MagneticEffectPoint, otherPort.MagneticEffectPoint);
            craftScript.Data.Assembly.AddPartConnection(partConnection);
            IBodyScript bodyScript = base.PartScript.BodyScript;
            IBodyScript bodyScript2 = otherPort.PartScript.BodyScript;
            partConnection.BodyJointData = new BodyJointData(partConnection);
            partConnection.BodyJointData.Axis = Vector3.right;
            partConnection.BodyJointData.SecondaryAxis = Vector3.up;
            partConnection.BodyJointData.Position = bodyScript.Transform.InverseTransformPoint(base.PartScript.Transform.TransformPoint(MagneticEffectPoint.Position * Data.Diameter));
            partConnection.BodyJointData.ConnectedPosition = bodyScript2.Transform.InverseTransformPoint(otherPort.PartScript.Transform.TransformPoint(otherPort.MagneticEffectPoint.Position * otherPort.Data.Diameter));
            partConnection.BodyJointData.BreakTorque = 100000f;
            partConnection.BodyJointData.JointType = BodyJointData.BodyJointType.Docking;
            partConnection.BodyJointData.Body = bodyScript.Data;
            partConnection.BodyJointData.ConnectedBody = bodyScript2.Data;
            return partConnection;
        }

        public void DestroyMagneticJoint()
        {
            UnityEngine.Object.Destroy(_magneticJoint);
            _magneticJoint = null;
            _otherElectroMagnet = null;
            _force = 0f;
        }

        private void Dock(ElectroMagnetScript otherPort)
        {
            CraftScript obj = base.PartScript.CraftScript as CraftScript;
            CraftScript craftScript = otherPort.PartScript.CraftScript as CraftScript;
            _otherElectroMagnet = otherPort;
            _alignmentTime = 0f;
            IBodyScript bodyScript = base.PartScript.BodyScript;
            IBodyScript bodyScript2 = otherPort.PartScript.BodyScript;
            Vector3 jointPosition = GetJointPosition(MagneticEffectPoint);
            Vector3 jointPosition2 = otherPort.GetJointPosition(MagneticEffectPoint);
            float distanceSQR = Vector3.SqrMagnitude(jointPosition2 - jointPosition);
            _magneticJoint = CreateJoint(bodyScript, jointPosition, bodyScript.Transform.InverseTransformDirection(base.transform.up), bodyScript.Transform.InverseTransformDirection(base.transform.right), bodyScript2.RigidBody, jointPosition2);
            SetMagneticJointForces(distanceSQR, false);
            Quaternion targetBodyLocalRotation = Quaternion.FromToRotation(bodyScript.Transform.InverseTransformDirection(otherPort.transform.up), bodyScript.Transform.InverseTransformDirection(-base.transform.up));
            CraftBuilder.SetJointTargetRotation(_magneticJoint, targetBodyLocalRotation);
        }

        private Vector3 GetJointPosition(AttachPoint AttachPoint)
        {
            return base.PartScript.BodyScript.Transform.InverseTransformPoint(GetJointWorldPosition(AttachPoint));
        }

        private Vector3 GetJointWorldPosition(AttachPoint AttachPoint)
        {
            return base.PartScript.Transform.TransformPoint(AttachPoint.Position * Data.Diameter);
        }

        private IEnumerator OnDockingCompleteNextFrame(string playerCraftName, string otherCraftName)
        {
            yield return null;
            (base.PartScript.CraftScript as CraftScript).OnDockComplete(playerCraftName, otherCraftName);
        }

        private float MagneticForceMagnitude(float distanceSQR, ElectroMagnetScript ThatMagnet)
        {
            float distanceAdjusted = Math.Max(distanceSQR, 0.0001f);
            float force = Math.Min(0.0000001f * MagneticPoleStrength * ThatMagnet.MagneticPoleStrength / distanceAdjusted, float.MaxValue);
            // 0.0000001f = vacuumPermeability / 4 * Math.PI
            return force;
        }

        public void SetMagneticJointForces(float distanceSQR, bool IsRotate)
        {
            JointDrive jointDrive = default(JointDrive);
            jointDrive.maximumForce = MagneticForceMagnitude(distanceSQR, _otherElectroMagnet)/2f;
            _force = jointDrive.maximumForce;
            jointDrive.positionSpring = float.MaxValue;
            jointDrive.positionDamper = 0f;
            _magneticJoint.xDrive = jointDrive;
            _magneticJoint.yDrive = jointDrive;
            _magneticJoint.zDrive = jointDrive;
            _magneticJoint.rotationDriveMode = RotationDriveMode.XYAndZ;

            if (IsRotate)
            {
                jointDrive.positionDamper = jointDrive.maximumForce; // test float.MaxValue
                _magneticJoint.angularXDrive = jointDrive;
                Vector3 latchPetalDirection = latchPetal.transform.InverseTransformDirection(latchPetal.transform.up);
                Vector3 dockingPortDirection = latchPetal.transform.InverseTransformDirection(_otherElectroMagnet.latchPetal.transform.up);
                
                float rotationAngle = Quaternion.FromToRotation(latchPetalDirection, dockingPortDirection).eulerAngles.z;
                float rotationAngleMod = rotationAngle % 120;
                if (rotationAngleMod >= 60f) {rotationAngle += rotationAngleMod;} else {rotationAngle -= rotationAngleMod;}
                _magneticJoint.targetRotation = Quaternion.Euler(rotationAngle, 0f, 0f);
            }
        }

        public void Magnetism(Vector3 magneticForce, Vector3 pole)
        {GetComponentInParent<Rigidbody>().AddForceAtPosition(magneticForce, pole);}

    }
}