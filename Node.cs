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

	public class Node : MonoBehaviour {

		public string id;
		public TextMesh nodeText;
		public GameController controller;
		public Vector3 myPos;
		
		public bool selected
		{
			get{return _selected;}
			set
			{
				if (value) renderer.material.color=Color.blue; 
				else renderer.material.color=Color.white;//new Color(22,70,109,255);//Color.blue;
				_selected=value; 
			}
		
		}
		bool _selected;
		public Sprite[] sprites;
		int currentSprite=0;
		
		bool dragged=false;
		
		void Start()
		{
			SetSprite(currentSprite);
			//gameObject.isStatic=true;
		}
		
		void Update () {
			//node text always facing camera
			transform.LookAt (Camera.main.transform);
			nodeText.transform.LookAt (Camera.main.transform);
		}
		
		
		void OnMouseDown()
		{
			//Camera.main.gameObject.GetComponent<CameraControlZeroG>().controller
			controller.ClickNode(this);
			dragged=true;
			StartCoroutine(DragRoutine());
		}
		
		IEnumerator DragRoutine()
		{
			float i=0;
			//Wait for an 0.2 of a second mouse button hold 
			while (i<0.2 && dragged) 
			{
				i+=Time.deltaTime;
				yield return new WaitForFixedUpdate();
			}
			//do drag until mouse is let go
			while (dragged) 
			{
				float z=0;
				z=Camera.main.WorldToScreenPoint(transform.position).z;
				transform.position=Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x,Input.mousePosition.y,z));
				//call controller to align links
				yield return new WaitForFixedUpdate();
			}
			controller.DragNode(this);
			yield break;
		}
		
		void OnMouseUp()
		{
			dragged=false;
		}
		
		public void SetSprite(int spriteNum)
		{
			gameObject.GetComponent<SpriteRenderer>().sprite=sprites[spriteNum];
			currentSprite=spriteNum;
		}
		
		public int GetSpriteIndex()
		{return currentSprite;}
	}

}