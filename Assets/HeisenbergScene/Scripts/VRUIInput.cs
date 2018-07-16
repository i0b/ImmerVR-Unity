using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Collections;

[RequireComponent(typeof(SteamVR_LaserPointer))]
public class VRUIInput : MonoBehaviour
{
	public Text DebugOutput;

	public GameObject ActualPointerPosition;

	public Logger log;

	public SteamVR_TrackedController SecondController;

	private const long TIMEOUT = 10000000;

	private SteamVR_LaserPointer laserPointer;
	private SteamVR_TrackedController trackedController;

	private bool PointerInButton = false;
	private bool SecondTriggerClicked = false;
	private long SecondTriggerClickTime = 0;

	private int CurrentTrial = 0;

	private Vector3 ControllerPositionBefore, ControllerForwardVectorBefore, ControllerUpVectorBefore;

	private void OnEnable()
	{
		laserPointer = GetComponent<SteamVR_LaserPointer>();
		laserPointer.PointerIn -= HandlePointerIn;
		laserPointer.PointerIn += HandlePointerIn;
		laserPointer.PointerOut -= HandlePointerOut;
		laserPointer.PointerOut += HandlePointerOut;

		trackedController = GetComponent<SteamVR_TrackedController>();
		if (trackedController == null)
		{
			trackedController = GetComponentInParent<SteamVR_TrackedController>();
		}


		trackedController.TriggerClicked -= HandleTriggerClicked;
		trackedController.TriggerClicked += HandleTriggerClicked;

		trackedController.TriggerUnclicked -= HandleTriggerUnclicked;
		trackedController.TriggerUnclicked += HandleTriggerUnclicked;

		trackedController.PadTouched -= HandleTouchPadTouched;
		trackedController.PadTouched += HandleTouchPadTouched;

		trackedController.PadClicked -= HandleTouchPadClicked;
		trackedController.PadClicked += HandleTouchPadClicked;

		SecondController.TriggerClicked -= HandleSecondTriggerClicked;
		SecondController.TriggerClicked += HandleSecondTriggerClicked;
	}

	void Start(){
		log.Initialize ("TouchPad");
		log.writeHeader ("Timestamp;UserID;Trial;Event;OldPosX;OldPosY;OldPosZ;NewPosX;NewPosY;NewPosZ;XDiffPos;YDiffPos;ZDiffPos;Distance;OldFwdX;OldFwdY;OldFwdZ;NewFwdX;NewFwdY;NewFwdZ;XDiffAngle;YDiffAngle;ZDiffAngle;TotalAngle;");
	}



	private void HandleTriggerClicked(object sender, ClickedEventArgs e)
	{
		
		// Accept click only if resting position is set and not too "old"
		if (SecondTriggerClicked && System.DateTime.Now.Ticks - SecondTriggerClickTime < TIMEOUT) {

			CalculateOffsetTriggerClicked ();


		}

	}

	private void HandleTriggerUnclicked(object sender, ClickedEventArgs e)
	{

		// Accept click only if resting position is set and not too "old"
		if (SecondTriggerClicked && System.DateTime.Now.Ticks - SecondTriggerClickTime < TIMEOUT) {

			CalculateOffsetTriggerUnclicked ();
			// reset
			SecondTriggerClicked = false;
			CurrentTrial++;

			ActualPointerPosition.SetActive (true);
			ActualPointerPosition.transform.position = laserPointer.pointer.transform.position +(- trackedController.transform.position + laserPointer.pointer.transform.position);
			StartCoroutine ("HidePointerFeedback");
		}

	}



	private void HandleTouchPadTouched(object sender, ClickedEventArgs e){
		
		// Accept click only if resting position is set and not too "old"
		if (SecondTriggerClicked && System.DateTime.Now.Ticks - SecondTriggerClickTime < TIMEOUT) {

			CalculateOffsetTouchTap ();


		}
	}

	private void HandleTouchPadClicked(object sender, ClickedEventArgs e)
	{

		// Accept click only if resting position is set and not too "old"
		if (SecondTriggerClicked && System.DateTime.Now.Ticks - SecondTriggerClickTime < TIMEOUT) {

			CalculateOffsetTouchClick ();
			// reset
			SecondTriggerClicked = false;
			CurrentTrial++;
			ActualPointerPosition.SetActive (true);
			ActualPointerPosition.transform.position = laserPointer.pointer.transform.position +(- trackedController.transform.position + laserPointer.pointer.transform.position);
			StartCoroutine ("HidePointerFeedback");
		}

	}

	private void HandlePointerIn(object sender, PointerEventArgs e)
	{
		var button = e.target.GetComponent<Button>();
		if (button != null)
		{
			button.Select();

			// Pointer in button, selection can start now
			PointerInButton = true;

			Debug.Log("HandlePointerIn", e.target.gameObject);
		}
	}

	private void HandlePointerOut(object sender, PointerEventArgs e)
	{

		var button = e.target.GetComponent<Button>();
		if (button != null)
		{
			// Pointer left button
			PointerInButton = false;

			EventSystem.current.SetSelectedGameObject(null);
			Debug.Log("HandlePointerOut", e.target.gameObject);
		}
	}

	private void HandleSecondTriggerClicked(object sender, ClickedEventArgs e){
		if (PointerInButton) {
			// Set initial position and rotation now
			ControllerPositionBefore = trackedController.transform.position;
			ControllerForwardVectorBefore = trackedController.transform.forward;
			ControllerUpVectorBefore = trackedController.transform.up;
			SecondTriggerClickTime = System.DateTime.Now.Ticks;
			SecondTriggerClicked = true;
		}

	}

	private void CalculateOffsetTriggerClicked(){
		// ##### Position #####
		Vector3 DistanceVector = trackedController.transform.position - ControllerPositionBefore;
		float TotalDistance = DistanceVector.magnitude;

		// ##### Rotation #####
		// X difference
		Vector3 OldForwardVectorProjectedX = Vector3.ProjectOnPlane(ControllerForwardVectorBefore,trackedController.transform.right);
		Vector3 CurrentForwardVectorProjectedX = Vector3.ProjectOnPlane (trackedController.transform.forward, trackedController.transform.right);
		float XDiff = Vector3.SignedAngle (OldForwardVectorProjectedX, CurrentForwardVectorProjectedX, trackedController.transform.right);

		// Y difference
		Vector3 OldForwardVectorProjectedY = Vector3.ProjectOnPlane(ControllerForwardVectorBefore,Vector3.up);
		Vector3 CurrentForwardVectorProjectedY = Vector3.ProjectOnPlane (trackedController.transform.forward, Vector3.up);
		float YDiff = Vector3.SignedAngle (OldForwardVectorProjectedY, CurrentForwardVectorProjectedY, Vector3.up);

		// Z difference
		Vector3 OldUpVectorProjectedZ = Vector3.ProjectOnPlane(ControllerUpVectorBefore,trackedController.transform.forward);
		Vector3 CurrentUpVectorProjectedZ = Vector3.ProjectOnPlane (trackedController.transform.up, trackedController.transform.forward);
		float ZDiff = Vector3.SignedAngle (OldUpVectorProjectedZ, CurrentUpVectorProjectedZ, trackedController.transform.forward);

		// Ovarall difference
		float TotalDiff = Vector3.Angle(ControllerForwardVectorBefore, trackedController.transform.forward);

		log.writeToLog(string.Format("{0};TriggerClicked;{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12};{13};{14};{15};{16};{17};{18};{19};{20};",CurrentTrial,ControllerPositionBefore.x,ControllerPositionBefore.y,ControllerPositionBefore.z,trackedController.transform.position.x,trackedController.transform.position.y,trackedController.transform.position.z,DistanceVector.x,DistanceVector.y,DistanceVector.z,TotalDistance,ControllerForwardVectorBefore.x,ControllerForwardVectorBefore.y,ControllerForwardVectorBefore.z,trackedController.transform.forward.x,trackedController.transform.forward.y,trackedController.transform.forward.z,XDiff,YDiff,ZDiff,TotalDiff));
			
		DebugOutput.text = string.Format ("Current trial:{5}\nDistance: {0}\nOffset rotation: X:{1}, Y:{2}, Z:{3}\nTotal offset angle: {4}\n", TotalDistance, XDiff, YDiff, ZDiff, TotalDiff,CurrentTrial);
	}

	private void CalculateOffsetTriggerUnclicked(){
		// ##### Position #####
		Vector3 DistanceVector = trackedController.transform.position - ControllerPositionBefore;
		float TotalDistance = DistanceVector.magnitude;

		// ##### Rotation #####
		// X difference
		Vector3 OldForwardVectorProjectedX = Vector3.ProjectOnPlane(ControllerForwardVectorBefore,trackedController.transform.right);
		Vector3 CurrentForwardVectorProjectedX = Vector3.ProjectOnPlane (trackedController.transform.forward, trackedController.transform.right);
		float XDiff = Vector3.SignedAngle (OldForwardVectorProjectedX, CurrentForwardVectorProjectedX, trackedController.transform.right);

		// Y difference
		Vector3 OldForwardVectorProjectedY = Vector3.ProjectOnPlane(ControllerForwardVectorBefore,Vector3.up);
		Vector3 CurrentForwardVectorProjectedY = Vector3.ProjectOnPlane (trackedController.transform.forward, Vector3.up);
		float YDiff = Vector3.SignedAngle (OldForwardVectorProjectedY, CurrentForwardVectorProjectedY, Vector3.up);

		// Z difference
		Vector3 OldUpVectorProjectedZ = Vector3.ProjectOnPlane(ControllerUpVectorBefore,trackedController.transform.forward);
		Vector3 CurrentUpVectorProjectedZ = Vector3.ProjectOnPlane (trackedController.transform.up, trackedController.transform.forward);
		float ZDiff = Vector3.SignedAngle (OldUpVectorProjectedZ, CurrentUpVectorProjectedZ, trackedController.transform.forward);

		// Ovarall difference
		float TotalDiff = Vector3.Angle(ControllerForwardVectorBefore, trackedController.transform.forward);

		log.writeToLog(string.Format("{0};TriggerUnclick;{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12};{13};{14};{15};{16};{17};{18};{19};{20};",CurrentTrial,ControllerPositionBefore.x,ControllerPositionBefore.y,ControllerPositionBefore.z,trackedController.transform.position.x,trackedController.transform.position.y,trackedController.transform.position.z,DistanceVector.x,DistanceVector.y,DistanceVector.z,TotalDistance,ControllerForwardVectorBefore.x,ControllerForwardVectorBefore.y,ControllerForwardVectorBefore.z,trackedController.transform.forward.x,trackedController.transform.forward.y,trackedController.transform.forward.z,XDiff,YDiff,ZDiff,TotalDiff));

		DebugOutput.text += string.Format ("Distance: {0}\nOffset rotation: X:{1}, Y:{2}, Z:{3}\nTotal offset angle: {4}\n", TotalDistance, XDiff, YDiff, ZDiff, TotalDiff);
	}

	private void CalculateOffsetTouchTap(){
		// ##### Position #####
		Vector3 DistanceVector = trackedController.transform.position - ControllerPositionBefore;
		float TotalDistance = DistanceVector.magnitude;

		// ##### Rotation #####
		// X difference
		Vector3 OldForwardVectorProjectedX = Vector3.ProjectOnPlane(ControllerForwardVectorBefore,trackedController.transform.right);
		Vector3 CurrentForwardVectorProjectedX = Vector3.ProjectOnPlane (trackedController.transform.forward, trackedController.transform.right);
		float XDiff = Vector3.SignedAngle (OldForwardVectorProjectedX, CurrentForwardVectorProjectedX, trackedController.transform.right);

		// Y difference
		Vector3 OldForwardVectorProjectedY = Vector3.ProjectOnPlane(ControllerForwardVectorBefore,Vector3.up);
		Vector3 CurrentForwardVectorProjectedY = Vector3.ProjectOnPlane (trackedController.transform.forward, Vector3.up);
		float YDiff = Vector3.SignedAngle (OldForwardVectorProjectedY, CurrentForwardVectorProjectedY, Vector3.up);

		// Z difference
		Vector3 OldUpVectorProjectedZ = Vector3.ProjectOnPlane(ControllerUpVectorBefore,trackedController.transform.forward);
		Vector3 CurrentUpVectorProjectedZ = Vector3.ProjectOnPlane (trackedController.transform.up, trackedController.transform.forward);
		float ZDiff = Vector3.SignedAngle (OldUpVectorProjectedZ, CurrentUpVectorProjectedZ, trackedController.transform.forward);

		// Ovarall difference
		float TotalDiff = Vector3.Angle(ControllerForwardVectorBefore, trackedController.transform.forward);

		log.writeToLog(string.Format("{0};TouchPadTouch;{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12};{13};{14};{15};{16};{17};{18};{19};{20};",CurrentTrial,ControllerPositionBefore.x,ControllerPositionBefore.y,ControllerPositionBefore.z,trackedController.transform.position.x,trackedController.transform.position.y,trackedController.transform.position.z,DistanceVector.x,DistanceVector.y,DistanceVector.z,TotalDistance,ControllerForwardVectorBefore.x,ControllerForwardVectorBefore.y,ControllerForwardVectorBefore.z,trackedController.transform.forward.x,trackedController.transform.forward.y,trackedController.transform.forward.z,XDiff,YDiff,ZDiff,TotalDiff));

		DebugOutput.text = string.Format ("Current trial:{5}\nDistance: {0}\nOffset rotation: X:{1}, Y:{2}, Z:{3}\nTotal offset angle: {4}\n", TotalDistance, XDiff, YDiff, ZDiff, TotalDiff,CurrentTrial);
	}



	private void CalculateOffsetTouchClick(){
		// ##### Position #####
		Vector3 DistanceVector = trackedController.transform.position - ControllerPositionBefore;
		float TotalDistance = DistanceVector.magnitude;

		// ##### Rotation #####
		// X difference
		Vector3 OldForwardVectorProjectedX = Vector3.ProjectOnPlane(ControllerForwardVectorBefore,trackedController.transform.right);
		Vector3 CurrentForwardVectorProjectedX = Vector3.ProjectOnPlane (trackedController.transform.forward, trackedController.transform.right);
		float XDiff = Vector3.SignedAngle (OldForwardVectorProjectedX, CurrentForwardVectorProjectedX, trackedController.transform.right);

		// Y difference
		Vector3 OldForwardVectorProjectedY = Vector3.ProjectOnPlane(ControllerForwardVectorBefore,Vector3.up);
		Vector3 CurrentForwardVectorProjectedY = Vector3.ProjectOnPlane (trackedController.transform.forward, Vector3.up);
		float YDiff = Vector3.SignedAngle (OldForwardVectorProjectedY, CurrentForwardVectorProjectedY, Vector3.up);

		// Z difference
		Vector3 OldUpVectorProjectedZ = Vector3.ProjectOnPlane(ControllerUpVectorBefore,trackedController.transform.forward);
		Vector3 CurrentUpVectorProjectedZ = Vector3.ProjectOnPlane (trackedController.transform.up, trackedController.transform.forward);
		float ZDiff = Vector3.SignedAngle (OldUpVectorProjectedZ, CurrentUpVectorProjectedZ, trackedController.transform.forward);

		// Ovarall difference
		float TotalDiff = Vector3.Angle(ControllerForwardVectorBefore, trackedController.transform.forward);

		log.writeToLog(string.Format("{0};TouchPadClick;{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12};{13};{14};{15};{16};{17};{18};{19};{20};",CurrentTrial,ControllerPositionBefore.x,ControllerPositionBefore.y,ControllerPositionBefore.z,trackedController.transform.position.x,trackedController.transform.position.y,trackedController.transform.position.z,DistanceVector.x,DistanceVector.y,DistanceVector.z,TotalDistance,ControllerForwardVectorBefore.x,ControllerForwardVectorBefore.y,ControllerForwardVectorBefore.z,trackedController.transform.forward.x,trackedController.transform.forward.y,trackedController.transform.forward.z,XDiff,YDiff,ZDiff,TotalDiff));

		DebugOutput.text += string.Format ("Distance: {0}\nOffset rotation: X:{1}, Y:{2}, Z:{3}\nTotal offset angle: {4}\n", TotalDistance, XDiff, YDiff, ZDiff, TotalDiff);
	}


	public IEnumerator HidePointerFeedback(){
		yield return new WaitForSeconds (2f);
		ActualPointerPosition.SetActive (false);
	}

}
