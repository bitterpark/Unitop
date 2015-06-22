/*
 * Copyright 2014 Jason Graves (GodLikeMouse/Collaboradev)
 * http://www.collaboradev.com
 *
 * This file is part of Unity - Topology.
 *
 * Unity - Topology is free software: you can redistribute it 
 * and/or modify it under the terms of the GNU General Public 
 * License as published by the Free Software Foundation, either 
 * version 3 of the License, or (at your option) any later version.
 *
 * Unity - Topology is distributed in the hope that it will be useful, 
 * but WITHOUT ANY WARRANTY; without even the implied warranty of 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU 
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License 
 * along with Unity - Topology. If not, see http://www.gnu.org/licenses/.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Topology;

[AddComponentMenu("Camera-Control/Move ZeroG")]
public class CameraControlZeroG : MonoBehaviour {

	public float speed = 5f;
	public GUIText movementSpeed;

	private Vector3 move = new Vector3();
	//cluster 1 is also startpoint
	private Vector3 cluster1 = new Vector3(1960, 1791, 2726);
	/*
	private Vector3 cluster2 = new Vector3(2042, 1579, 4254);
	private Vector3 cluster3 = new Vector3(2692, 81, 2526);
	private Vector3 cluster4 = new Vector3(531, 2317, 3776);
	private Vector3 cluster5 = new Vector3(-587, 2043, 2194);
	*/
	public GameObject controller;
	//private List <Node> selection=new List<Node>();
	private int selectMode=0;
	
	void Start(){
		//set to first cluster position
		transform.position = cluster1;
	}
	
	void Update () {
		if (Input.GetKeyDown ("f")) 
		{
			bool mouseLookOn=!GetComponent<MouseLook>().enabled;
			GetComponent<MouseLook>().enabled=mouseLookOn;
			if (mouseLookOn){Screen.lockCursor=true;} else {Screen.lockCursor=false;}
		}
		
		move.x = Input.GetAxis("Horizontal") * speed * Time.deltaTime;
		move.z = Input.GetAxis("Vertical") * speed * Time.deltaTime;
		move.y = 0;
		
		if (Input.GetKey ("space")) {
			move.y = speed * Time.deltaTime;
		}

		if (Input.GetKey ("z")) {
			move.y = -speed * Time.deltaTime;
		}

		//adjust speed with mouse wheel
		speed += Input.GetAxis("Mouse ScrollWheel");
		if (speed < 5)
			speed = 5;

		movementSpeed.text = "Move Speed: " + speed;

		move = transform.TransformDirection(move);
		transform.position += move;
		
	}

}
