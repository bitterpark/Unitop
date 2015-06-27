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
using Vectrosity;

namespace Topology {

	public class Link {//: MonoBehaviour {

		public string id;
		public Node source;
		public Node target;
		public string sourceId;
		public string targetId;
		public string status;
		public string color;
		public bool loaded = false;
		VectorLine myLine;
		//public int sourcePointIndex;
		//public int targetPointIndex;
		//public Material lineMaterial;

		//LineRenderer lineRenderer;
		CapsuleCollider capsule;
		//public GameObject controller;
		public GameController controller;
		
		public bool selected
		{
			get{return _selected;}
			set
			{
				if (value) {controller.LinkChangeColor(this,Color.blue);}//myLine.SetColor(Color.blue);//renderer.material.color=Color.blue; 
				else {controller.LinkChangeColor(this,GetColorFromString(color));}//myLine.SetColor(GetColorFromString(color));//renderer.sharedMaterial.color=GetColorFromString(color);//new Color(22,70,109,255);//Color.blue;
				_selected=value; 
			}
			
		}
		bool _selected;
		
		/*
		void Start () 
		{
			//get controller for prefab
			//controller=Camera.main.gameObject.GetComponent<CameraControlZeroG>().controller;
			
			//lineRenderer = gameObject.GetComponent<LineRenderer>();//gameObject.AddComponent<LineRenderer>();
			
			//add collider
			//capsule = gameObject.AddComponent<CapsuleCollider>();
			
			//color link according to status
			
			
			//draw line
			//lineRenderer.sortingOrder=-2;
			float lineWidth=0.3f;
			//lineRenderer.sortingOrder=-2;
			//lineRenderer.material = new Material (Shader.Find("Self-Illumin/Diffuse"));
			//lineRenderer.material.SetColor ("_Color",GetColorFromString(color));
			//lineRenderer.sharedMaterial.SetColor("_Color",GetColorFromString(color));
			//lineRenderer.SetWidth(lineWidth,lineWidth);
			//lineRenderer.SetVertexCount(2);
			//lineRenderer.SetPosition(0, new Vector3(0,0,0));
			//lineRenderer.SetPosition(1, new Vector3(1,0,0));
			
			//configure collider
			
			capsule.radius = lineWidth;// / 2;
			capsule.center = Vector3.zero;
			capsule.direction = 2; // Z-axis for easier "LookAt" orientation
			capsule.isTrigger=true;
		}*/
		
		public int GetColorIndex()
		{
			int retInt=0;
			switch (color)
			{
				case "black": {retInt=0; break;}
				case "red": {retInt=1; break;}
				case "green": {retInt=2; break;}
				case "yellow": {retInt=3; break;}
				case "cyan": {retInt=4; break;}
			}
			return retInt;
		}
		
		public Color GetColorFromString(string color)
		{
			Color c=Color.cyan;
			switch (color)
			{
				case "black": {c=Color.gray; break;}//Color.grey; break;}
				case "red": {c=Color.red; break;}
				case "green": {c=Color.green; break;}
				case "yellow": {c=Color.yellow; break;}
				case "cyan": {c=Color.cyan; break;}
			}
			return c;
		}
		
		/*
		void Update () {
			//First frame object setup
			if(source && target && !loaded)
			{
				Vector3[] linePoints=new Vector3[2];
				linePoints[0]=source.transform.position;
				linePoints[1]=target.transform.position;
				//myLine=VectorLine.SetLine3D(GetColorFromString(color),linePoints);
				//myLine.SetWidth(2f);
				//myLine.sortingOrder=-5;
				//myLine.Draw3DAuto();
				//draw links as full duplex, half in each direction
				//Vector3 m = (target.transform.position - source.transform.position)/2 + source.transform.position;
				//lineRenderer.SetPosition(0, source.transform.position);
				//lineRenderer.SetPosition(1, target.transform.position);
				//configure collider
				
				capsule.transform.position = source.transform.position + (target.transform.position - source.transform.position)*0.5f;
				capsule.transform.LookAt(source.transform.position);
				capsule.height = (target.transform.position-source.transform.position).magnitude;//(m - source.transform.position).magnitude*8.5;
				loaded = true;
			}
			//if (Input.GetMouseButtonDown(0) && myLine.Selected(Input.mousePosition)) {print ("1");}//controller.ClickLink(this);}
			
		}*/
		/*
		void OnMouseDown()
		{
            controller.ClickLink(this);
		}*/
	}

}