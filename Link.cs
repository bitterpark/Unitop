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

namespace Topology {

	public class Link : MonoBehaviour {

		public string id;
		public Node source;
		public Node target;
		public string sourceId;
		public string targetId;
		public string status;
		public bool loaded = false;

		LineRenderer lineRenderer;
		CapsuleCollider capsule;
		public GameObject controller;
		
		public bool selected
		{
			get{return _selected;}
			set
			{
				if (value) renderer.material.color=Color.blue; 
				else renderer.material.color=Color.black;//new Color(22,70,109,255);//Color.blue;
				_selected=value; 
			}
			
		}
		public bool _selected;
		
		void Start () 
		{
			//get controller for prefab
			controller=Camera.main.gameObject.GetComponent<CameraControlZeroG>().controller;
			
			lineRenderer = gameObject.AddComponent<LineRenderer>();
			//add collider
			capsule = gameObject.AddComponent<CapsuleCollider>();
			
			//color link according to status
			Color c;
			if (status == "Up")
				c = Color.black;
			else
				c = Color.red;
			c.a = 0.5f;

			//draw line
			//lineRenderer.sortingOrder=-2;
			float lineWidth=0.3f;
			lineRenderer.sortingOrder=-2;
			lineRenderer.material = new Material (Shader.Find("Self-Illumin/Diffuse"));
			lineRenderer.material.SetColor ("_Color", c);
			lineRenderer.SetWidth(lineWidth,lineWidth);
			lineRenderer.SetVertexCount(2);
			lineRenderer.SetPosition(0, new Vector3(0,0,0));
			lineRenderer.SetPosition(1, new Vector3(1,0,0));
			
			//configure collider
			capsule.radius = lineWidth;// / 2;
			capsule.center = Vector3.zero;
			capsule.direction = 2; // Z-axis for easier "LookAt" orientation
			capsule.isTrigger=true;
		}

		void Update () {
			if(source && target && !loaded){
				//draw links as full duplex, half in each direction
				Vector3 m = (target.transform.position - source.transform.position)/2 + source.transform.position;
				lineRenderer.SetPosition(0, source.transform.position);
				lineRenderer.SetPosition(1, m);
				//configure collider
				
				capsule.transform.position = source.transform.position + (m - source.transform.position) / 2;
				capsule.transform.LookAt(source.transform.position);
				capsule.height = (m-source.transform.position).magnitude;//(m - source.transform.position).magnitude*8.5;
				loaded = true;
			}
			
			
		}
		
		void OnMouseDown()
		{
			//Camera.main.gameObject.GetComponent<CameraControlZeroG>().LinkReturn(this);
			controller.GetComponent<GameController>().ClickLink(this);
		}
	}

}