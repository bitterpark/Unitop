using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Topology;

[AddComponentMenu("Camera-Control/Move ZeroG")]
public class CameraControlZeroG : MonoBehaviour {

	public float startSpeed = 1000f;
	float realSpeed;
	public GUIText movementSpeed;

	private Vector3 move = new Vector3();
	
	public GameObject controller;
	public float zoomSpd=96;
	public float maxZoom=768;
	int zoomLvl=0;
	//private List <Node> selection=new List<Node>();
	private int selectMode=0;
	
	void Start(){
		//set to first cluster position
		//transform.position = cluster1;
	}
	
	void Update () 
	{
		
		ManageCameraZoom();
		//Shift+a is for select all
		if(Input.GetAxis("Horizontal")>0 | !Input.GetKey(KeyCode.LeftShift)) {move.x = Input.GetAxis("Horizontal") * realSpeed * Time.deltaTime;}
		move.y=Input.GetAxis("Vertical") * realSpeed * Time.deltaTime;
		move.z=0;
		
		if (Input.GetKey ("space")) {
			move.y = realSpeed * Time.deltaTime;
		}

		if (Input.GetKey ("z")) {
			move.y = -realSpeed * Time.deltaTime;
		}
		
		ManageMMBScroll();
		//ManageLMBScroll();
		
		ManageBorderScroll();
		//adjust speed with mouse wheel
		/*
		speed += Input.GetAxis("Mouse ScrollWheel");
		if (speed < 5)
			speed = 5;
		*/
		movementSpeed.text = "Move Speed: " + realSpeed;

		move = transform.TransformDirection(move);
		transform.position += move;
		
	}
	
	void ManageCameraZoom()
	{
		float orthCameraSize=Camera.main.orthographicSize;
		Vector3 perspCameraMove=Vector3.zero;
		bool zoomIn=false;
		if (Input.GetAxis("Mouse ScrollWheel")>0) 
		{
			perspCameraMove+=new Vector3(0,0,100);
			Vector3 cursorWorldPoint=Camera.main.ScreenToWorldPoint(Input.mousePosition);
			orthCameraSize-=zoomSpd;
			zoomLvl-=1;	
		}
		if (Input.GetAxis("Mouse ScrollWheel")<0) 
		{
			perspCameraMove+=new Vector3(0,0,-100);
			//Camera.main.transform.position=Camera.main.ScreenToWorldPoint(Input.mousePosition);
			
			orthCameraSize+=zoomSpd;
			zoomLvl+=1;
		}
		if (Camera.main.isOrthoGraphic)
		{
			if (orthCameraSize<maxZoom) {orthCameraSize=maxZoom;}
			if (zoomLvl<0) {zoomLvl=0;}
			realSpeed=startSpeed+startSpeed*zoomLvl;
			Vector3 cursorWorldPoint=Camera.main.ScreenToWorldPoint(Input.mousePosition);
			Camera.main.orthographicSize=orthCameraSize;
			if (zoomIn=true)
			{
				Vector3 cursorWorldDelta=cursorWorldPoint-Camera.main.ScreenToWorldPoint(Input.mousePosition);
				Camera.main.transform.position+=cursorWorldDelta;
			}
			//switchCam.camera.orthographicSize=orthCameraSize-
			
		}
		else {transform.position+=perspCameraMove;}
	}
	
	IEnumerator PsideZoomManager(Vector3 cursorWorld)
	{
		//yield return new WaitForEndOfFrame();
		Vector3 cursorWorldDelta=cursorWorld-Camera.main.ScreenToWorldPoint(Input.mousePosition);
		Camera.main.transform.position+=cursorWorldDelta;
		yield break;
	}
	
	//mmb camera pan
	void ManageMMBScroll()
	{
		if (Input.GetMouseButton(2))
		{
			Screen.lockCursor=true;
			move.x = Input.GetAxis("Mouse X") * realSpeed * Time.deltaTime;
			move.y = Input.GetAxis("Mouse Y") * realSpeed * Time.deltaTime;
		}
		else {Screen.lockCursor=false;}
	}
	/*
	//Inverse pan
	void ManageLMBScroll()
	{
		if (Input.GetMouseButton(0))
		{
			Screen.lockCursor=true;
			move.x = -Input.GetAxis("Mouse X") * realSpeed * Time.deltaTime;
			move.y = -Input.GetAxis("Mouse Y") * realSpeed * Time.deltaTime;
		}
		else {Screen.lockCursor=false;}
	}*/
	
	void ManageBorderScroll()
	{
		float xTolerance=10f;
		float yTolerance=5f;
		if ((Input.mousePosition.x)<xTolerance) {move.x-=realSpeed*Time.deltaTime;}
		if ((Input.mousePosition.y)<yTolerance) {move.y-=realSpeed*Time.deltaTime;}
		if ((Input.mousePosition.x)>Screen.width-xTolerance+5) {move.x+=realSpeed*Time.deltaTime;}
		if ((Input.mousePosition.y)>Screen.height-yTolerance) {move.y+=realSpeed*Time.deltaTime;}
	}

}
