	using Assets.Scripts;
	using Assets.Scripts.Craft.Parts.Modifiers;
	using ModApi;
	using ModApi.Craft;
	using ModApi.Craft.Parts;
	using ModApi.Design;
	using ModApi.GameLoop;
	using ModApi.GameLoop.Interfaces;
	using System.Collections.Generic;
	using UnityEngine;

	public class LinearMotionResistanceScript : PartModifierScript<LinearMotionResistanceData>, IDesignerStart, IGameLoopItem, IFlightStart, IFlightUpdate
	{
		private Dictionary<AttachPoint, Vector3> _attachPointsOriginalLocalPositions = new Dictionary<AttachPoint, Vector3>();

		private IBodyJoint _bodyJoint;

		private Transform _bottomPoint;

		private float _breakTimer;

		private ConfigurableJoint _joint;

		private Transform _shaft;

		private Transform _spring;

		private Transform _suspension;

		private Transform _topShaftMeshOrigin;

		void IDesignerStart.DesignerStart(in DesignerFrameData frame)
		{
			UpdateScale();
		}

		void IFlightStart.FlightStart(in FlightFrameData frame)
		{
			UpdateScale();
			_shaft = Utilities.FindFirstGameObjectMyselfOrChildren("TopShaft", base.PartScript.GameObject).transform;
			_spring = Utilities.FindFirstGameObjectMyselfOrChildren("Spring", base.PartScript.GameObject).transform;
			_topShaftMeshOrigin = Utilities.FindFirstGameObjectMyselfOrChildren("TopShaftMeshOrigin", base.PartScript.GameObject).transform;
			_bottomPoint = Utilities.FindFirstGameObjectMyselfOrChildren("BottomPoint", base.PartScript.GameObject).transform;
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
							Rigidbody component = jointForAttachPoint.GetComponent<Rigidbody>();
							if (base.PartScript.BodyScript.RigidBody == component)
							{
								component.maxDepenetrationVelocity = 1f;
								_bodyJoint = joint;
								_joint = jointForAttachPoint;
								ConfigureJoint();
								break;
							}
						}
					}
				}
			}
			if (_joint == null)
			{
				Debug.Log("Can't find joint");
			}
		}

		void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
		{
			float num = 0.5f;
			if (_joint != null && !_bodyJoint.Broken)
			{
				Vector3 position = _joint.connectedBody.transform.TransformPoint(_joint.connectedAnchor);
				Vector3 vector = _bottomPoint.InverseTransformPoint(position);
				if (vector.y > 0.8f)
				{
					_breakTimer += frame.DeltaTime;
				}
				else
				{
					_breakTimer = 0f;
				}
				if (_breakTimer > 0.5f && !base.Data.PreventBreaking)
				{
					_bodyJoint.Destroy();
				}
				num = vector.y;
			}
			float value = num - 0.5f;
			value = Mathf.Clamp(value, -0.4f, 2f);
			_shaft.localPosition = new Vector3(0f, value, 0f);
			float value2 = num / 0.5f;
			value2 = Mathf.Clamp(value2, 0.1f, 2f);
			_spring.transform.localScale = new Vector3(1f, value2, 1f);
			if (value > 0f)
			{
				_topShaftMeshOrigin.localScale = new Vector3(1f, 1.5f, 1f);
			}
			else
			{
				_topShaftMeshOrigin.localScale = Vector3.one;
			}
		}

		public override void OnSymmetry(SymmetryMode mode, IPartScript originalPart, bool created)
		{
			UpdateScale();
		}

		public void UpdateScale()
		{
			Vector3 scale = base.Data.Scale;
			_suspension.localScale = scale;
			if (Game.InDesignerScene)
			{
				foreach (AttachPoint attachPoint in base.Data.Part.AttachPoints)
				{
					attachPoint.Position = Vector3.Scale(_attachPointsOriginalLocalPositions[attachPoint], scale);
					attachPoint.AttachPointScript.transform.localPosition = attachPoint.Position;
				}
			}
		}

		protected override void OnInitialized()
		{
			base.OnInitialized();
			_suspension = Utilities.FindFirstGameObjectMyselfOrChildren("Suspension", base.PartScript.GameObject).transform;
			if (Game.InDesignerScene)
			{
				foreach (AttachPoint attachPoint in base.Data.Part.AttachPoints)
				{
					_attachPointsOriginalLocalPositions[attachPoint] = attachPoint.Position;
				}
			}
		}

		private void BreakJoint()
		{
			_bodyJoint.Destroy();
		}

		private void ConfigureJoint()
		{
			JointDrive xDrive = default(JointDrive);

			xDrive.positionSpring = base.Data.Resistance / Data.Dislocation;
			xDrive.positionDamper = 0f;
			xDrive.maximumForce =  base.Data.Resistance;

			_joint.xDrive = xDrive;
			_joint.xMotion = ConfigurableJointMotion.Free;
			_joint.yMotion = ConfigurableJointMotion.Locked;
			_joint.zMotion = ConfigurableJointMotion.Locked;
			
			_joint.targetPosition = new Vector3(0.5f, 0f, 0f) * base.Data.Part.Config.PartScale.y * base.Data.Size;
			_joint.anchor = _bodyJoint.Body.Transform.InverseTransformPoint(_bottomPoint.position);
		}
	}
