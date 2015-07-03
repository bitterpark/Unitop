

using UnityEngine;
using System.Collections;

namespace Topology {

	public class Node : MonoBehaviour {

		public string id;
		public TextMesh nodeText;
		public GameController controller;
		//public Vector3 myPos;
		
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
			//controller.ClickNode(this,true);
			controller.ClickedNodeAction(this,true);
			//print ("drag routine started!");
			//StartCoroutine(DragRoutine());
		}
		
		void OnMouseDrag()
		{
			//Wait for mouse hold for 0.2 sec
			
			if (!dragged)
			{
				/*
				if (myTimer==null) {myTimer=new TimerDetector(0.1f);}
				else 
				{
					if (myTimer.UpdateTimer()) 
					{
						dragged=true;
						controller.NodeDragStart();
						myTimer=null;
					}
				}*/
				
				//Vector3 mouseProjection=(Camera.main.ScreenToWorldPoint(Input.mousePosition));
				Vector3 myPosProjection=Camera.main.WorldToScreenPoint(transform.position);
				//print ("Start pos proj:"+myPosProjection);
				myPosProjection.z=0;
				//print ("Corrected pos proj:"+myPosProjection);
				//print ("Mouse pos:"+Input.mousePosition);
				
				//mouseProjection.z=3000;
				//float projectedDragTolerance=50f*Screen.height/(Camera.main.orthographicSize*2);
				if ((Input.mousePosition-myPosProjection).magnitude>50)//projectedDragTolerance) 
				{
					print ("Magn: "+(Input.mousePosition-myPosProjection).magnitude);
					controller.NodeDragStart();
					dragged=true;
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
		
		void OnDestroy() 
		{
			if (dragged) {controller.NodeDragComplete();}
			StopAllCoroutines();
		}
		
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
			if (dragged) {controller.NodeDragComplete();}
			controller.ClickedNodeAction(this,dragged);
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