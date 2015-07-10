﻿
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
	//cluster 1 is also startpoint
	private Vector3 cluster1 = new Vector3(1960, 1791, 0);
	/*
	private Vector3 cluster2 = new Vector3(2042, 1579, 4254);
	private Vector3 cluster3 = new Vector3(2692, 81, 2526);
	private Vector3 cluster4 = new Vector3(531, 2317, 3776);
	private Vector3 cluster5 = new Vector3(-587, 2043, 2194);
	*/
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
		
		//mmb camera pan
		if (Input.GetMouseButton(2))
		{
			Screen.lockCursor=true;
			move.x = Input.GetAxis("Mouse X") * realSpeed * Time.deltaTime;
			move.y = Input.GetAxis("Mouse Y") * realSpeed * Time.deltaTime;
		}
		else {Screen.lockCursor=false;}
		
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
		if (Input.GetAxis("Mouse ScrollWheel")>0) 
		{
			perspCameraMove+=new Vector3(0,0,100); 
			orthCameraSize-=zoomSpd;
			zoomLvl-=1;
		}
		if (Input.GetAxis("Mouse ScrollWheel")<0) 
		{
			perspCameraMove+=new Vector3(0,0,-100);
			orthCameraSize+=zoomSpd;
			zoomLvl+=1;
		}
		if (Camera.main.isOrthoGraphic)
		{
			if (orthCameraSize<maxZoom) {orthCameraSize=maxZoom;}
			if (zoomLvl<0) {zoomLvl=0;}
			realSpeed=startSpeed+startSpeed*zoomLvl;
			Camera.main.orthographicSize=orthCameraSize;
		}
		else {transform.position+=perspCameraMove;}
	}
	
	void ManageBorderScroll()
	{
		float xTolerance=10f;
		float yTolerance=0f;
		if ((Input.mousePosition.x)<xTolerance) {move.x-=realSpeed*Time.deltaTime;}
		if ((Input.mousePosition.y)<yTolerance) {move.y-=realSpeed*Time.deltaTime;}
		if ((Input.mousePosition.x)>Screen.width-xTolerance) {move.x+=realSpeed*Time.deltaTime;}
		if ((Input.mousePosition.y)>Screen.height-yTolerance) {move.y+=realSpeed*Time.deltaTime;}
	}

}
