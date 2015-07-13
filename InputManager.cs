using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Topology;
using Vectrosity;

public class InputManager : MonoBehaviour {

	GameController controller;
	
	public static InputManager mainInputManager;
	
	Link lastSelectedLink=null;
	Node lastSelectedNode=null;
	TimerDetector dclickTimer=null;
	
	List<Link> selectedLinks=new List<Link>();
	List<Node> selectedNodes=new List<Node>();
	//GUIContainer contextMenu;
	public Rect contextMenuPosNodes=new Rect(5,125,290,150);
	public Rect contextMenuPosLinks=new Rect(5,125,290,100);
	
	Rect nodeListRect=new Rect(Screen.width-190,0,190,Screen.height);
	string nodeListSearchFilter="";
	Node nodeListLastClicked=null;
	TimerDetector nodeListDclickTimer=null;
	
	int nodeListFirstElementIndex=0;
	Rect saveButtonRect=new Rect(5,90,80,20);
	Rect openButtonRect=new Rect(5,60,80,20);
	Rect fileBrowserWindowRect=new Rect(100, 100, 600, 500);
	
	int tooltipMode
	{
		get {return _tooltipMode;}
		set 
		{
			if (value!=_tooltipMode) 
			{
				//lastSelectedLinkNode=null;
				if (value==0) {lastSelectedLink=null;}
				if (value==1) {lastSelectedNode=null;}
			}
			_tooltipMode=value;
		}
		
	}
	int _tooltipMode=0;
	
	enum MouseClickMode {SingleSelect, MultiSelect, ControlSelect, CreateLinkMode, CreateHierarchyLinkMode};
	//ctrl, shift,alt select, link create and hierarchy link toggle
	MouseClickMode currentClickMode=MouseClickMode.SingleSelect;
	
	//something was selected this frame, so no deselect
	bool selectionMade=false;
	
	Popup selectItemDroplist=new Popup();
	Popup selectColorDroplist=new Popup();
	Popup selectIconDroplist=new Popup();
	//public Texture[] nodeTextures;
	public Texture[] colorTextures;
	bool renderList=false;
	public GUISkin fbSkin;
	public GUISkin droplistSkin;
	public Texture2D file,folder;
	FileBrowser fb;
	
	bool draggingNode=false;
	
	// Use this for initialization
	void Start () {
		//Rect nodeListRect=new Rect(Screen.width-160,0,160,Screen.height);
		mainInputManager=this;
		controller=gameObject.GetComponent<GameController>();
	}
	
	//fires before physics clicks and Update
	void FixedUpdate()
	{
		HandleClickMode();
	}	
	
	//fires after physics clicks
	void Update()
	{
		
		//manage input
		ManageAllNodeSelect();
		
		ManageLinkSelection();
		
		ManageClickDeselect();
		ManageDoubleclick();
		ManageSelectionBox();
		
		ManageObjectDeletion();
		ManageObjectCopyPaste();
		//Prepare selection var for next frame
		selectionMade=false;
		
	}
	
	protected void OnGUI () 
	{
		GUI.skin=fbSkin;
		if (fb != null) 
		{
			fb.OnGUI();
		} 
		else 
		{
			OnGUIMain();
		}
		if (controller.SceneIsLoaded()) 
		{
			DrawNodeList();
			if (GUI.Button(saveButtonRect,"Сохранить")) {controller.SaveAll();}
		}
		ManageTooltip();
	}
	
	protected void OnGUIMain() {
		
		GUILayout.BeginHorizontal();
		//GUILayout.Label("Xml File", GUILayout.Width(100));
		GUILayout.FlexibleSpace();
		//GUILayout.Label(sourceFile ?? "none selected");
		if (GUI.Button(openButtonRect,"Открыть..."))
		{//GUILayout.ExpandWidth(false))) {
			fb = new FileBrowser(fileBrowserWindowRect,"Выберите xml файл",FileSelectedCallback);
			
			fb.SelectionPattern = "*.xml";
			fb.DirectoryImage=folder;
			fb.FileImage=file;
			
		}
		GUILayout.EndHorizontal();
	}
	
	protected void FileSelectedCallback(string path) {
		
		//sourceFile = path;
		controller.SetSourceFile(path);
		if (path!=null)
		{
			if (controller.SceneIsLoaded()) 
			{
				selectedLinks.Clear();
				selectedNodes.Clear();
				controller.ClearScene();
			}
			controller.StartLayoutLoad();//StartCoroutine( LoadLayout() );
		}
		fb=null;
	}
	

	
	void HandleClickMode()
	{
		currentClickMode=MouseClickMode.SingleSelect;
		if (Input.GetKey("left shift")) {currentClickMode=MouseClickMode.MultiSelect;}
		if (Input.GetKey("left ctrl")) {currentClickMode=MouseClickMode.ControlSelect;}
		if (Input.GetKey("left alt"))  {currentClickMode=MouseClickMode.CreateLinkMode;}
		if (Input.GetKey (KeyCode.M)) {currentClickMode=MouseClickMode.CreateHierarchyLinkMode;}
		//if (draggingNode) {selectMode=1;}	
	}
	
	//Nodes
	
	
	public void ClickNode(Node clickedNode, bool nonClick)
	{
		ClickedNodeAction(clickedNode,nonClick);
	}
	
	public void ClickedNodeAction(Node clickedNode, bool nonClick, bool dragged)
	{
		if (dragged) {currentClickMode=MouseClickMode.MultiSelect;}
		ClickedNodeAction(clickedNode,nonClick);
	}
	
	public void ClickedNodeAction(Node clickedNode, bool nonClick)
	{
		if (!ClickedOnGUI() | nonClick)
		{
			selectionMade=true;
			DeselectAllLinks();
			tooltipMode=0;
			// single select mode
			if (currentClickMode==MouseClickMode.SingleSelect)
			{	
				DeselectAllNodes();
				selectedNodes.Add(clickedNode);//HashtableAppendNum(selectedNodes,clickedNode);//selectedNodes.Add(clickedNode);
				clickedNode.selected=true;
			}
		
			//shift select mode
			if (currentClickMode==MouseClickMode.MultiSelect)
			{ 
				//selectedNodes.Add(clickedNode);
				if (!selectedNodes.Contains(clickedNode))
				{
					selectedNodes.Add(clickedNode);//HashtableAppendNum(selectedNodes,clickedNode);
					clickedNode.selected=true;
				}
			}
		
			// ctrl select mode
			if (currentClickMode==MouseClickMode.ControlSelect) 
			{
				if (selectedNodes.Contains(clickedNode))//selectedNodes.Contains(clickedNode)) 
				{
					selectedNodes.Remove(clickedNode);//selectedNodes.Remove(HashtableFindKeyOfValue(clickedNode));
					clickedNode.selected=false;
				}
				else
				{
					selectedNodes.Add(clickedNode);//HashtableAppendNum(selectedNodes,clickedNode);//selectedNodes.Add(clickedNode);
					clickedNode.selected=true;
				}
			}
			if (currentClickMode==MouseClickMode.CreateLinkMode)
			{
				if (selectedNodes.Count>0) 
				{
					foreach (Node selectedNode in selectedNodes)
					{
						if (selectedNode.id!=clickedNode.id)
						{
							controller.CreateNewLink(selectedNode.id,clickedNode.id);
						}
					}
				}
			}
			if (currentClickMode==MouseClickMode.CreateHierarchyLinkMode)
			{
				if (selectedNodes.Count>0) 
				{
					foreach (Node selectedNode in selectedNodes)
					{
						if (selectedNode.id!=clickedNode.id)
						{
							if (selectedNode.parentNode!=clickedNode)
							{
								controller.SetNodeAsChild(selectedNode,clickedNode);
								controller.CreateNewLink(selectedNode.id, clickedNode.id);
							}
							else
							{
								controller.UnchildNode(selectedNode);
								controller.DeleteLinkBetweenNodes(selectedNode,clickedNode);
							}
							//controller.ToggleLink(selectedNode.id,clickedNode.id);
							//controller.CreateNewLink(selectedNode.id,clickedNode.id);
							//controller.ToggleNodeHierarchy(selectedNode,clickedNode);
						}		
					}
				}
			}
		}
	}
	//Called by node when its dragged	
	public void DragNode(Node draggedNode, Vector3 moveDelta)
	{
		List<Link> affectedLinks=new List<Link>();
		Hashtable cachedList=controller.GetLinks();
		//List<Vector3Pair> affectedLinkPoss=new List<Vector3Pair>();
		foreach (Node node in selectedNodes)
		{
			if (node!=draggedNode) {node.DragAlong(moveDelta);}
			//call all connected links to realign
			foreach (Link link in cachedList.Values)
			{
				if (link.source==node) 
				{
					if (!affectedLinks.Contains(link)) {affectedLinks.Add(link);}
				}
				if (link.target==node)
				{
					if (!affectedLinks.Contains(link)) {affectedLinks.Add(link);}
				}
			}
		}
		controller.linkDrawManager.UpdateSwappedLinkPositions(affectedLinks);//,affectedLinkPoss);	
	}
	
	public void NodeDragStart()
	{
		List<Link> swapLinks=new List<Link>();
		Hashtable cachedList=controller.GetLinks();
		//Swap links in main linkvector for a separate swap object
		foreach(Node node in selectedNodes)
		{
			foreach (Link link in cachedList.Values)
			{
				if (link.source==node | link.target==node) 
				{
					swapLinks.Add (link);
				}
			}
		}
		Link[] swapOverAr=new Link[swapLinks.Count];
		//swapLinks.ToArray(swapOverAr);
		swapOverAr=swapLinks.ToArray();
		controller.linkDrawManager.SwapDrawnLinks(swapOverAr);
	}
	
	//Put connecting link drawing back into the main renderer
	public void NodeDragComplete()
	{
		//VectorLine.Destroy(ref swapLinksVector);
		
		List<Link> unswapLinks=new List<Link>();
		Hashtable cachedList=controller.GetLinks();
		foreach(Node node in selectedNodes)
		{
			//Link[] unswapLinks=new Link[];
			
			foreach (Link link in cachedList.Values)
			{
				if (link.source==node | link.target==node) 
				{
					unswapLinks.Add(link);
				}
			}
			
		}
		//UpdateLinkVector();
		Link[] swapOverAr=new Link[unswapLinks.Count];
		swapOverAr=unswapLinks.ToArray();
		controller.linkDrawManager.UnswapDrawnLinks(swapOverAr);
	}
	
	void ManageAllNodeSelect()
	{
		#if UNITY_EDITOR
		if (Input.GetKeyDown (KeyCode.A))
		{
			Hashtable cachedNodes=controller.GetNodes();
			foreach(Node node in cachedNodes.Values)
			{
				ClickedNodeAction(node,true, true);
			}
		}
		#else
		if (Input.GetKey(KeyCode.LeftShift))
		{
			if (Input.GetKeyDown (KeyCode.A))
			{
				Hashtable cachedNodes=controller.GetNodes();
				foreach(Node node in cachedNodes.Values)
				{
					ClickedNodeAction(node,true, true);
				}
			} 
		}
		#endif
	}
	
	void CopySelectedNodes()
	{
		if (selectedNodes.Count>0)
		{
			controller.CopyNodes(selectedNodes);
		}
	}
	
	void DeselectAllNodes()
	{
		if ( selectedNodes.Count>0) 
		{
			foreach (Node selectedNode in selectedNodes)
			{
				selectedNode.selected=false;
			}
			selectedNodes.Clear();
		}
	}
	
	void DeleteSelectedNodes()
	{
		controller.DeleteNodes(selectedNodes);
		selectedNodes.Clear();
	}
	
	//LINKS///
	
	Link AttemptSelectLink() 
	{
		Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
		
		//-1 means no point selected
		//int iSelect = -1;
		Link linkSelect=null;
		float closestYet = Mathf.Infinity;
		
		float selectionThreshold=20f;//0.22f;
		
		Dictionary<Link, Vector3Pair> allPoints=controller.linkDrawManager.GetLinkPoints();
		foreach (Vector3Pair points in allPoints.Values)
		{
			Vector3 closest1;
			Vector3 closest2;
			if (Math3d.ClosestPointsOnTwoLines(out closest1, out  closest2, ray.origin, ray.direction, points.p1, points.p2 - points.p1)) {
				Vector3 v = Math3d.ProjectPointOnLineSegment(points.p1, points.p2, closest1);
				float dist = (v - closest1).magnitude;
				if (dist < closestYet && dist < selectionThreshold) 
				{
					//iSelect = i;
					linkSelect=controller.linkDrawManager.PointDictionaryFindKey(points);//PointDictionaryFindKey(points);
					closestYet = dist;
				}
			}
		}
		//Returns the index of the point of selected link
		return linkSelect;
	}
	
	void ManageLinkSelection()
	{
		if (Input.GetMouseButtonDown(0) && !selectionMade)
		{
			//int selectedPointIndex=AttemptSelectLink();
			//if (selectedPointIndex!=-1) {ClickLink(GetLinkFromPointIndex(selectedPointIndex));}
			Link selectedLink=AttemptSelectLink();
			if (selectedLink!=null) {ClickLink(selectedLink);}
		}
	}
	
	//Link click action
	public void ClickLink(Link clickedLink)
	{
		if (!ClickedOnGUI())
		{
			//get sibling (if one exists)
			Link sibling=null;
			//Link sibling=GetLinkSibling(clickedLink);
			//If sibling doesn't exist, pass null
			ClickedLinkAction(clickedLink,sibling);
		}
	}
	
	
	//Do action based on select mode
	void ClickedLinkAction(Link actionLink, Link sibling)
	{
		selectionMade=true;
		DeselectAllNodes();
		//drawTooltip=true;
		tooltipMode=1;
		switch (currentClickMode)
		{
			//Single select
		case MouseClickMode.SingleSelect:
		{
			DeselectAllLinks();
			selectedLinks.Add (actionLink);//HashtableAppendNum(selectedLinks,actionLink);//selectedLinks.Add (actionLink);
			actionLink.selected=true;
			//Make sure sibling is kept selected
			if (sibling!=null)
			{	
				selectedLinks.Add (sibling);//HashtableAppendNum(selectedLinks,actionLink);//selectedLinks.Add (sibling);
				sibling.selected=true;
			}
			break;
		}
			//multiselect
		case MouseClickMode.MultiSelect:
		{
			selectedLinks.Add (actionLink);//HashtableAppendNum(selectedLinks,actionLink);//selectedLinks.Add (actionLink);
			actionLink.selected=true;
			if (sibling!=null)
			{
				selectedLinks.Add (sibling);//HashtableAppendNum(selectedLinks,sibling);//selectedLinks.Add (sibling);
				sibling.selected=true;
			}
			break;
		}
			//ctrl select
		case MouseClickMode.ControlSelect:
		{
			if (actionLink.selected)
			{
				selectedLinks.Remove (actionLink);//selectedLinks.Remove(HashtableFindKeyOfValue(actionLink));//selectedLinks.Remove(actionLink);
				actionLink.selected=false;
				//assume sibling has the same status as main
				if (sibling!=null)
				{
					selectedLinks.Remove(sibling);//selectedLinks.Remove(HashtableFindKeyOfValue(sibling));//selectedLinks.Remove(sibling);
					sibling.selected=false;
				}
			}
			else
			{
				selectedLinks.Add (actionLink);//HashtableAppendNum(actionLink);//selectedLinks.Add(actionLink);
				actionLink.selected=true;
				//assume sibling has the same status as main
				if (sibling!=null)
				{
					selectedLinks.Add (sibling);//HashtableAppendNum(sibling);//selectedLinks.Add(sibling);
					sibling.selected=true;
				}
				
			}
			break;
		}
		}
	}
	
	void DeselectAllLinks()
	{
		if (selectedLinks.Count>0) 
		{
			foreach (Link selectedLink in selectedLinks)
			{
				selectedLink.selected=false;
			}
			selectedLinks.Clear();
		}
	}
	
	void DeleteSelectedLinks()
	{
		controller.DeleteLinks(selectedLinks);
		selectedLinks.Clear();
	}
	
	///////////////////////////INPUT FUNCTIONS//////////////////////////
	
	//Deselect current selection on clicking empty space
	void ManageClickDeselect()
	{
		if (Input.GetMouseButtonDown(0) && controller.SceneIsLoaded()) 
		{
			Vector2 mousePosInGUICoords = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
			if (EmptySpaceClick())
			{	
				DeselectAllNodes();
				DeselectAllLinks();
			}
		}
	}
	
	void ManageSelectionBox()
	{
		if (Input.GetMouseButtonDown(0)) 
		{
			Vector2 mousePosInGUICoords = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
			if (EmptySpaceClick())
			{StartCoroutine(ManageSelectionBoxRoutine());}	
		}
	}
	
	IEnumerator ManageSelectionBoxRoutine()
	{
		//Switches to multiselect
		Rect screenSelectRect=new Rect();
		Vector2 originalPos = Input.mousePosition;
		VectorLine selectionBoxLine=new VectorLine("Selection", new Vector2[5], null, 4.0f, LineType.Continuous);
		selectionBoxLine.textureScale=4.0f;
		yield return new WaitForEndOfFrame();

		while(Input.GetMouseButton(0))
		{	
			selectionBoxLine.MakeRect (originalPos, Input.mousePosition);
			selectionBoxLine.Draw();
			selectionBoxLine.textureOffset = -Time.time*2.0f % 1;
			
			currentClickMode=MouseClickMode.MultiSelect;//selectMode=1;
			screenSelectRect.position=originalPos;
			screenSelectRect.xMax=Input.mousePosition.x;
			screenSelectRect.yMax=Input.mousePosition.y;
			
			//DeselectAllNodes();
			Hashtable cachedNodesList=controller.GetNodes();
			foreach (Node node in cachedNodesList.Values)
			{
				if (screenSelectRect.Contains(Camera.main.WorldToScreenPoint(node.transform.position),true)) 
				{
					ClickedNodeAction(node,true);
				} 
				else 
				{
					if (selectedNodes.Contains(node)) {node.selected=false; selectedNodes.Remove(node);}
				}
			}
			
			yield return new WaitForFixedUpdate();
		}
		VectorLine.Destroy(ref selectionBoxLine);
		yield break;
	}
	
	void ManageDoubleclick()
	{
		if (Input.GetMouseButtonDown(0) && controller.SceneIsLoaded()) 
		{
			//Vector2 mousePosInGUICoords = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
			if (EmptySpaceClick())
			{	
				if (dclickTimer==null) {StartCoroutine(ManageDoubleclickTimer());}
			}
			
		}
	}
	
	//checks if select mode is right and the click doesn't land on GUI elements
	bool EmptySpaceClick()
	{
		bool clickApplicable=true;
		Vector2 mousePosInGUICoords = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
		if (currentClickMode==MouseClickMode.SingleSelect && !selectionMade && controller.SceneIsLoaded() && !ClickedOnGUI())
		{clickApplicable=true;} else {clickApplicable=false;}
		return clickApplicable;
	}
	
	bool ClickedOnGUI()
	{
		bool clickedOnGUI=false;
		Vector2 mousePosInGUICoords = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
		if ((fb==null | !fileBrowserWindowRect.Contains(mousePosInGUICoords))
		    && !openButtonRect.Contains(mousePosInGUICoords)
		    && !saveButtonRect.Contains(mousePosInGUICoords)
		    && !contextMenuPosNodes.Contains(mousePosInGUICoords) 
		    && !contextMenuPosLinks.Contains(mousePosInGUICoords)
		    && !selectItemDroplist.GetCurrentDimensions().Contains(mousePosInGUICoords) 
		    && !selectColorDroplist.GetCurrentDimensions().Contains(mousePosInGUICoords)
		    && !selectIconDroplist.GetCurrentDimensions().Contains(mousePosInGUICoords)
		    && !nodeListRect.Contains(mousePosInGUICoords))
		{clickedOnGUI=false;} else {clickedOnGUI=true;}
		return clickedOnGUI;
	}
	
	//for dclick creating new nodes
	IEnumerator ManageDoubleclickTimer()
	{
		//if (Input.GetMouseButtonUp(0))
		//{
		dclickTimer=new TimerDetector(0.2f);
		yield return new WaitForFixedUpdate();
		while (!dclickTimer.UpdateTimer())
		{
			if (Input.GetMouseButtonDown(0) && controller.SceneIsLoaded()) 
			{
				//Vector2 mousePosInGUICoords = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
				//make sure it doesn't fire while over gui elements
				if (EmptySpaceClick())
				{controller.CreateNewNode(); break;}
			}
			yield return new WaitForFixedUpdate();	
		}
		yield return new WaitForFixedUpdate();
		dclickTimer=null;
		yield break;
	}
	
	void ManageObjectDeletion()
	{
		if (Input.GetKeyDown (KeyCode.Delete))
		{
			if (selectedNodes.Count>0){DeleteSelectedNodes();}
			if (selectedLinks.Count>0){DeleteSelectedLinks();}
		}
	}
	
	void ManageObjectCopyPaste()
	{
		#if UNITY_EDITOR
		if (Input.GetKeyDown (KeyCode.C))
		{
			CopySelectedNodes();
		} else 
			if (Input.GetKeyDown (KeyCode.V))//if (Input.GetKey(KeyCode.LeftControl) &&Input.GetKeyDown(KeyCode.V))
		{
			controller.PasteCopiedNodes();
		}
		#else
		if (Input.GetKey(KeyCode.LeftControl))
		{
			if (Input.GetKeyDown (KeyCode.C))
			{
				CopySelectedNodes();
			} else 
			if (Input.GetKeyDown (KeyCode.V))//if (Input.GetKey(KeyCode.LeftControl) &&Input.GetKeyDown(KeyCode.V))
			{
				controller.PasteCopiedNodes();
			}
		}
		#endif
	}
	
	//////GUI DRAW FUNCTIONS/////////////////////////////////////////////
	
	void ManageTooltip()
	{
		int ttMode=0;
		if (selectedNodes.Count>0) {ttMode=1;}
		if (selectedLinks.Count>0) {ttMode=2;}
		if (ttMode!=0) {DrawTooltip(ttMode);}
	}
	
	void DrawTooltip(int mode)
	{
		float elementSizeX=110;
		float elementSizeY=40;
		//This only works as long as links and nodes context menus have the same startpoint (upper left point)
		float leftColumnStartX=contextMenuPosLinks.x+20;//30;
		float leftColumnStartY=contextMenuPosLinks.y+15;//115;
		float rightColumnStartX=contextMenuPosLinks.x+160;//170;
		float rightColumnStartY=contextMenuPosLinks.y+15;
		float vPad=5;
		
		//nodes tooltip
		if (mode==1)
		{
			//check if last select is still in the list
			if (selectedNodes.Contains(lastSelectedNode))//(Node)lastSelectedLinkNode))
			{
				selectItemDroplist.SetSelectedItemIndex(selectedNodes.IndexOf(lastSelectedNode));//((Node)lastSelectedLinkNode));
			} else {selectItemDroplist.SetSelectedItemIndex(0);}
			
			//Generate droplist content
			GUIContent[] droplistContent=new GUIContent[selectedNodes.Count];
			for (int i=0; i<selectedNodes.Count; i++)
			{
				droplistContent[i]=new GUIContent(selectedNodes[i].id);
			}
			
			//Main box and labels
			GUI.Box(contextMenuPosNodes,"",fbSkin.box);
			//GUI.BeginGroup(contextMenuPosNodes);
			GUI.Label (new Rect(leftColumnStartX,leftColumnStartY,elementSizeX,elementSizeY),"Выбор элемента:");
			GUI.Label (new Rect(rightColumnStartX,rightColumnStartY,elementSizeX,elementSizeY),"Имя элемента:");
			GUI.Label (new Rect(leftColumnStartX,leftColumnStartY+elementSizeY*2+vPad*2,elementSizeX,elementSizeY),"Иконка:");
			
			//name edit field
			string nodeName=selectedNodes[selectItemDroplist.GetSelectedItemIndex()].nodeText.text;
			nodeName=GUI.TextField(new Rect(rightColumnStartX,rightColumnStartY+elementSizeY+3,elementSizeX,elementSizeY),nodeName);
			selectedNodes[selectItemDroplist.GetSelectedItemIndex()].nodeText.text=nodeName;
			//GUI.EndGroup();
			
			//set current icon selection
			Node currentNode=selectedNodes[selectItemDroplist.GetSelectedItemIndex()];
			selectIconDroplist.SetSelectedItemIndex(currentNode.GetSpriteIndex());
			//generate icon droplist content
			Texture[] cachedNodeTextures=controller.GetNodeTextures();
			GUIContent[] iconDroplistContent=new GUIContent[cachedNodeTextures.Length];//new GUIContent[4];
			
			iconDroplistContent[0]=new GUIContent("WinXP",cachedNodeTextures[0]);
			iconDroplistContent[1]=new GUIContent("Windows 7",cachedNodeTextures[1]);
			iconDroplistContent[2]=new GUIContent("Windows 8",cachedNodeTextures[2]);
			iconDroplistContent[3]=new GUIContent("Server 2008",cachedNodeTextures[3]);
			iconDroplistContent[4]=new GUIContent("Server 2012",cachedNodeTextures[4]);
			//iconDroplistContent[0]=new GUIContent("WinXP",nodeTextures[0]);
			//iconDroplistContent[1]=new GUIContent("Win07",nodeTextures[1]);
			//iconDroplistContent[2]=new GUIContent("2008",nodeTextures[2]);
			//iconDroplistContent[3]=new GUIContent("2003",nodeTextures[3]);
			//select icon droplist
			selectIconDroplist.List(new Rect(leftColumnStartX+elementSizeX*0.5f,leftColumnStartY+elementSizeY*2+vPad*2,elementSizeX*1.5f,elementSizeY)
			                        ,iconDroplistContent,"box",fbSkin.customStyles[1]);
			//Set new icon for all selected nodes
			foreach (Node node in selectedNodes) {node.SetSprite(selectIconDroplist.GetSelectedItemIndex());}
			//currentNode.SetSprite(selectIconDroplist.GetSelectedItemIndex());
			
			//select obj menu (must be last item rendered)
			selectItemDroplist.List(new Rect(leftColumnStartX,leftColumnStartY+elementSizeY,elementSizeX,elementSizeY)//elementSizeY+100)
			                        ,droplistContent,"box",fbSkin.customStyles[1]);
			lastSelectedNode=selectedNodes[selectItemDroplist.GetSelectedItemIndex()];//lastSelectedLinkNode=selectedNodes[selectItemDroplist.GetSelectedItemIndex()];
		}
		
		//links tooltip
		if (mode==2) 
		{
			//check if last select is still in the list
			if (selectedLinks.Contains(lastSelectedLink))//(Link)lastSelectedLinkNode))
			{
				selectItemDroplist.SetSelectedItemIndex(selectedLinks.IndexOf(lastSelectedLink));//(Link)lastSelectedLinkNode));
			} else {selectItemDroplist.SetSelectedItemIndex(0);}
			//Generate droplist content
			GUIContent[] droplistContent=new GUIContent[selectedLinks.Count];
			for (int i=0; i<selectedLinks.Count; i++)
			{
				droplistContent[i]=new GUIContent(selectedLinks[i].id);
			}	
			
			//main box and left hand labels
			GUI.Box(contextMenuPosLinks,"");
			//GUI.BeginGroup(contextMenuPosLinks);
			GUI.Label (new Rect(leftColumnStartX,leftColumnStartY,elementSizeX,elementSizeY),"Выбор элемента:");
			GUI.Label (new Rect(rightColumnStartX,rightColumnStartY,elementSizeX,elementSizeY),"Цвет элемента:");
			//GUI.EndGroup();
			
			//Set current color select
			Link coloredLink=selectedLinks[selectItemDroplist.GetSelectedItemIndex()];
			selectColorDroplist.SetSelectedItemIndex(coloredLink.GetColorIndex());
			//Generate droplist content
			GUIContent[] droplistColorContent=new GUIContent[5];
			droplistColorContent[0]=new GUIContent(colorTextures[0]);//"black");
			droplistColorContent[1]=new GUIContent(colorTextures[1]);//"red");
			droplistColorContent[2]=new GUIContent(colorTextures[2]);//"green");
			droplistColorContent[3]=new GUIContent(colorTextures[3]);//"yellow");
			droplistColorContent[4]=new GUIContent(colorTextures[4]);//"cyan");
			//Draw color droplist
			int droplistPick=selectColorDroplist.List(new Rect(rightColumnStartX,rightColumnStartY+elementSizeY,elementSizeX,elementSizeY)//elementSizeY)
			 ,droplistColorContent,"box",fbSkin.customStyles[1]); 
			switch (droplistPick)
			{
				case 0:{coloredLink.color="black"; break;}
				case 1:{coloredLink.color="red"; break;}
				case 2:{coloredLink.color="green"; break;}
				case 3:{coloredLink.color="yellow"; break;}
				case 4:{coloredLink.color="cyan"; break;}
			}
			
			//select obj menu (must be after endgroup rendered)
			selectItemDroplist.List(new Rect(leftColumnStartX,leftColumnStartY+elementSizeY,elementSizeX,elementSizeY)//elementSizeY)
			                        ,droplistContent,"box",fbSkin.customStyles[1]);
			//lastSelectedLinkNode=selectedLinks[selectItemDroplist.GetSelectedItemIndex()];
			lastSelectedLink=selectedLinks[selectItemDroplist.GetSelectedItemIndex()];
		}
	}
	
	/*
	struct DrawnNode
	{
		public Node drawnNode;
		public Node parentNode;
		public bool hasChildren;
		public bool unfoldChildren;
		
		public DrawnNode(Node newDrawnNode, Node newNodeParent, bool nodeHasChildren)
		{
			drawnNode=newDrawnNode;
			parentNode=newNodeParent;
			hasChildren=nodeHasChildren;
			unfoldChildren=false;
		}
		
		public DrawnNode(Node newDrawnNode, Node newNodeParent)
		{
			drawnNode=newDrawnNode;
			parentNode=newNodeParent;
			hasChildren=false;
			unfoldChildren=false;
		}
		
	}*/
	
	/*
	public void StartDrawnNodeList()
	{
		//DrawnNode addedNode;
		foreach(Node node in controller.GetRootNodes())
		{
			//addedNode=new DrawnNode();
			//addedNode.drawnNode=node;
			//addedNode.parentNode=null;
			//if (controller.GetNodeTrees().ContainsKey(node)) {addedNode.hasChildren=true;}
			//nodeListDrawnNodes.Add(addedNode);
			drawnNodeList.Add(node);	
		}
		//print (drawnNodeList.Count);
	}*/
	
	/*
	void drawnNodeListContainsNode(Node node)
	{
		bool nodeFound=false;
		//if (nodeListDrawnNodes.Contains(node)) {}
		foreach(DrawnNode listNode in nodeListDrawnNodes)
		{
			if (listNode.drawnNode==node) 
			{
				nodeFound=true;
				break;
			}
		}
		return nodeFound;
	}
	*/
	
	List<Node> DownwardRecursiveDrawNodeChildren(Node parentNode)
	{
		List<Node> returnedNodes=new List<Node>();
		if (parentNode.unfoldChildren)
		{
			foreach (Node childNode in controller.GetNodeTrees()[parentNode])
			{
				returnedNodes.Add(childNode);
				returnedNodes.AddRange(DownwardRecursiveDrawNodeChildren(childNode));
			}	
		}
		return returnedNodes;
	}
	
	void DrawNodeList()
	{		
		float entryHeight=30f;
		float vPad=3f;
		float width=nodeListRect.width;//150f;
		float topOffset=40f;
		
		float scrollBarXStart=nodeListRect.x+nodeListRect.width-20;
		float searchFilterXStart=nodeListRect.x+10;
		
		//List<int> tst=new List<int>();
		//link a selection index to node dictionary
		//Node[] cachedNodes=new Node[controller.GetNodes().Count];
		//controller.GetNodes().Values.CopyTo(cachedNodes,0);
		List<Node> menuDrawnNodeList=new List<Node>();
		
		//Sync drawlist with current root list
		foreach(Node node in controller.GetRootNodes())
		{
			menuDrawnNodeList.Add(node);
			menuDrawnNodeList.AddRange(DownwardRecursiveDrawNodeChildren(node));
			/*
			Node downwardIteratorPointer=node;
			while (downwardIteratorPointer.unfoldChildren)
			{
				foreach (Node childNode in controller.GetNodeTrees()[downwardIteratorPointer])
				{
					menuDrawnNodeList.Add(downwardIteratorPointer);
					
				}
			}*/
			/*
			if (node.hasChildren && node.unfoldChildren)
			{
				foreach (Node childNode in controller.GetNodeTrees()[node])
				{
					menuDrawnNodeList.Add(childNode);
				}
			}*/
		}
		
		
		//backdrop
		GUI.Box(new Rect(nodeListRect),"");
		
		
		//Search bar
		nodeListSearchFilter=GUI.TextField(new Rect(searchFilterXStart,5,width-20,entryHeight),nodeListSearchFilter);
		//Set starting position for the first item in the list
		Rect entryRect=new Rect(Screen.width-width+30,topOffset,width-20,entryHeight);
			
		//Find max amount of entries that will fit on the screen, or node count if it is lower
		int maxEntries=Mathf.Min(Mathf.FloorToInt((Screen.height-topOffset)/(entryHeight+vPad)),menuDrawnNodeList.Count);
		
		//Find the max first entry index that will still allow the list to fill the entire screen
		int firstEntryMaxIndex=menuDrawnNodeList.Count-maxEntries;
		//Draw scrollbar if necessary
		if (firstEntryMaxIndex>0)
		{
			nodeListFirstElementIndex=Mathf.FloorToInt(GUI.VerticalScrollbar(new Rect(scrollBarXStart,5,10,Screen.height-10)
			,nodeListFirstElementIndex,0.5f,0,firstEntryMaxIndex));
		}
		else {nodeListFirstElementIndex=0;}
		
			//Draw all nodes as buttons
			GUIContent buttonContent=new GUIContent();
			for (int i=nodeListFirstElementIndex; i<nodeListFirstElementIndex+maxEntries; i++) 
			{
				//print (i);
				if (nodeListSearchFilter=="" | menuDrawnNodeList[i].nodeText.text.StartsWith(nodeListSearchFilter))
				{
							//Determine visual parent offset count
							float parentOffset=0;
							Node upwardRecursivePos=menuDrawnNodeList[i];
							while(upwardRecursivePos.parentNode!=null) 
							{
								//Node parentsParent=controller.FindParentOfChild(upwardRecursivePos.parentNode);
								//bool hasChildren=controller.GetNodeTrees().ContainsKey();
								parentOffset+=20f;
								upwardRecursivePos=upwardRecursivePos.parentNode;//controller.FindParentOfChild(upwardRecursivePos.parentNode);//parentsParent;
								if (upwardRecursivePos==null) break;
							}
							
							//Offset from parent node
							Rect modifiedEntryRect=entryRect;
							modifiedEntryRect.x+=parentOffset;
							
							//Mark out selected nodes with blue
							if (selectedNodes.Contains(menuDrawnNodeList[i])) 
							{GUI.Box(modifiedEntryRect,"",fbSkin.customStyles[3]);}
							
							//Handle children unfold button
							if (menuDrawnNodeList[i].hasChildren) 
							{
								Rect unfoldRect=new Rect(modifiedEntryRect);
								unfoldRect.x-=20f;
								unfoldRect.width=20f;
								//unfoldRect.height=20f;
								string unfoldButtonSign="x";
								if (menuDrawnNodeList[i].unfoldChildren) {unfoldButtonSign="-";}
								else {unfoldButtonSign="+";}
								if (GUI.Button (unfoldRect,unfoldButtonSign,fbSkin.customStyles[4])) 
								{
									menuDrawnNodeList[i].unfoldChildren=!menuDrawnNodeList[i].unfoldChildren;
									//print ("unfold set to:"+menuDrawnNodeList[i].unfoldChildren);
								}
							}
							
							//Draw actual node entry
							buttonContent.text=menuDrawnNodeList[i].nodeText.text;
							buttonContent.image=menuDrawnNodeList[i].renderer.material.GetTexture(0);
							if (GUI.Button(modifiedEntryRect,buttonContent,fbSkin.customStyles[2])) 
							{
								if (nodeListLastClicked==menuDrawnNodeList[i])
								{
									if (nodeListDclickTimer!=null) 
									{
										
										StopCoroutine("NodeListDclickTimerManager");
										print ("coroutine stopped");
										nodeListDclickTimer=null;
										Camera.main.transform.position=(Vector2)menuDrawnNodeList[i].transform.position;		
									}
									else
									{
										ClickNode(menuDrawnNodeList[i],true);
										StartCoroutine("NodeListDclickTimerManager");
									}
								}
								else
								{
									StopCoroutine("NodeListDclickTimerManager");
									StartCoroutine("NodeListDclickTimerManager");
									//print ("coroutine started on new");
									ClickNode(menuDrawnNodeList[i],true);
								}
								nodeListLastClicked=menuDrawnNodeList[i];
							}
							entryRect.y+=entryHeight+vPad;
						//}
				}
			}
		//}
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
