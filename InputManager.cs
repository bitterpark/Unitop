using UnityEngine;
using System.IO;
using System.Text;
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
	
	public List<Link> GetSelectedLinks() {return selectedLinks;}
	void AddSelectedLinks(Link addedLink)
	{
		selectedLinks.Add (addedLink);
		SelectedLinksChanged();
	}
	void RemoveSelectedLinks(Link removedLink)
	{
		selectedLinks.Remove (removedLink);
		SelectedLinksChanged();
	}
	
	
	public Texture[] colorTextures;
	
	public delegate void NodeSelectChangeDelegate();
	public event NodeSelectChangeDelegate SelectedNodesChanged;
	public delegate void LinkSelectChangeDelegate();
	public event LinkSelectChangeDelegate SelectedLinksChanged;
	
	public delegate void NodeDragEndDelegate();
	public event NodeDragEndDelegate NodeDragEnded;
	
	public delegate void SelectionBoxChangeDelegate(float x, float y, float maxX, float maxY);
	public event SelectionBoxChangeDelegate SelectionBoxChanged;
	
	public NodeList myNodeList;

	ContextMenuManager myContextMenu;
	bool supressContextMenu=false;
	float errorPopOutForSecs=3;
	float errorPopInForSecs=3;
	bool showErrorPopup=true;
	string errorPopupText="";
	Rect errorPopupRect;
	
	Rect openButtonRect=new Rect(5,10,100,30);
	Rect saveButtonRect=new Rect(105,10,100,30);
	
	public Texture2D savePopupCheckmark;
	bool showSavePopup=false;
	Rect savePopupRect=new Rect();
	float popOutForSecs=1;
	float popInForSecs=3;
	
	Rect dbButtonRect=new Rect(205,10,100,30);
	Rect helpButtonRect=new Rect(305,10,100,30);
	Rect quitButtonRect=new Rect(405,10,100,30);
	Rect fileBrowserWindowRect=new Rect(100, 100, 600, 500);
	Rect fileBrowserCurrentPos;
	Rect readmeWindowrect=new Rect(100, 100, 700, 550);
	//must be equal to readmeWindowrect but with height set to 20 (or whatever window drag handle is)
	Rect readmeWindowCurrentPos=new Rect(100,100,600,20);
	Vector2 readmeScrollPos=Vector2.zero;
	bool showReadme=false;
	string readmeText="txt";
	float readmeTextHeight=0;
	
	Rect quitWithoutSaveRect=new Rect(100,100,280,130);
	bool showQuitWithoutSaveDialog=false;
	bool showEmptyPushDialog=false;	
	
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
		quitWithoutSaveRect.x=Screen.width*0.5f-quitWithoutSaveRect.width*0.5f;
		quitWithoutSaveRect.y=Screen.height*0.5f-quitWithoutSaveRect.height*0.5f;
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
		ManageSaveHotkey();	
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
			
			fileBrowserCurrentPos=GUI.Window(4,fileBrowserCurrentPos,DragWindowFunc,"",GUIStyle.none);
			fb.OnGUI(fileBrowserCurrentPos.x,fileBrowserCurrentPos.y);
		} 
		else 
		{
			DrawFbButton();
		}
		if (controller.SceneIsLoaded()) 
		{
			myNodeList.DrawNodeListWindow();
			if (controller.HasSourceFile())
			{
				if (GUI.Button(saveButtonRect,"Сохранить")) {CallSave();}//controller.SaveAll();}
			}
			else 
			{
			  	if (GUI.Button(saveButtonRect,"Пуш в БД")) 
			  	{
					bool emptyNodeNamesExist=false;
					foreach(Node sceneNode in GameController.mainController.GetNodes().Values)
					{
						if (sceneNode.hostNode && sceneNode.text=="-")
						{
							emptyNodeNamesExist=true;
							break;
						}
					}
					if (!emptyNodeNamesExist) {CallDBPush();}
					else {showEmptyPushDialog=true;}
			  	}
			}
		}
		
		if (GUI.Button (dbButtonRect,"Синхронизация"))
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
			/*
			string helpPath=Application.dataPath+"/../Readme.txt";
			Application.OpenURL(helpPath);
			*/
			showReadme=!showReadme;
			if (showReadme) {readmeText=LoadReadmeText();}
		}
		if (showReadme) {DrawReadmeWindow();}
		
		if (GUI.Button(quitButtonRect,"Выход")) {Application.Quit();}
		
		
		//ManageTooltip();
		myContextMenu.ManageTooltip(supressContextMenu);
		
		if (showQuitWithoutSaveDialog) {DrawQuitWithoutSaveDialog();}
		if (showEmptyPushDialog) {DrawPushWithEmptyNodesDialog();}
		if (showSavePopup) {DrawSavePopup();}
		if (showErrorPopup) 
		{
			DrawContextNameErrorPopup();
		}
	}
	
	
	
	void PrepSavePopupDraw()
	{
		popOutForSecs=3;
		popInForSecs=3;
		showSavePopup=true;
		savePopupRect=new Rect(-15,Screen.height-160,120,40);
	}
	
	//IEnumerator SavePopupRoutine()
	void DrawSavePopup ()
	{
		//Screen.height-80,120,40);
		
		//float popOutForSecs=1;
		//float popInForSecs=3;
		float horizontalDeltaPerSec=63f;
		float xMax=15;
		GUIContent savePopupContent=new GUIContent("Сохранено",savePopupCheckmark);
		
		
		//while (popOutForSecs>0 | popInForSecs>0)
		//{
			
			GUI.Box(savePopupRect,savePopupContent);
			if (popOutForSecs>0)
			{
				popOutForSecs-=Time.deltaTime;
				savePopupRect.x+=horizontalDeltaPerSec*Time.deltaTime;
				//GameController.mainController.stat statusText = GameObject.Find("StatusText").guiText;
				//statusText.text = Time.deltaTime.ToString();
				
			}
			else
			{
				
				popInForSecs-=Time.deltaTime;
				savePopupRect.x-=horizontalDeltaPerSec*Time.deltaTime;
			}
			//GameController.mainController.statusText.text=Time.deltaTime.ToString();
			savePopupRect.x=Mathf.Clamp(savePopupRect.x,Mathf.NegativeInfinity,xMax);
			//yield return new WaitForEndOfFrame();
		//}
		if (popOutForSecs<=0 && popInForSecs<=0)
		showSavePopup=false;
		//yield break;
	}
	
	public void StartContextNameErrorPopup(int mode)
	{
		PrepContextNameErrorPopupDraw(mode);
	}
	
	void PrepContextNameErrorPopupDraw(int mode)
	{
		//incorrect name error
		if (mode==0) 
		{
			errorPopupText="Неверный IP";
		}
		//ip is taken within the workgroup
		if (mode==1) 
		{
			errorPopupText="IP занят";
		}
		errorPopOutForSecs=3;
		errorPopInForSecs=3;
		showErrorPopup=true;
		errorPopupRect=new Rect(-15,Screen.height-160,120,40);
	}
	
	//IEnumerator SavePopupRoutine()
	void DrawContextNameErrorPopup ()
	{
		//Screen.height-80,120,40);
		
		//float errorPopOutForSecs=1;
		//float errorPopInForSecs=3;
		float horizontalDeltaPerSec=63f;
		float xMax=15;
		GUIContent popupContent=new GUIContent(errorPopupText);
		//while (errorPopOutForSecs>0 | errorPopInForSecs>0)
		//{
		
		GUI.Box(errorPopupRect,popupContent);
		if (errorPopOutForSecs>0)
		{
			errorPopOutForSecs-=Time.deltaTime;
			errorPopupRect.x+=horizontalDeltaPerSec*Time.deltaTime;
			
		}
		else
		{
			
			errorPopInForSecs-=Time.deltaTime;
			errorPopupRect.x-=horizontalDeltaPerSec*Time.deltaTime;
		}
		errorPopupRect.x=Mathf.Clamp(errorPopupRect.x,Mathf.NegativeInfinity,xMax);
		//yield return new WaitForEndOfFrame();
		//}
		if (errorPopOutForSecs<=0 && errorPopInForSecs<=0)
			showErrorPopup=false;
		//yield break;
	}
	
	void DrawReadmeWindow()
	{
		readmeWindowCurrentPos=GUI.Window(5,readmeWindowCurrentPos,DragWindowFunc,"",GUIStyle.none);
		
		readmeWindowrect.x=readmeWindowCurrentPos.x;
		readmeWindowrect.y=readmeWindowCurrentPos.y;
		
		
		GUI.Box(readmeWindowrect,"Помощь",new GUIStyle("window"));
		
		Rect helpTextRect=new Rect(readmeWindowrect);
		helpTextRect.x+=20;
		helpTextRect.y+=40;
		helpTextRect.width-=40;
		helpTextRect.height-=95;	
		Rect scrollDims=new Rect(helpTextRect);
		Rect scrollArea=new Rect(helpTextRect.x,helpTextRect.y,helpTextRect.width-20,readmeTextHeight+5);
		readmeScrollPos=GUI.BeginScrollView(scrollDims,readmeScrollPos,scrollArea,false,false);	
		//GUI.Label(scrollArea,readmeText);
		GUI.Label(scrollArea,readmeText);
		
		
		if (currentCursorLoc==InputManager.CursorLoc.OverGUI)
		{
			Vector2 mousePosInGUICoords = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
			if (readmeWindowrect.Contains(mousePosInGUICoords))
			{
				float scrollbarValueDelta=7.5f;
				if (Input.GetAxis("Mouse ScrollWheel")>0)
				{
					readmeScrollPos.y-=scrollbarValueDelta;
				}
				if (Input.GetAxis("Mouse ScrollWheel")<0)
				{
					readmeScrollPos.y+=scrollbarValueDelta;
				}
				readmeScrollPos.y=Mathf.Clamp(readmeScrollPos.y,0,scrollArea.height);
			}
		}
		GUI.EndScrollView();
		
		Rect closeButtonRect=new Rect(readmeWindowrect);
		closeButtonRect.x+=readmeWindowrect.width-100;
		closeButtonRect.y+=readmeWindowrect.height-40;
		closeButtonRect.width=80;
		closeButtonRect.height=29;
		if (GUI.Button(closeButtonRect,"Закрыть")) {showReadme=false;}
	}
	
	void DrawQuitWithoutSaveDialog()
	{
		GUI.Box(quitWithoutSaveRect,"Подтвердите выход","window");
		GUI.BeginGroup(quitWithoutSaveRect);
		GUI.Label(new Rect(quitWithoutSaveRect.width*0.5f-120,quitWithoutSaveRect.height*0.25f,250,29),"Остались несохраненные изменения");
		GUI.Label(new Rect(quitWithoutSaveRect.width*0.5f-80,quitWithoutSaveRect.height*0.4f,200,29),"Выйти не сохраняя?");
		if (GUI.Button(new Rect(10,quitWithoutSaveRect.height-10-29,80,29),"Сохранить"))
		{
			GameController.mainController.SaveAll();
			Application.Quit();
		}
		if (GUI.Button(new Rect(10+80+5,quitWithoutSaveRect.height-10-29,90,29),"Не сохранять"))
		{
			GameController.mainController.unsavedChangesExist=false;
			Application.Quit();	
		}
		if (GUI.Button(new Rect(10+80+5+90+5,quitWithoutSaveRect.height-10-29,80,29),"Вернуться"))
		{
			//GameController.mainController.unsavedChangesExist=false;
			showQuitWithoutSaveDialog=false;
			//Application.Quit();	
		}
		GUI.EndGroup();
	}
	
	void DrawPushWithEmptyNodesDialog()
	{
		GUI.Box(quitWithoutSaveRect,"Есть пустые адреса","window");
		GUI.BeginGroup(quitWithoutSaveRect);
		GUI.Label(new Rect(quitWithoutSaveRect.width*0.5f-120,quitWithoutSaveRect.height*0.25f,250,29),"Пустые адреса не будут сохранены");
		GUI.Label(new Rect(quitWithoutSaveRect.width*0.5f-80,quitWithoutSaveRect.height*0.4f,200,29),"Продолжить?");
		if (GUI.Button(new Rect(10,quitWithoutSaveRect.height-10-29,80,29),"Да"))
		{
			//GameController.mainController.CallDBPush();
			CallDBPush();
			showEmptyPushDialog=false;
			//Application.Quit();
		}
		/*
		if (GUI.Button(new Rect(10+80+5,quitWithoutSaveRect.height-10-29,90,29),"Не сохранять"))
		{
			GameController.mainController.unsavedChangesExist=false;
			Application.Quit();	
		}*/
		if (GUI.Button(new Rect(10+80+5+90+5,quitWithoutSaveRect.height-10-29,80,29),"Нет"))
		{
			//GameController.mainController.unsavedChangesExist=false;
			showEmptyPushDialog=false;
			//Application.Quit();	
		}
		GUI.EndGroup();
	}
	
	string LoadReadmeText()
	{
		string readmeString="";
		
		string helpPath=Application.dataPath+"/../Readme.txt";
			
			string line;
			StreamReader theReader = new StreamReader(helpPath);

			readmeTextHeight=0;
			using (theReader)
			{
				// While there's lines left in the text file, do this:
				do
				{
					line = theReader.ReadLine();
					
					if (line != null)
					{
						readmeString+=line;
						readmeString+="\n";
						readmeTextHeight+=fbSkin.label.lineHeight;
					}
				}
				while (line != null);
				 
				theReader.Close();
			}
		return readmeString;
	}
	
	protected void DrawFbButton() {
		
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
		
		controller.SetSourceFile(path);
		if (path!=null)
		{
			if (controller.SceneIsLoaded()) 
			{
				selectedLinks.Clear();
				selectedNodes.Clear();
				controller.ClearScene();
			}
			controller.StartLayoutLoad();
		}
		fb=null;
	}
	
	void DragWindowFunc(int emptySig) 
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
								//controller.CreateNewLink(selectedNode.id, clickedNode.id);
							}
							else
							{
								controller.UnchildNode(selectedNode);
								//controller.DeleteLinkBetweenNodes(selectedNode,clickedNode);
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
		GameController.mainController.unsavedChangesExist=true;
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
		NodeDragEnded();
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
				//controller.SaveAll();
				CallSave();
			}
			#else
			if (Input.GetKey(KeyCode.LeftControl))
			{
				if (Input.GetKeyDown (KeyCode.S))
				{
					//controller.SaveAll();\
					CallSave();
				} 
			}
			#endif
		}
	}
	
	void CallSave()
	{
		GameController.mainController.SaveAll();
		//StartCoroutine(SavePopupRoutine());
		PrepSavePopupDraw();
	}
	
	void CallDBPush()
	{
		GameController.mainController.CallDBPush();
		PrepSavePopupDraw();
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
			AddSelectedLinks (actionLink);//HashtableAppendNum(selectedLinks,actionLink);//AddSelectedLinks (actionLink);
			actionLink.selected=true;
			//Make sure sibling is kept selected
			if (sibling!=null)
			{	
				AddSelectedLinks (sibling);//HashtableAppendNum(selectedLinks,actionLink);//AddSelectedLinks (sibling);
				sibling.selected=true;
			}
			break;
		}
			//multiselect
		case MouseClickMode.MultiSelect:
		{
			AddSelectedLinks (actionLink);//HashtableAppendNum(selectedLinks,actionLink);//AddSelectedLinks (actionLink);
			actionLink.selected=true;
			if (sibling!=null)
			{
				AddSelectedLinks (sibling);//HashtableAppendNum(selectedLinks,sibling);//AddSelectedLinks (sibling);
				sibling.selected=true;
			}
			break;
		}
			//ctrl select
		case MouseClickMode.ControlSelect:
		{
			if (actionLink.selected)
			{
				RemoveSelectedLinks (actionLink);//RemoveSelectedLinks(HashtableFindKeyOfValue(actionLink));//RemoveSelectedLinks(actionLink);
				actionLink.selected=false;
				//assume sibling has the same status as main
				if (sibling!=null)
				{
					RemoveSelectedLinks(sibling);//RemoveSelectedLinks(HashtableFindKeyOfValue(sibling));//RemoveSelectedLinks(sibling);
					sibling.selected=false;
				}
			}
			else
			{
				AddSelectedLinks (actionLink);//HashtableAppendNum(actionLink);//AddSelectedLinks(actionLink);
				actionLink.selected=true;
				//assume sibling has the same status as main
				if (sibling!=null)
				{
					AddSelectedLinks (sibling);//HashtableAppendNum(sibling);//AddSelectedLinks(sibling);
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
			SelectionBoxChanged(screenSelectRect.x,screenSelectRect.y,screenSelectRect.xMax, screenSelectRect.yMax);
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
		    && !(readmeWindowrect.Contains(mousePosInGUICoords) && showReadme)
		    && !(quitWithoutSaveRect.Contains(mousePosInGUICoords) && (showQuitWithoutSaveDialog | showEmptyPushDialog))
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
	
	
	void OnApplicationQuit()
	{
		#if UNITY_EDITOR
		{}
		#else
		if (GameController.mainController.unsavedChangesExist && GameController.mainController.HasSourceFile())
		{
			Application.CancelQuit();
			showQuitWithoutSaveDialog=true;
		}
		#endif
	}
	
}
