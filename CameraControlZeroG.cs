using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Topology;

[AddComponentMenu("Camera-Control/Move ZeroG")]
public class CameraControlZeroG : MonoBehaviour {

	public float startSpeed = 1000f;
	float realSpeed;
	public GUIText movementSpeed;

	Vector3 move = new Vector3();
	
	public GameObject controller;
	public float zoomSpd=96;
	public float maxZoom=2048;
	int zoomLvl=0;
	public int GetZoomLvl() {return zoomLvl;}
	public void SetZoomLvl(int newZoom) {zoomLvl=newZoom; ManageCameraZoom();}
	//private List <Node> selection=new List<Node>();
	int selectMode=0;
	
	public static CameraControlZeroG mainCameraControl;
	
	public delegate void ZoomDelegate();
	public event ZoomDelegate ZoomChanged;
	public delegate void PreZoomDelegate();
	public event PreZoomDelegate PreZoomChanged;
	
	void Start(){
		mainCameraControl=this;
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
		//Vector3 perspCameraMove=Vector3.zero;
		bool zoomIn=false;
		if (InputManager.mainInputManager.currentCursorLoc==InputManager.CursorLoc.OverScene)
		{
			if (Input.GetAxis("Mouse ScrollWheel")>0) 
			{
				zoomIn=true;
				//orthCameraSize-=zoomSpd;
				zoomLvl-=1;	
			}
			if (Input.GetAxis("Mouse ScrollWheel")<0) 
			{
				//orthCameraSize+=zoomSpd;
				zoomLvl+=1;
			}
		}
		
		if (Camera.main.isOrthoGraphic)
		{
			//if (orthCameraSize<maxZoom) {orthCameraSize=maxZoom;}
			if (zoomLvl<0) {zoomLvl=0;}
			realSpeed=startSpeed+startSpeed*zoomLvl*0.75f;
			Vector3 cursorWorldPoint=Camera.main.ScreenToWorldPoint(Input.mousePosition);
			if (PreZoomChanged!=null) PreZoomChanged();
			Camera.main.orthographicSize=orthCameraSize=maxZoom+zoomSpd*zoomLvl;
			if (zoomIn==true)
			{
				Vector3 cursorWorldDelta=cursorWorldPoint-Camera.main.ScreenToWorldPoint(Input.mousePosition);
				Camera.main.transform.position+=cursorWorldDelta;
			}
			if (ZoomChanged!=null)ZoomChanged();
		}
	}
	
	/*
	void SetCameraZoom(int newZoom)
	{
		bool zoomIn=false;
		if (newZoom<zoomLvl) {zoomIn=true;}
		float orthCameraSize=Camera.main.orthographicSize;
		if (Camera.main.isOrthoGraphic)
		{
			if (orthCameraSize<maxZoom) {orthCameraSize=maxZoom;}
			if (zoom<0) {zoomLvl=0;}
			realSpeed=startSpeed+startSpeed*zoomLvl;
			Vector3 cursorWorldPoint=Camera.main.ScreenToWorldPoint(Input.mousePosition);
			Camera.main.orthographicSize=orthCameraSize;
			if (zoomIn==true)
			{
				Vector3 cursorWorldDelta=cursorWorldPoint-Camera.main.ScreenToWorldPoint(Input.mousePosition);
				Camera.main.transform.position+=cursorWorldDelta;
			}
		}
	}
	*/
	
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
		float yTolerance=20f;
		//if cursor is to the left
		if ((Input.mousePosition.x)<xTolerance) {move.x-=realSpeed*Time.deltaTime;}
		//if downward
		if ((Input.mousePosition.y)<yTolerance) {move.y-=realSpeed*Time.deltaTime;}
		//if right
		if ((Input.mousePosition.x)>Screen.width-xTolerance+5) {move.x+=realSpeed*Time.deltaTime;}
		
		if ((Input.mousePosition.y)>Screen.height-yTolerance+10) {move.y+=realSpeed*Time.deltaTime;}
	}

}
