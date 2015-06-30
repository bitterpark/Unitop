

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
				if (value) renderer.material.color=Color.blue;//renderer.material.color=Color.blue; 
				else renderer.material.color=Color.white;//new Color(22,70,109,255);//Color.blue;
				_selected=value; 
			}
		
		}
		bool _selected;
		public Texture[] textures;
		int currentSprite=0;
		
		bool dragged=false;
		TimerDetector myTimer=null;
		
		void Start()
		{
			SetSprite(currentSprite);
			//renderer.sharedMaterial.SetTexture(0,currentSprite);
			//gameObject.isStatic=true;
		}
		
		void Update () {
			//node text always facing camera
			//transform.LookAt (Camera.main.transform);
			//nodeText.transform.LookAt (Camera.main.transform);
			//print ("fucknuts");
		}
		
		
		void OnMouseDown()
		{
			//Camera.main.gameObject.GetComponent<CameraControlZeroG>().controller
			//dragged=true;
			
//			controller.StartDragNode();
			controller.ClickNode(this,true);
			//print ("drag routine started!");
			//StartCoroutine(DragRoutine());
		}
		
		void OnMouseDrag()
		{
			//Wait for mouse hold for 0.2 sec
			if (!dragged)
			{
				if (myTimer==null) {myTimer=new TimerDetector(0.2f);}
				else 
				{
					if (myTimer.UpdateTimer()) 
					{
						dragged=true;
						myTimer=null;
					}
				}
			}
			else
			{
				//if mouse was held, activate drag mode
				Vector3 delta=Vector3.zero;
				delta=transform.position;
				float z=0;
				z=3000;//Camera.main.WorldToScreenPoint(transform.position).z;
				transform.position=Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x,Input.mousePosition.y,z));
				delta=transform.position-delta;
				//Call controller to align links and dragalong nodes
				controller.DragNode(this,delta);
			}
		}
		
		/*
		IEnumerator DragRoutine()
		{
			float i=0;
			Vector3 delta=Vector3.zero;
			//Wait for an 0.2 of a second mouse button hold 
			while (i<0.1) 
			{
				i+=Time.deltaTime;
				yield return new WaitForFixedUpdate();
			}
			
			if (!dragged) {}
			else
			{ 
				//do drag until mouse is let go
				while (dragged) 
				{
					//print ("start transform: "+transform.position);
					delta=transform.position;
					float z=0;
					z=3000;//Camera.main.WorldToScreenPoint(transform.position).z;
					transform.position=Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x,Input.mousePosition.y,z));
					//print ("new transform: "+transform.position);
					delta=transform.position-delta;
					//print ("delta: "+delta);
					//print ("applying move!");
					controller.DragNode(this,delta);
					//call controller to align links
					yield return new WaitForFixedUpdate();
				}
			}
			yield break;
		}*/
		
		public void DragAlong(Vector3 moveDelta)
		{
			moveDelta.z=0;
			transform.position+=moveDelta;
			//print ("applying dragalong!");
		}
		
		void OnMouseUp()
		{
			//if (dragged) {}
			//dragged=false;
			controller.ClickNode(this,dragged);
			dragged=false;
			myTimer=null;
		}
		
		public void SetSprite(int spriteNum)
		{
			//gameObject.GetComponent<SpriteRenderer>().sprite=sprites[spriteNum];
			renderer.material.SetTexture(0,textures[spriteNum]);
			currentSprite=spriteNum;
		}
		
		public int GetSpriteIndex()
		{return currentSprite;}
	}

}