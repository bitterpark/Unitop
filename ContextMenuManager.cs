using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using Topology;

public class ContextMenuManager{
	
	Rect contextMenuPosNodes=new Rect(5,125,310,70);
	Rect contextMenuPosLinks=new Rect(5,125,155,70);
	Vector3 linksMenuPreZoomWorldPosition;
	float contextMenuNodesWidthSingle=310; //Must be equal to contextMenuPosNodes start width
	float contextMenuNodesWidthMulti=155;
	Vector3 nodesMenuPreZoomWorldPosition;//=Vector3.zero;
	public Rect GetContextMenuPosNodes() {return contextMenuPosNodes;}
	public Rect GetContextMenuPosLinks() {return contextMenuPosLinks;}
	public bool isDrawn=false;
	
	int tooltipMode
	{
		get {return _tooltipMode;}
		set 
		{
			if (value!=_tooltipMode) 
			{
				//lastSelectedLinkNode=null;
				nameEditFieldInFocus=false;
				if (value==0) {lastSelectedLink=null;}
				if (value==1) {}//lastSelectedNode=null;}
			}
			_tooltipMode=value;
		}
		
	}
	int _tooltipMode=0;
	public void SetTooltipMode(int newMode) {tooltipMode=newMode;}
	
	string editedNodeName="";
	bool nameEditFieldInFocus=false;
	
	
	
	public Popup selectItemDroplist=new Popup();
	public Popup selectColorDroplist=new Popup();
	public Popup selectIconDroplist=new Popup();
	public Texture[] colorTextures;
	public GUISkin currentSkin;
	Link lastSelectedLink=null;
	
	public ContextMenuManager(GUISkin mySkin, Texture[] linkColorTextures)
	{
		currentSkin=mySkin;
		colorTextures=linkColorTextures;
		InputManager.mainInputManager.SelectedNodesChanged+=AdjustToNodeSelectionChanges;
		InputManager.mainInputManager.myNodeList.JumpedToNode+=AnchorToLastSelectedNode;
		InputManager.mainInputManager.NodeDragEnded+=AnchorToLastSelectedNode;
		InputManager.mainInputManager.SelectedLinksChanged+=AnchorToLastSelectedLink;
		InputManager.mainInputManager.SelectionBoxChanged+=AnchorToSelectionBox;
		//GameController.mainController.mainCameraControl.PreZoomChanged+=PreZoomSetup;
		//GameController.mainController.mainCameraControl.ZoomChanged+=AdjustToZoom;
		Camera.main.GetComponent<CameraControlZeroG>().PreZoomChanged+=PreZoomSetup;
		Camera.main.GetComponent<CameraControlZeroG>().ZoomChanged+=AdjustToZoom;
		//CameraControlZeroG.mainCameraControl.ZoomChanged+=AdjustToZoom;
		//CameraControlZeroG.mainCameraControl.PreZoomChanged+=PreZoomSetup;
	}
	
	void AdjustToNodeSelectionChanges()
	{
		nameEditFieldInFocus=false;
		AnchorToLastSelectedNode();
	}
	
	void AnchorToLastSelectedNode()
	{
		if (InputManager.mainInputManager.GetSelectedNodes().Count==1)//if (InputManager.mainInputManager.GetSelectedNodes().Count>0)
		{
			//For new small window system
			Node lastSelectedNode=InputManager.mainInputManager.GetSelectedNodes()[InputManager.mainInputManager.GetSelectedNodes().Count-1];
			Vector3 screenPos=Camera.main.WorldToScreenPoint(lastSelectedNode.transform.position);
			contextMenuPosNodes.x=screenPos.x;
			//Tranform from screen space to UI space
			contextMenuPosNodes.y=Screen.height-screenPos.y;
			contextMenuPosNodes.y-=contextMenuPosNodes.height*0.2f;
			float menuOffsetFromNode=50f;
			float maxAllowedXCoord=Screen.width-InputManager.mainInputManager.myNodeList.GetNodeListRect().width;
			if (InputManager.mainInputManager.GetSelectedNodes().Count==1) {maxAllowedXCoord-=contextMenuNodesWidthSingle;}
			else {maxAllowedXCoord-=contextMenuNodesWidthMulti;}
			if (contextMenuPosNodes.x+menuOffsetFromNode<=maxAllowedXCoord) {contextMenuPosNodes.x+=menuOffsetFromNode;}
			else {contextMenuPosNodes.x=maxAllowedXCoord;}
			//if (contextMenuPosNodes.x-(contextMenuPosNodes.width+menuOffsetFromNode)>0) {contextMenuPosNodes.x-=contextMenuPosNodes.width+menuOffsetFromNode;}
			//else {contextMenuPosNodes.x+=menuOffsetFromNode;}
		}
	}
	
	void AnchorToSelectionBox(float x, float y, float maxX, float maxY)
	{
		//For new small window system
		//Node lastSelectedNode=InputManager.mainInputManager.GetSelectedNodes()[InputManager.mainInputManager.GetSelectedNodes().Count-1];
		if (InputManager.mainInputManager.GetSelectedNodes().Count>1)
		{
			Vector3 screenPos=new Vector3(Mathf.Max(x,maxX),Mathf.Max(y,maxY),0);//Camera.main.WorldToScreenPoint(lastSelectedNode.transform.position);
			contextMenuPosNodes.x=screenPos.x-contextMenuPosNodes.width+5;
			//Tranform from screen space to UI space
			contextMenuPosNodes.y=Screen.height-screenPos.y;
			contextMenuPosNodes.y-=contextMenuPosNodes.height+10;
			//float menuXOffsetFromPos=50f;
			//float menuYOffsetFromPos=20f;
			//float maxAllowedXCoord=Screen.width-InputManager.mainInputManager.myNodeList.GetNodeListRect().width;
			//maxAllowedXCoord-=contextMenuNodesWidthMulti;
			//if (contextMenuPosNodes.x+menuXOffsetFromPos<=maxAllowedXCoord) {contextMenuPosNodes.x+=menuXOffsetFromPos;}
			//else {contextMenuPosNodes.x=maxAllowedXCoord;}
		}
	}
	
	void AnchorToLastSelectedLink()
	{
		if (InputManager.mainInputManager.GetSelectedLinks().Count>0)
		{	
			Link lastSelectedLink=InputManager.mainInputManager.GetSelectedLinks()[InputManager.mainInputManager.GetSelectedLinks().Count-1];
			Vector3 screenPos=Camera.main.WorldToScreenPoint(lastSelectedLink.target.transform.position+(lastSelectedLink.source.transform.position-lastSelectedLink.target.transform.position)*0.5f);//.transform.position);
			contextMenuPosLinks.x=screenPos.x;
			//Tranform from screen space to UI space
			contextMenuPosLinks.y=Screen.height-screenPos.y;
			contextMenuPosLinks.y-=contextMenuPosLinks.height*0.2f;
			float menuOffsetFromLink=50f;
			float maxAllowedXCoord=Screen.width-InputManager.mainInputManager.myNodeList.GetNodeListRect().width-contextMenuPosLinks.width;
			//if (InputManager.mainInputManager.GetSelectedLinks().Count==1) {maxAllowedXCoord-=contextMenuNodesWidthSingle;}
			//else {maxAllowedXCoord-=contextMenuNodesWidthMulti;}
			if (contextMenuPosLinks.x+menuOffsetFromLink<=maxAllowedXCoord) {contextMenuPosLinks.x+=menuOffsetFromLink;}
			else {contextMenuPosLinks.x=maxAllowedXCoord;}
		}
	
	}
	
	void PreZoomSetup()
	{
		Vector2 myGUICoordsToScreen=new Vector2(contextMenuPosNodes.x,Screen.height-contextMenuPosNodes.y);
		nodesMenuPreZoomWorldPosition=Camera.main.ScreenToWorldPoint(myGUICoordsToScreen);
		myGUICoordsToScreen=new Vector2(contextMenuPosLinks.x,Screen.height-contextMenuPosLinks.y);
		linksMenuPreZoomWorldPosition=Camera.main.ScreenToWorldPoint(myGUICoordsToScreen);
	}
	
	void AdjustToZoom()
	{
		//for nodes menu
		Vector2 myNewScreenCoords=Camera.main.WorldToScreenPoint(nodesMenuPreZoomWorldPosition);
		contextMenuPosNodes.x=myNewScreenCoords.x;
		contextMenuPosNodes.y=Screen.height-myNewScreenCoords.y;
		
		//for links menu
		myNewScreenCoords=Camera.main.WorldToScreenPoint(linksMenuPreZoomWorldPosition);
		contextMenuPosLinks.x=myNewScreenCoords.x;
		contextMenuPosLinks.y=Screen.height-myNewScreenCoords.y;
	}
	
	public void ManageTooltip(bool isSupressed)
	{
		int ttMode=0;
		if (InputManager.mainInputManager.GetSelectedNodes().Count>0) {ttMode=1;}
		if (InputManager.mainInputManager.GetSelectedLinks().Count>0) {ttMode=2;}
		if (ttMode!=0 && !isSupressed) 
		{
			isDrawn=true;
			TooltipWindow(ttMode);
		} 
		else {isDrawn=false;}
	}
	
	void TooltipWindow(int mode)
	{
		if (mode==1) 
		{
			
			Rect nodeWindowRect=new Rect( contextMenuPosNodes.x,contextMenuPosNodes.y,contextMenuPosNodes.width,20);
			nodeWindowRect=GUI.Window (1,nodeWindowRect,DragWindowFunc,"",GUIStyle.none);
			contextMenuPosNodes.x=nodeWindowRect.x;
			contextMenuPosNodes.y=nodeWindowRect.y;
			DrawTooltip(mode);
		}
		else 
		{
			Rect linkWindowRect=new Rect( contextMenuPosLinks.x,contextMenuPosLinks.y,contextMenuPosNodes.width,20);
			linkWindowRect=GUI.Window (2,linkWindowRect,DragWindowFunc,"",GUIStyle.none);
			contextMenuPosLinks.x=linkWindowRect.x;
			contextMenuPosLinks.y=linkWindowRect.y;
			DrawTooltip(mode);
		}
	}
	
	void DragWindowFunc(int emptySig) 
	{
		//Make window draggable
		GUI.DragWindow();
	}
	
	void DrawTooltip(int mode)
	{
		float elementSizeX=110;
		float elementSizeY=30;
		//This denotes pad relative to window border
		float leftColumnStartX=5;//contextMenuPosLinks.x+20;//contextMenuPosLinks.x+20;
		float leftColumnStartY=30;//contextMenuPosLinks.y+15;//contextMenuPosLinks.y+15;
		float rightColumnStartX=elementSizeX+40;//contextMenuPosLinks.x+160;//contextMenuPosLinks.x+160;
		float rightColumnStartY;
		//float rightColumnStartY=leftColumnStartY;//contextMenuPosLinks.y+15;//contextMenuPosLinks.y+15;
		float vPad=1;
		
		
		//nodes tooltip
		if (mode==1)
		{
			List<Node> selected=InputManager.mainInputManager.GetSelectedNodes();
			
			//print("context:"+contextMenuPosNodes);
			
			leftColumnStartX+=contextMenuPosNodes.x;
			leftColumnStartY+=contextMenuPosNodes.y;
			rightColumnStartX+=leftColumnStartX;
			rightColumnStartY=leftColumnStartY;
			if (selected.Count==1)
			{
				contextMenuPosNodes.width=contextMenuNodesWidthSingle;
			} else {contextMenuPosNodes.width=contextMenuNodesWidthMulti;}
			GUI.Box(contextMenuPosNodes,"Контекстное меню",new GUIStyle("window"));
			//check if last select is still in the list
			/*
			if (InputManager.mainInputManager.GetSelectedNodes().Contains(lastSelectedNode))//(Node)lastSelectedLinkNode))
			{
				selectItemDroplist.SetSelectedItemIndex(InputManager.mainInputManager.GetSelectedNodes().IndexOf(lastSelectedNode));//((Node)lastSelectedLinkNode));
			} else {selectItemDroplist.SetSelectedItemIndex(0);}
			*/
			//Generate droplist content
			GUIContent[] droplistContent=new GUIContent[selected.Count];
			for (int i=0; i<selected.Count; i++)
			{
				droplistContent[i]=new GUIContent(selected[i].id);
			}
			
			//Main box and labels
			//GUI.Box(contextMenuPosNodes,"",currentSkin.box);
			
			/*
			GUI.Label (new Rect(leftColumnStartX,leftColumnStartY+elementSizeY+vPad*2,elementSizeX,elementSizeY),"Выбор элемента:");
			GUI.Label (new Rect(leftColumnStartX,leftColumnStartY,elementSizeX,elementSizeY),"Имя элемента:");
			GUI.Label (new Rect(rightColumnStartX,rightColumnStartY+elementSizeY+vPad*2,elementSizeX,elementSizeY),"Иконка:");
			*/
			
			//NAME EDIT FIELD
			if (selected.Count==1)
			{
				Node selectedNode=selected[selectItemDroplist.GetSelectedItemIndex()];
				//string nodeName=selected[selectItemDroplist.GetSelectedItemIndex()].text;
				//string oldName=nodeName;
				string nameDisplayedInEditField="";
				if (nameEditFieldInFocus)
				{
					nameDisplayedInEditField=editedNodeName;
				} else {nameDisplayedInEditField=selectedNode.text;}
				GUI.SetNextControlName("NameField");
				editedNodeName=GUI.TextField(new Rect(rightColumnStartX,rightColumnStartY+3,elementSizeX*1.3f,elementSizeY),nameDisplayedInEditField,25);
				if (GUI.GetNameOfFocusedControl() == "NameField") 
				{
					//if focus was set this frame
					if (!nameEditFieldInFocus)
					{
						editedNodeName=selectedNode.text;
					}
					nameEditFieldInFocus=true;
				} 
				else 
				{			
					//if focus was lost this frame
					if (nameEditFieldInFocus) 
					{
						if (editedNodeName!=selectedNode.text) 
						{
							//if pseudonode or if the scene is not generated from DB - assign name without tests
							if (!selectedNode.hostNode | !GameController.mainController.occupiedWorkspaceIpsInDB.ContainsKey(selectedNode.dbWorkspaceid)) 
							{
								selectedNode.text=editedNodeName;
								GameController.mainController.unsavedChangesExist=true;
							}
							else
							{
								//if real node from DB - test name for proper, unoccupied ip
								IPAddress outAddress;
								if (IPAddress.TryParse(editedNodeName,out outAddress))//.TryParse(editedNodeName,out outAddress))
								{
									string newIp=outAddress.ToString();
									//see if check is necessary
										//check if ip is unique within the workspace
									if (!GameController.mainController.occupiedWorkspaceIpsInDB[selectedNode.dbWorkspaceid].Contains(newIp))
									{	
										//if (!GameController.mainController.occupiedWorkspaceIpsInDB.ContainsKey(selectedNode.dbWorkspaceid)) 
										//{GameController.mainController.occupiedWorkspaceIpsInDB.Add (selectedNode.dbWorkspaceid,new List<string>());}
										//mark new ip as occupied
										GameController.mainController.occupiedWorkspaceIpsInDB[selectedNode.dbWorkspaceid].Add(newIp);
										if (GameController.mainController.occupiedWorkspaceIpsInDB[selectedNode.dbWorkspaceid].Contains(selectedNode.text))
										{GameController.mainController.occupiedWorkspaceIpsInDB[selectedNode.dbWorkspaceid].Remove(selectedNode.text);}
										selectedNode.text=newIp;
										selectedNode.changesMade=true;
										GameController.mainController.unsavedChangesExist=true;
									}
									else {InputManager.mainInputManager.StartContextNameErrorPopup(1);}
								} 
								else 
								{
									InputManager.mainInputManager.StartContextNameErrorPopup(0);
									editedNodeName=selectedNode.text;
								}
							}
						}
						
						
					}
					nameEditFieldInFocus=false;
				}
				//if (nodeName!=oldName) {GameController.mainController.unsavedChangesExist=true;}
				//selected[selectItemDroplist.GetSelectedItemIndex()].text=nodeName;
			}		
			//SELECT OBJ MENU (must be last item rendered)
			/*
			selectItemDroplist.List(new Rect(leftColumnStartX,leftColumnStartY+elementSizeY*1.6f+vPad*2,elementSizeX,elementSizeY)//elementSizeY+100)
			                        ,droplistContent,"box",currentSkin.customStyles[1]);
			lastSelectedNode=selected[selectItemDroplist.GetSelectedItemIndex()];
			*/
			
			//set current icon selection
			Node currentNode=selected[selectItemDroplist.GetSelectedItemIndex()];
			selectIconDroplist.SetSelectedItemIndex(currentNode.GetSpriteIndex());
			//generate icon droplist content
			Texture[] cachedNodeTextures=GameController.mainController.GetNodeTextures();
			//GUIContent[] iconDroplistContent=new GUIContent[cachedNodeTextures.Length];//new GUIContent[4];
			List<GUIContent> iconDroplistContent=new List<GUIContent>();
			iconDroplistContent.Add (new GUIContent("WindowsXP   ",cachedNodeTextures[0]));
			iconDroplistContent.Add (new GUIContent("WindowsVista",cachedNodeTextures[1]));
			iconDroplistContent.Add (new GUIContent("Windows7    ",cachedNodeTextures[2]));
			iconDroplistContent.Add (new GUIContent("Windows8    ",cachedNodeTextures[3]));
			iconDroplistContent.Add (new GUIContent("Server2000  ",cachedNodeTextures[4]));
			iconDroplistContent.Add (new GUIContent("Server2003  ",cachedNodeTextures[5]));
			iconDroplistContent.Add (new GUIContent("Server2008  ",cachedNodeTextures[6]));
			iconDroplistContent.Add (new GUIContent("Server2012  ",cachedNodeTextures[7]));
			iconDroplistContent.Add (new GUIContent("Linux       ",cachedNodeTextures[8]));
			iconDroplistContent.Add (new GUIContent("MacOS       ",cachedNodeTextures[9]));
			
			//select icon droplist
			selectIconDroplist.List(new Rect(leftColumnStartX,leftColumnStartY,elementSizeX*1.3f,elementSizeY)
			                        ,iconDroplistContent,"box",currentSkin.customStyles[5],currentSkin.customStyles[6]);
			//Set new icon for all selected nodes
			if (selectIconDroplist.SelectionWasMade())
			{
				//InputManager.DebugPrint("changing sprites!"); //print("changing sprites!");
				foreach (Node node in selected) {node.SetSprite(selectIconDroplist.GetSelectedItemIndex());}
				GameController.mainController.unsavedChangesExist=true;
			}				
		}
		
		//links tooltip
		if (mode==2) 
		{
			leftColumnStartX+=contextMenuPosLinks.x;
			leftColumnStartY+=contextMenuPosLinks.y;
			rightColumnStartX+=leftColumnStartX;
			rightColumnStartY=leftColumnStartY;
			GUI.Box(contextMenuPosLinks,"Контекстное меню",new GUIStyle("window"));
			//check if last select is still in the list
			/*
			if (InputManager.mainInputManager.GetSelectedLinks().Contains(lastSelectedLink))//(Link)lastSelectedLinkNode))
			{
				selectItemDroplist.SetSelectedItemIndex(InputManager.mainInputManager.GetSelectedLinks().IndexOf(lastSelectedLink));//(Link)lastSelectedLinkNode));
			} else {selectItemDroplist.SetSelectedItemIndex(0);}*/
			
			
			//Generate droplist content
			List<GUIContent> droplistContent=new List<GUIContent>();
			for (int i=0; i<InputManager.mainInputManager.GetSelectedLinks().Count; i++)
			{
				//droplistContent[i]=new GUIContent(InputManager.mainInputManager.GetSelectedLinks()[i].id);
				droplistContent.Add (new GUIContent(InputManager.mainInputManager.GetSelectedLinks()[i].id));
			}	
			
			//main box and left hand labels
			//GUI.Box(contextMenuPosLinks,"");
			
			//GUI.Label (new Rect(leftColumnStartX,leftColumnStartY,elementSizeX,elementSizeY),"Выбор элемента:");
			//GUI.Label (new Rect(rightColumnStartX,rightColumnStartY,elementSizeX,elementSizeY),"Цвет элемента:");
			//GUI.EndGroup();
			
			//Set current color select
			Link coloredLink=InputManager.mainInputManager.GetSelectedLinks()[selectItemDroplist.GetSelectedItemIndex()];
			selectColorDroplist.SetSelectedItemIndex(coloredLink.GetColorIndex());
			//Generate droplist content
			//GUIContent[] droplistColorContent=new GUIContent[5];
			List <GUIContent> droplistColorContent=new List<GUIContent>();
			droplistColorContent.Add(new GUIContent(colorTextures[0]));//"black");
			droplistColorContent.Add(new GUIContent(colorTextures[1]));//"red");
			droplistColorContent.Add(new GUIContent(colorTextures[2]));//"green");
			droplistColorContent.Add(new GUIContent(colorTextures[3]));//"yellow");
			droplistColorContent.Add(new GUIContent(colorTextures[4]));//"cyan");
			
			/*
			GUIContent currentSelected=droplistColorContent[selectColorDroplist.GetSelectedItemIndex()];
			droplistColorContent.RemoveAt(selectColorDroplist.GetSelectedItemIndex());
			droplistColorContent.Insert(0,currentSelected);
			*/
			//Draw color droplist
			int droplistPick=selectColorDroplist.List(new Rect(leftColumnStartX,leftColumnStartY,elementSizeX*1.3f,elementSizeY)//elementSizeY)
			                                          ,droplistColorContent,"box",currentSkin.customStyles[1]); 
			                                         
			if (selectColorDroplist.SelectionWasMade())
			{
				foreach (Link selectedLink in InputManager.mainInputManager.GetSelectedLinks())
				{
					switch (droplistPick)
					{
						case 0:{selectedLink.color="black"; break;}
						case 1:{selectedLink.color="red"; break;}
						case 2:{selectedLink.color="green"; break;}
						case 3:{selectedLink.color="yellow"; break;}
						case 4:{selectedLink.color="cyan"; break;}
					}
				}
				GameController.mainController.unsavedChangesExist=true;
			}
			/*
			//select obj menu (must be after endgroup rendered)
			selectItemDroplist.List(new Rect(leftColumnStartX,leftColumnStartY+elementSizeY*0.6f,elementSizeX,elementSizeY)//elementSizeY)
			                        ,droplistContent,"box",currentSkin.customStyles[1]);
			lastSelectedLink=InputManager.mainInputManager.GetSelectedLinks()[selectItemDroplist.GetSelectedItemIndex()];
			*/
		}
	}
	
	
}
