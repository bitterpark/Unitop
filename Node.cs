

using UnityEngine;
using System.Collections;

namespace Topology {

	public class Node : MonoBehaviour {

		public string id;
		public TextMesh nodeText;
		public string text
		{
			get {return _text;}
			set {_text=value; nodeText.text=osText+"\n"+_text;}
		}
		string _text="";
		string osText="";
		public GameController controller;
		Vector3 beginDragMousePos=Vector3.zero;
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
		bool clickActionDone=false;
		public bool hasChildren
		{
			get {return _hasChildren;}
			set 
			{
				_hasChildren=value;	
				if (!_hasChildren) {unfoldChildren=false;}
			}
		}
		bool _hasChildren=false;
		public bool unfoldChildren=false;
		//public bool hasParent
		
		public Node parentNode=null;
		
		public Texture[] textures;
		int currentSprite
		{
			get {return _currentSprite;}
			set 
			{
				_currentSprite=value;
				switch (_currentSprite)
				{
					case 0:{osText="XP";break;}
					case 1:{osText="Vista";break;}
					case 2:{osText="7";break;}
					case 3:{osText="8";break;}
					case 4:{osText="2000";break;}
					case 5:{osText="2003";break;}
					case 6:{osText="2008";break;}
					case 7:{osText="2012";break;}
					case 8:{osText="Linux";break;}
					case 9:{osText="Mac";break;}	
				}
				text=text;
			}
		}
		int _currentSprite=0;
		
		bool dragged=false;
		TimerDetector myTimer=null;
		
		void Start()
		{
			SetSprite(currentSprite);
		}
		
		
		void OnMouseDown()
		{
			//Make sure node can't be dragged through GUI
			if (!InputManager.mainInputManager.ClickedOnGUI())
			{
				clickActionDone=InputManager.mainInputManager.ClickNode(this,false);
				//if (InputManager.mainInputManager.GetSelectedNodes().Contains(this)) {}
				//else {controller.myInputManager.ClickedNodeAction(this,false,false); clickActionDone=true;} 
				//controller.myInputManager.ClickedNodeAction(this,false,true);}	
				beginDragMousePos=Input.mousePosition;
			}
		}
		
		void OnMouseDrag()
		{
			if (!InputManager.mainInputManager.ClickedOnGUI())
			{
				if (!dragged)
				{
					//Make sure the drag was initiated and init click didn't land on GUI
					if ((Input.mousePosition-beginDragMousePos).magnitude>25 && beginDragMousePos!=Vector3.zero)
					{
						controller.myInputManager.NodeDragStart();
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
					controller.myInputManager.DragNode(this,delta);//controller.DragNode(this,delta);
				}
			} else {Release ();}
		}
		
		void Release()
		{
			if (dragged) 
			{
				controller.myInputManager.NodeDragComplete();
				beginDragMousePos=Vector3.zero;
			}
			if (!clickActionDone) {controller.myInputManager.ClickedNodeAction(this,false,dragged);}
			clickActionDone=false;
			dragged=false;
			myTimer=null;
		}
		
		void OnDestroy() 
		{
			if (dragged) {controller.myInputManager.NodeDragComplete();}
			//StopAllCoroutines();
		}
		
		public void DragAlong(Vector3 moveDelta)
		{
			moveDelta.z=0;
			transform.position+=moveDelta;
			//print ("applying dragalong!");
		}
		
		void OnMouseUp()
		{
			Release();
		}
		
		public void SetSprite(int spriteNum)
		{
			if (spriteNum<controller.nodeIconTextures.Length && spriteNum>=0)
			{
				renderer.material.SetTexture(0,controller.nodeIconTextures[spriteNum]);
				currentSprite=spriteNum;
			}
			else 
			{
				renderer.material.SetTexture(0,textures[0]);
				currentSprite=0;
			}
		}
		
		
		public int GetSpriteIndex()
		{return currentSprite;}
	}

}