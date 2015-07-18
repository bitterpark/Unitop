using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Topology;

public class NodeList: MonoBehaviour
{
	Rect nodeListRect=new Rect(Screen.width-190,0,190,Screen.height);
	public Rect GetNodeListRect() {return nodeListRect;}
	string nodeListSearchFilter="";
	Node nodeListLastClicked=null;
	TimerDetector nodeListDclickTimer=null;
	
	float nodeListFirstElementIndex=0;
	Vector2 nodeListScrollPos=Vector2.zero;
	float nodeListProjectedWidth=190;
	public GUISkin mySkin;
	
	/*
	public NodeList(GUISkin usedSkin)
	{
		mySkin=usedSkin;
	}*/
	
	public void DrawNodeListWindow()
	{
		GUI.Window(0,nodeListRect,DrawNodeList,"Карта Нод");
	}
	
	List<Node> DownwardRecursiveDrawNodeChildren(Node parentNode)
	{
		List<Node> returnedNodes=new List<Node>();
		if (parentNode.unfoldChildren)
		{
			//increase scroll area width by parent offset
			nodeListProjectedWidth+=20f;
			foreach (Node childNode in GameController.mainController.GetNodeTrees()[parentNode])
			{
				if (nodeListSearchFilter=="" | childNode.nodeText.text.StartsWith(nodeListSearchFilter)) returnedNodes.Add(childNode);
				returnedNodes.AddRange(DownwardRecursiveDrawNodeChildren(childNode));
			}	
		}
		return returnedNodes;
	}
	
	void DrawNodeList(int sigInt)
	{		
		float entryHeight=30f;
		float vPad=3f;
		float parentOffsetDelta=20f;
		//float width=nodeListRect.width;//150f;
		
		//list offset from top border and search area
		float topOffset=80f;
		//list offset from bottom border
		float bottomOffset=30f;
		float leftOffset=40f;
		float rightOffset=10f;
		float expandButtonWidth=20;
		float entryWidth=nodeListRect.width-rightOffset-leftOffset;
		float searchBarWidth=nodeListRect.width-40;
		float verticalScrollbarWidth=10f;
		nodeListProjectedWidth=entryWidth+expandButtonWidth;
		
		float searchFilterXStart=20;//nodeListRect.x+10;
		float searchFilterYStart=55;
		
		//SEARCH BAR
		GUI.Label(new Rect(searchFilterXStart,searchFilterYStart-23,entryWidth,entryHeight),"Поиск");
		nodeListSearchFilter=GUI.TextField(new Rect(searchFilterXStart,searchFilterYStart,searchBarWidth,entryHeight),nodeListSearchFilter);
		
		List<Node> menuDrawnNodeList=new List<Node>();
		
		//Sync drawlist with current root list
		foreach(Node node in GameController.mainController.GetRootNodes())
		{
			if (nodeListSearchFilter=="" | node.nodeText.text.StartsWith(nodeListSearchFilter)) menuDrawnNodeList.Add(node);
			menuDrawnNodeList.AddRange(DownwardRecursiveDrawNodeChildren(node));
		}
		
		//print ("Projected width:"+nodeListProjectedWidth);
		//print ("Crammed into:"+entryWidth);
		
		//Find max amount of entries that will fit on the screen, or node count if it is lower
		int maxEntries=Mathf.Min(Mathf.FloorToInt((Screen.height-topOffset-bottomOffset)/(entryHeight+vPad)),menuDrawnNodeList.Count);							
		//Find the max first entry index that will still allow the list to fill the entire screen
		int firstEntryMaxIndex=menuDrawnNodeList.Count-maxEntries;
		
		//DRAW SCROLLBAR IF NECESSARY
		float scrollBarXStart=nodeListRect.width-30;
		if (firstEntryMaxIndex>0)
		{
			//print ("maxvalu:"+firstEntryMaxIndex);
			nodeListFirstElementIndex=GUI.VerticalScrollbar(new Rect(scrollBarXStart,topOffset,verticalScrollbarWidth,Screen.height-bottomOffset-topOffset-40)
			                                                ,nodeListFirstElementIndex,0.4f,0,firstEntryMaxIndex);
		}
		else {nodeListFirstElementIndex=0;}
		
		//(HORIZONTAL) SCROLL AREA SETUP
		//leftOffset-expandButtonWidth
		Rect scrollDims=new Rect(15,topOffset,entryWidth,nodeListRect.height-bottomOffset-topOffset+10);
		Rect scrollArea=new Rect(15,topOffset,nodeListProjectedWidth,topOffset+(entryHeight+vPad)*(maxEntries-3));
		nodeListScrollPos=GUI.BeginScrollView(scrollDims,nodeListScrollPos,scrollArea,true,false);
		
		//Set starting position for the first item in the list
		Rect entryRect=new Rect(leftOffset,topOffset,entryWidth,entryHeight);//Screen.width-width+30,topOffset,width-20,entryHeight);
		
		//DRAW ALL NODES AS BUTTONS
		GUIContent buttonContent=new GUIContent();
		for (int i=Mathf.RoundToInt(nodeListFirstElementIndex); i<Mathf.RoundToInt(nodeListFirstElementIndex)+maxEntries; i++) 
		{	
			//Determine visual parent offset count
			float parentOffset=0;
			Node upwardRecursivePos=menuDrawnNodeList[i];
			while(upwardRecursivePos.parentNode!=null) 
			{
				parentOffset+=parentOffsetDelta;
				upwardRecursivePos=upwardRecursivePos.parentNode;
				if (upwardRecursivePos==null) break;
			}
			
			//Offset from parent node
			Rect modifiedEntryRect=entryRect;
			modifiedEntryRect.x+=parentOffset;
			
			//Mark out selected nodes with blue
			if (InputManager.mainInputManager.GetSelectedNodes().Contains(menuDrawnNodeList[i])) 
			{GUI.Box(modifiedEntryRect,"",mySkin.customStyles[3]);}
			
			//Handle children unfold button
			if (menuDrawnNodeList[i].hasChildren) 
			{
				Rect unfoldRect=new Rect(modifiedEntryRect);
				unfoldRect.x-=20f;
				unfoldRect.width=expandButtonWidth;
				//unfoldRect.height=20f;
				string unfoldButtonSign="x";
				if (menuDrawnNodeList[i].unfoldChildren) {unfoldButtonSign="-";}
				else {unfoldButtonSign="+";}
				if (GUI.Button (unfoldRect,unfoldButtonSign,mySkin.customStyles[4])) 
				{
					menuDrawnNodeList[i].unfoldChildren=!menuDrawnNodeList[i].unfoldChildren;
				}
			}
			
			//Draw actual node entry
			buttonContent.text=menuDrawnNodeList[i].nodeText.text;
			buttonContent.image=menuDrawnNodeList[i].renderer.material.GetTexture(0);
			if (GUI.Button(modifiedEntryRect,buttonContent,mySkin.customStyles[2])) 
			{
				if (nodeListLastClicked==menuDrawnNodeList[i])
				{
					if (nodeListDclickTimer!=null) 
					{
						
						StopCoroutine("NodeListDclickTimerManager");
						nodeListDclickTimer=null;
						Camera.main.transform.position=(Vector2)menuDrawnNodeList[i].transform.position;		
					}
					else
					{
						InputManager.mainInputManager.ClickNode(menuDrawnNodeList[i],true);
						StartCoroutine("NodeListDclickTimerManager");
					}
				}
				else
				{
					StopCoroutine("NodeListDclickTimerManager");
					StartCoroutine("NodeListDclickTimerManager");
					//print ("coroutine started on new");
					InputManager.mainInputManager.ClickNode(menuDrawnNodeList[i],true);
				}
				nodeListLastClicked=menuDrawnNodeList[i];
			}
			entryRect.y+=entryHeight+vPad;
		}
		GUI.EndScrollView();
	}
	
	
	IEnumerator NodeListDclickTimerManager()
	{
		//print ("coroutine started");
		nodeListDclickTimer=new TimerDetector(0.3f);
		while (!nodeListDclickTimer.UpdateTimer())
		{
			yield return new WaitForFixedUpdate();
		}
		nodeListDclickTimer=null;
		yield break;
		
	}

}
