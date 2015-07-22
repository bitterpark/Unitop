﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Topology;

public class ContextMenuManager{
	
	Rect contextMenuPosNodes=new Rect(5,125,310,70);
	Rect contextMenuPosLinks=new Rect(5,125,350,100);
	float contextMenuNodesWidthSingle=310; //Must be equal to contextMenuPosNodes start width
	float contextMenuNodesWidthMulti=155;
	Vector3 preZoomWorldPosition;//=Vector3.zero;
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
				if (value==0) {lastSelectedLink=null;}
				if (value==1) {}//lastSelectedNode=null;}
			}
			_tooltipMode=value;
		}
		
	}
	int _tooltipMode=0;
	public void SetTooltipMode(int newMode) {tooltipMode=newMode;}
	
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
		InputManager.mainInputManager.SelectedNodesChanged+=AnchorToLastSelectedNode;
		//GameController.mainController.mainCameraControl.PreZoomChanged+=PreZoomSetup;
		//GameController.mainController.mainCameraControl.ZoomChanged+=AdjustToZoom;
		Camera.main.GetComponent<CameraControlZeroG>().PreZoomChanged+=PreZoomSetup;
		Camera.main.GetComponent<CameraControlZeroG>().ZoomChanged+=AdjustToZoom;
		//CameraControlZeroG.mainCameraControl.ZoomChanged+=AdjustToZoom;
		//CameraControlZeroG.mainCameraControl.PreZoomChanged+=PreZoomSetup;
	}
	
	void AnchorToLastSelectedNode()
	{
		if (InputManager.mainInputManager.GetSelectedNodes().Count>0)
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
	
	void PreZoomSetup()
	{
		Vector2 myGUICoordsToScreen=new Vector2(contextMenuPosNodes.x,Screen.height-contextMenuPosNodes.y);
		preZoomWorldPosition=Camera.main.ScreenToWorldPoint(myGUICoordsToScreen);
	}
	
	void AdjustToZoom()
	{
		Vector2 myNewScreenCoords=Camera.main.WorldToScreenPoint(preZoomWorldPosition);
		contextMenuPosNodes.x=myNewScreenCoords.x;
		contextMenuPosNodes.y=Screen.height-myNewScreenCoords.y;
		//Vector2 myAdjustedGUICoords=new Vector2 ();
		//transform guicoords y to screenspace y, then transform all to world
		//Vector2 myGUICoordsToScreen=new Vector2(contextMenuPosNodes.x,Screen.height-contextMenuPosNodes.y);
		//Vector3 myWorldPoint=Camera.main.ScreenToWorldPoint(myGUICoordsToScreen);
		//Vector3 cursorWorldDelta=preZoomWorldPosition-myWorldPoint;
		//Camera.main.transform.position+=cursorWorldDelta;
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
		} else {isDrawn=false;}
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
				string nodeName=selected[selectItemDroplist.GetSelectedItemIndex()].text;
				nodeName=GUI.TextField(new Rect(rightColumnStartX,rightColumnStartY+3,elementSizeX*1.3f,elementSizeY),nodeName);
				selected[selectItemDroplist.GetSelectedItemIndex()].text=nodeName;
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
			iconDroplistContent.Add (new GUIContent("Windows XP   ",cachedNodeTextures[0]));
			iconDroplistContent.Add (new GUIContent("Windows Vista",cachedNodeTextures[1]));
			iconDroplistContent.Add (new GUIContent("Windows 7    ",cachedNodeTextures[2]));
			iconDroplistContent.Add (new GUIContent("Windows 8    ",cachedNodeTextures[3]));
			iconDroplistContent.Add (new GUIContent("Server 2000  ",cachedNodeTextures[4]));
			iconDroplistContent.Add (new GUIContent("Server 2003  ",cachedNodeTextures[5]));
			iconDroplistContent.Add (new GUIContent("Server 2008  ",cachedNodeTextures[6]));
			iconDroplistContent.Add (new GUIContent("Server 2012  ",cachedNodeTextures[7]));
			iconDroplistContent.Add (new GUIContent("Linux        ",cachedNodeTextures[8]));
			iconDroplistContent.Add (new GUIContent("Mac OS       ",cachedNodeTextures[9]));
			/*
			iconDroplistContent[0]=new GUIContent("Windows XP   ",cachedNodeTextures[0]);
			iconDroplistContent[1]=new GUIContent("Windows Vista",cachedNodeTextures[1]);
			iconDroplistContent[2]=new GUIContent("Windows 7    ",cachedNodeTextures[2]);
			iconDroplistContent[3]=new GUIContent("Windows 8    ",cachedNodeTextures[3]);
			iconDroplistContent[4]=new GUIContent("Server 2000  ",cachedNodeTextures[4]);
			iconDroplistContent[5]=new GUIContent("Server 2003  ",cachedNodeTextures[5]);
			iconDroplistContent[6]=new GUIContent("Server 2008  ",cachedNodeTextures[6]);
			iconDroplistContent[7]=new GUIContent("Server 2012  ",cachedNodeTextures[7]);
			iconDroplistContent[8]=new GUIContent("Linux        ",cachedNodeTextures[8]);
			iconDroplistContent[9]=new GUIContent("Mac OS       ",cachedNodeTextures[9]);
			*/
			
			/*
			GUIContent currentSelected=iconDroplistContent[selectIconDroplist.GetSelectedItemIndex()];
			iconDroplistContent.RemoveAt(selectIconDroplist.GetSelectedItemIndex());
			iconDroplistContent.Insert(0,currentSelected);
			*/
			//select icon droplist
			selectIconDroplist.List(new Rect(leftColumnStartX,leftColumnStartY,elementSizeX*1.3f,elementSizeY)
			                        ,iconDroplistContent,"box",currentSkin.customStyles[5],currentSkin.customStyles[6]);
			//Set new icon for all selected nodes
			if (selectIconDroplist.SelectionWasMade())
			{
				//InputManager.DebugPrint("changing sprites!"); //print("changing sprites!");
				foreach (Node node in selected) {node.SetSprite(selectIconDroplist.GetSelectedItemIndex());}
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
			if (InputManager.mainInputManager.GetSelectedLinks().Contains(lastSelectedLink))//(Link)lastSelectedLinkNode))
			{
				selectItemDroplist.SetSelectedItemIndex(InputManager.mainInputManager.GetSelectedLinks().IndexOf(lastSelectedLink));//(Link)lastSelectedLinkNode));
			} else {selectItemDroplist.SetSelectedItemIndex(0);}
			//Generate droplist content
			//GUIContent[] droplistContent=new GUIContent[InputManager.mainInputManager.GetSelectedLinks().Count];
			List<GUIContent> droplistContent=new List<GUIContent>();
			for (int i=0; i<InputManager.mainInputManager.GetSelectedLinks().Count; i++)
			{
				//droplistContent[i]=new GUIContent(InputManager.mainInputManager.GetSelectedLinks()[i].id);
				droplistContent.Add (new GUIContent(InputManager.mainInputManager.GetSelectedLinks()[i].id));
			}	
			
			//main box and left hand labels
			//GUI.Box(contextMenuPosLinks,"");
			
			GUI.Label (new Rect(leftColumnStartX,leftColumnStartY,elementSizeX,elementSizeY),"Выбор элемента:");
			GUI.Label (new Rect(rightColumnStartX,rightColumnStartY,elementSizeX,elementSizeY),"Цвет элемента:");
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
			int droplistPick=selectColorDroplist.List(new Rect(rightColumnStartX,rightColumnStartY+elementSizeY*0.6f,elementSizeX*1.3f,elementSizeY)//elementSizeY)
			                                          ,droplistColorContent,"box",currentSkin.customStyles[1]); 
			switch (droplistPick)
			{
				case 0:{coloredLink.color="black"; break;}
				case 1:{coloredLink.color="red"; break;}
				case 2:{coloredLink.color="green"; break;}
				case 3:{coloredLink.color="yellow"; break;}
				case 4:{coloredLink.color="cyan"; break;}
			}
			
			//select obj menu (must be after endgroup rendered)
			selectItemDroplist.List(new Rect(leftColumnStartX,leftColumnStartY+elementSizeY*0.6f,elementSizeX,elementSizeY)//elementSizeY)
			                        ,droplistContent,"box",currentSkin.customStyles[1]);
			lastSelectedLink=InputManager.mainInputManager.GetSelectedLinks()[selectItemDroplist.GetSelectedItemIndex()];
		}
	}
}
