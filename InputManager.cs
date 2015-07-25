using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Topology;
using Vectrosity;

public class InputManager : MonoBehaviour {

	GameController controller;
	
	public static InputManager mainInputManager;
	
	public enum CursorLoc {OverGUI,OverScene};
	public CursorLoc currentCursorLoc=CursorLoc.OverScene;
	
	TimerDetector dclickTimer=null;
	
	List<Link> selectedLinks=new List<Link>();
	List<Node> selectedNodes=new List<Node>();
	public List<Node> GetSelectedNodes() {return selectedNodes;}
	public List<Link> GetSelectedLinks() {return selectedLinks;}
	void AddSelectedNodes(Node addedNode) 
	{
		selectedNodes.Add (addedNode);
		SelectedNodesChanged();
	}
	void RemoveSelectedNodes(Node removedNode) 
	{
		selectedNodes.Remove (removedNode);
		SelectedNodesChanged();
	}
	
	
	public Texture[] colorTextures;
	
	public delegate void NodeSelectChangeDelegate();
	public event NodeSelectChangeDelegate SelectedNodesChanged;
	
	public NodeList myNodeList;
	//public float GetNodeListWidth() {return myNodeList.}
	ContextMenuManager myContextMenu;
	
	Rect openButtonRect=new Rect(5,5,80,30);
	Rect saveButtonRect=new Rect(85,5,80,30);
	Rect dbButtonRect=new Rect(165,5,80,30);
	Rect helpButtonRect=new Rect(245,5,80,30);
	Rect quitButtonRect=new Rect(325,5,80,30);
	Rect fileBrowserWindowRect=new Rect(100, 100, 600, 500);
	Rect fileBrowserCurrentPos;
	
	bool supressContextMenu=false;
	
	enum MouseClickMode {SingleSelect, MultiSelect, ControlSelect, CreateLinkMode, CreateHierarchyLinkMode};
	//ctrl, shift,alt select, link create and hierarchy link toggle
	MouseClickMode currentClickMode=MouseClickMode.SingleSelect;
	
	//something was selected this frame, so no deselect
	bool selectionMade=false;
	
	public static void DebugPrint(string prStr) {print (prStr);}
	
	public GUISkin fbSkin;
	//public GUISkin droplistSkin;
	public Texture2D file,folder;
	FileBrowser fb;
	
	bool draggingNode=false;
	
	// Use this for initialization
	void Start () {
		//Rect nodeListRect=new Rect(Screen.width-160,0,160,Screen.height);
		mainInputManager=this;
		controller=gameObject.GetComponent<GameController>();
		myNodeList=gameObject.GetComponent<NodeList>();
		myContextMenu=new ContextMenuManager(fbSkin,colorTextures);
	}
	
	//fires before physics clicks and Update
	void FixedUpdate()
	{
		//Must be in fixedUpdate
		HandleClickMode();
	}	
	
	//fires after physics clicks
	void Update()
	{
		
		//manage input
		HandleCursorLoc();
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
			
			fileBrowserCurrentPos=GUI.Window(4,fileBrowserCurrentPos,DragFbWindowFunc,"",GUIStyle.none);
			fb.OnGUI(fileBrowserCurrentPos.x,fileBrowserCurrentPos.y);
		} 
		else 
		{
			OnGUIMain();
		}
		if (controller.SceneIsLoaded()) 
		{
			myNodeList.DrawNodeListWindow();
			if (controller.HasSourceFile())
			{
				if (GUI.Button(saveButtonRect,"Сохранить")) {controller.SaveAll();}
			}
		}
		
		if (GUI.Button (dbButtonRect,"БД"))
		{
			if (controller.SceneIsLoaded()) 
			{
				selectedLinks.Clear();
				selectedNodes.Clear();
				controller.ClearScene();
			}
			controller.StartDBLoad();
		}
		
		if (GUI.Button(helpButtonRect,"Помощь")) 
		{
			string helpPath=Application.dataPath+"/../Readme.txt";
			/*
			#if UNITY_EDITOR
			helpPath=Application.dataPath+"/Data/Readme.txt";
			#else
			helpPath=Application.dataPath+"/../Readme.txt";
			#endif
			*/
			Application.OpenURL(helpPath);
		}
		
		
		if (GUI.Button(quitButtonRect,"Выход")) {Application.Quit();}
		//ManageTooltip();
		myContextMenu.ManageTooltip(supressContextMenu);
	}
	
	protected void OnGUIMain() {
		
		GUILayout.BeginHorizontal();
		//GUILayout.Label("Xml File", GUILayout.Width(100));
		GUILayout.FlexibleSpace();
		//GUILayout.Label(sourceFile ?? "none selected");
		if (GUI.Button(openButtonRect,"Открыть..."))
		{//GUILayout.ExpandWidth(false))) {
			fb = new FileBrowser(fileBrowserWindowRect,"Выберите xml-файл",FileSelectedCallback);
			
			fb.SelectionPattern = "*.xml";
			fb.DirectoryImage=folder;
			fb.FileImage=file;
			fileBrowserCurrentPos=fileBrowserWindowRect;
			fileBrowserCurrentPos.height=20f;
			
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
	
	void DragFbWindowFunc(int emptySig) 
	{
		//Make window draggable
		GUI.DragWindow();
	}
	
	void HandleCursorLoc()
	{
		if (ClickedOnGUI()) {currentCursorLoc=CursorLoc.OverGUI;}
		else {currentCursorLoc=CursorLoc.OverScene;}
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
	
	
	public bool ClickNode(Node clickedNode, bool nonClick)
	{
		selectionMade=true;
		bool clickActionDone=false;
		if (!selectedNodes.Contains(clickedNode)) 
		{
			ClickedNodeAction(clickedNode,nonClick);
			clickActionDone=true;
		}
		return clickActionDone;
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
			//tooltipMode=0;
			myContextMenu.SetTooltipMode(0);
			// single select mode
			if (currentClickMode==MouseClickMode.SingleSelect)
			{	
				DeselectAllNodes();
				AddSelectedNodes(clickedNode);
				clickedNode.selected=true;
			}
		
			//shift select mode
			if (currentClickMode==MouseClickMode.MultiSelect)
			{ 
				if (!selectedNodes.Contains(clickedNode))
				{
					AddSelectedNodes(clickedNode);//HashtableAppendNum(selectedNodes,clickedNode);
					clickedNode.selected=true;
				}
			}
		
			// ctrl select mode
			if (currentClickMode==MouseClickMode.ControlSelect) 
			{
				if (selectedNodes.Contains(clickedNode))//selectedNodes.Contains(clickedNode)) 
				{
					RemoveSelectedNodes(clickedNode);
					clickedNode.selected=false;
				}
				else
				{
					AddSelectedNodes(clickedNode);
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
							//print ("hier toggled");
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
		supressContextMenu=true;
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
		supressContextMenu=false;
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
	
	void ManageSaveHotkey()
	{
		if (controller.SceneIsLoaded())
		{
			#if UNITY_EDITOR
			if (Input.GetKeyDown (KeyCode.V))
			{
				controller.SaveAll();
			}
			#else
			if (Input.GetKey(KeyCode.LeftControl))
			{
				if (Input.GetKeyDown (KeyCode.S))
				{
					controller.SaveAll();
				} 
			}
			#endif
		}
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
		//tooltipMode=1;
		myContextMenu.SetTooltipMode(1);
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
					if (selectedNodes.Contains(node)) {node.selected=false; RemoveSelectedNodes(node);}
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
	
	public bool ClickedOnGUI()
	{
		bool clickedOnGUI=false;
		Vector2 mousePosInGUICoords = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
		if ((fb==null | !fileBrowserWindowRect.Contains(mousePosInGUICoords))
		    && !openButtonRect.Contains(mousePosInGUICoords)
		    && !saveButtonRect.Contains(mousePosInGUICoords)
		    && !helpButtonRect.Contains(mousePosInGUICoords)
		    && !dbButtonRect.Contains(mousePosInGUICoords)
		    && !quitButtonRect.Contains(mousePosInGUICoords)
		    && (!myContextMenu.isDrawn 
		    	|(!myContextMenu.GetContextMenuPosNodes().Contains(mousePosInGUICoords) 
		    	&& !myContextMenu.GetContextMenuPosLinks().Contains(mousePosInGUICoords)//contextMenuPosLinks.Contains(mousePosInGUICoords)
		    	&& !myContextMenu.selectItemDroplist.GetCurrentDimensions().Contains(mousePosInGUICoords) 
		    	&& !myContextMenu.selectColorDroplist.GetCurrentDimensions().Contains(mousePosInGUICoords)
		    	&& !myContextMenu.selectIconDroplist.GetCurrentDimensions().Contains(mousePosInGUICoords)))
		    && !myNodeList.GetNodeListRect().Contains(mousePosInGUICoords))//nodeListRect.Contains(mousePosInGUICoords))
		{clickedOnGUI=false; /*print ("click not on gui!");*/} else {clickedOnGUI=true; /*print ("click on gui!");*/}
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
/*	
	IEnumerator DclickNodeCreateTimer()
	{
	
	
	}
	*/
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
	
}
