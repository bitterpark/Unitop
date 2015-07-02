
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using Vectrosity;

namespace Topology {

	public class GameController : MonoBehaviour {

		public Node nodePrefab;
		public Link linkPrefab;

		Hashtable nodes=new Hashtable();
		Hashtable links=new Hashtable();
		VectorLine linksVector;
		VectorLine swapLinksVector;
		//VectorLine selectionBoxLine;
		public float vectorLineThickness=4f;
		Dictionary<Link,Vector3Pair> linkPoints=new Dictionary<Link, Vector3Pair>();
		Dictionary<Link,Color> linkColors=new Dictionary<Link, Color>();
		Dictionary<Link,Vector3Pair> dragConnectLinkPoints= new Dictionary<Link, Vector3Pair >();
		Dictionary<Link,Color> dragConnectLinkColors= new Dictionary<Link, Color>();
		
		public Material linksMat;
		
		GUIText statusText;
		int nodeCount = 0;
		int linkCount = 0;
		GUIText nodeCountText;
		GUIText linkCountText;
		//GUIContainer contextMenu;
		public Rect contextMenuPosNodes;
		public Rect contextMenuPosLinks;
		Rect saveButtonRect=new Rect(5,90,80,20);
		Rect openButtonRect=new Rect(5,60,80,20);
		Rect fileBrowserWindowRect=new Rect(100, 100, 600, 500);
		
		TimerDetector dclickTimer=null;
		
		List<Link> selectedLinks=new List<Link>();
		List<Node> selectedNodes=new List<Node>();
		//Object lastSelectedLinkNode=null;	
		Link lastSelectedLink=null;
		Node lastSelectedNode=null;
		
		string sourceFile;
		//0 - node mode. 1 - link mode
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
		//ctrl, shift or alt select
		int selectMode;
		//something was selected this frame, so no deselect
		bool selectionMade=false;
		
		bool sceneLoaded=false;
		
		//Starting file browser object
		FileBrowser fb;
		//dropselect selection index
		//int listSelectIndex=0;
		Popup selectItemDroplist=new Popup();
		Popup selectColorDroplist=new Popup();
		Popup selectIconDroplist=new Popup();
		public Texture[] nodeTextures;
		public Texture[] colorTextures;
		bool renderList=false;
		public GUISkin fbSkin;
		public GUISkin droplistSkin;
		public Texture2D file,folder;
		
		bool draggingNode=false;
		
		List<Node> nodeCopyBuffer=new List<Node>();
		
		//Method for loading the GraphML layout file
		IEnumerator LoadLayout()
		{
			//print ("loading layout");
			//print ("nodes:"+nodes.Count);
			statusText.text = "Загрузка файла: " + sourceFile;

			//determine which platform to load for
			string xml = null;
			if(Application.isWebPlayer){
				WWW www = new WWW (sourceFile);
				yield return www;
				xml = www.text;
			}
			else{
				StreamReader sr = new StreamReader(sourceFile);
				xml = sr.ReadToEnd();
				sr.Close();
			}

			XmlDocument xmlDoc = new XmlDocument();
			xmlDoc.LoadXml(xml);

			statusText.text = "Загрузка топологии";

			//int scale = 1;//2;

			XmlElement root = xmlDoc.FirstChild as XmlElement;
			for(int i=0; i<root.ChildNodes.Count; i++){
				XmlElement xmlGraph = root.ChildNodes[i] as XmlElement;

				for(int j=0; j<xmlGraph.ChildNodes.Count; j++){
					XmlElement xmlNode = xmlGraph.ChildNodes[j] as XmlElement;
					//create nodes
					if(xmlNode.Name == "node")
					{
						float x = float.Parse(xmlNode.Attributes["x"].Value);
						float y = float.Parse (xmlNode.Attributes["y"].Value);
						float z = 3000;//float.Parse(xmlNode.Attributes["z"].Value);
						
						int iconIndex=0;
						if (xmlNode.HasAttribute("icon")) {iconIndex=int.Parse(xmlNode.Attributes["icon"].Value);}
						CreateNewNode (new Vector2(x,y),xmlNode.Attributes["name"].Value,xmlNode.Attributes["id"].Value,iconIndex);
						//CreateNewNode(new Vector2(x,y),xmlNode.Attributes["name"].Value,xmlNode.Attributes["id"].Value);
						statusText.text = "Загрузка топологии: Вершина " + nodeCount;//nodeObject.id;
						nodeCountText.text = "Вершин: " + nodeCount;
						
						//print ("Icon value is: "+xmlNode.Attributes["icon"].Value);
						//if (xmlNode.HasAttribute) {}
					}

					//create links
					if(xmlNode.Name == "edge")
					{
						CreateNewLink(xmlNode.Attributes["source"].Value,xmlNode.Attributes["target"].Value
						 ,xmlNode.Attributes["id"].Value,xmlNode.Attributes["color"].Value);
						statusText.text = "Загрузка топологии: Ребро " + xmlNode.Attributes["id"].Value;
						
						//linkCountText.text = "Ребер: " + linkCount;
						
					}

					//every 100 cycles return control to unity
					if(j % 100 == 0)
						yield return true;
				}
			}

			//map node edges
			MapLinkNodes();
			UpdateLinkVector();
			statusText.text = "";
			sceneLoaded=true;
		}

		//Method for mapping links to nodes
		private void MapLinkNodes(){
			foreach(string key in links.Keys)
			{
				Link link = links[key] as Link;
				link.source = nodes[link.sourceId] as Node;
				link.target = nodes[link.targetId] as Node;
				linkPoints.Add(link,new Vector3Pair(link.source.transform.position,link.target.transform.position));
				linkColors.Add (link,link.GetColorFromString(link.color));
				//print ("color assigned:"+link.color);
				//linkPoints.Add (link.source.myPos);
				//linkPoints.
				//linkPoints.Add (link.target.myPos);
				//link.sourcePointIndex=linkPoints.FindIndex();
			}
		}
		
		Link AttemptSelectLink() 
		{
			Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
			
			//-1 means no point selected
			//int iSelect = -1;
			Link linkSelect=null;
			float closestYet = Mathf.Infinity;
			
			float selectionThreshold=20f;//0.22f;
			
			//for (int i = 0; i < linkPoints.Count-1; i++) {
			foreach (Vector3Pair points in linkPoints.Values)
			{
				Vector3 closest1;
				Vector3 closest2;
				if (Math3d.ClosestPointsOnTwoLines(out closest1, out  closest2, ray.origin, ray.direction, points.p1, points.p2 - points.p1)) {
					Vector3 v = Math3d.ProjectPointOnLineSegment(points.p1, points.p2, closest1);
					float dist = (v - closest1).magnitude;
					if (dist < closestYet && dist < selectionThreshold) 
					{
						//iSelect = i;
						linkSelect=PointDictionaryFindKey(points);
						closestYet = dist;
					}
				}
			}
			//Returns the index of the point of selected link
			return linkSelect;
		}
		
		Link PointDictionaryFindKey(Vector3Pair value)
		{
			Link retLink=null;
			foreach(Link key in linkPoints.Keys)
			{
				if (linkPoints[key].p1 == value.p1 && linkPoints[key].p2==value.p2) 
				{retLink=key; break;}
			}
			return retLink;	
		}
		
		public void LinkChangeColor(Link changedLink, Color c)
		{
			linkColors[changedLink]=c;
			UpdateLinkColors();
		}
		
		void UpdateLinkColors()
		{
			Color[] arColors=new Color[linkColors.Values.Count];
			linkColors.Values.CopyTo(arColors,0);
			linksVector.SetColors(arColors);
		}
		/*
		void UpdateLinkPoints()
		{	
			//Turn links twinned point dictionary into a point-based draw array
			Vector3[] linksDrawArray=new Vector3[linkPoints.Count*2];
			int k=0;
			foreach(Vector3Pair points in linkPoints.Values)
			{
				linksDrawArray[k]=points.p1;
				k++;
				linksDrawArray[k]=points.p2;
				k++;
			}
			linksVector.points3=linksDrawArray;
			print ("points updated!");
			print ("Points count: "+linksDrawArray.Length);
		}*/
		
		//Call this when points are updated to apply color properly
		void UpdateLinkVector()
		{
			VectorLine.Destroy(ref linksVector);
			//VectorLine.
			Vector3[] linksDrawArray=new Vector3[linkPoints.Count*2];
			int k=0;
			foreach(Vector3Pair points in linkPoints.Values)
			{
				linksDrawArray[k]=points.p1;
				k++;
				linksDrawArray[k]=points.p2;
				k++;
			}
			Color[] arColors=new Color[linkColors.Values.Count];
			linkColors.Values.CopyTo(arColors,0);
			//If there is a vector to draw
			if (linksDrawArray.Length>1)
			{
				
				//float lineWidth=vectorLineThickness*(Screen.height/2)*Camera.main.orthographicSize;
				float lineWidth=vectorLineThickness*Screen.height/(Camera.main.orthographicSize*2);
				linksVector=new VectorLine("All Link Line",linksDrawArray,arColors,linksMat,lineWidth,LineType.Discrete);
				//linksVector.SetColor(Color.black);
				linksVector.sortingOrder=-5;
				linksVector.Draw3D();
				//linksVector.SetWidth=vectorLineThickness;
			}
		}
		
		void CreateNewNode()
		{
			Vector3 newNodePos=Camera.main.ScreenToWorldPoint(Input.mousePosition);//Camera.main.transform.forward*40;;//Camera.main.transform.position+Camera.main.transform.forward*40;
			//newNodePos.z=3000;
			CreateNewNode (newNodePos);
		}
		
		void CreateNewNode(Vector2 newNodePosition) {CreateNewNode(newNodePosition,"127.0.0.1");}
		
		void CreateNewNode(Vector2 newNodePosition,string newNodeText)
		{
			int i=0;
			while (nodes.ContainsKey("node_"+i.ToString()))
			{
				i++;
			}
			string generatedId="node_"+i.ToString();
			CreateNewNode(newNodePosition,newNodeText,generatedId);
		
		}
		
		void CreateNewNode(Vector2 newNodePosition,string newNodeText, string newNodeId)
		{
			Vector3 newNodePos=(Vector3)newNodePosition;//Camera.main.transform.forward*40;;//Camera.main.transform.position+Camera.main.transform.forward*40;
			newNodePos.z=3000;
			
			Node nodeObject = Instantiate(nodePrefab, newNodePos, Quaternion.identity) as Node;
			//nodeObject.nodeText.text = xmlNode.Attributes["name"].Value;
			
			nodeObject.id = newNodeId;
			nodeObject.nodeText.text=newNodeText;
//			nodeObject.myPos=newNodePos;
			nodeObject.controller=this;
			nodes.Add(nodeObject.id, nodeObject);
			nodeCount++;
		}
		
		void CreateNewNode(Vector2 newNodePosition,string newNodeText, string newNodeId, int newNodeSpriteIndex)
		{
			Vector3 newNodePos=(Vector3)newNodePosition;//Camera.main.transform.forward*40;;//Camera.main.transform.position+Camera.main.transform.forward*40;
			newNodePos.z=3000;
			
			Node nodeObject = Instantiate(nodePrefab, newNodePos, Quaternion.identity) as Node;
			//nodeObject.nodeText.text = xmlNode.Attributes["name"].Value;
			
			nodeObject.id = newNodeId;
			nodeObject.nodeText.text=newNodeText;
			//			nodeObject.myPos=newNodePos;
			nodeObject.controller=this;
			nodeObject.SetSprite(newNodeSpriteIndex);
			nodes.Add(nodeObject.id, nodeObject);
			nodeCount++;
		}
		
		void DeleteNode(Node deletedNode)
		{
			nodes.Remove(deletedNode.id);
			nodeCount--;
			//Remove all connecting links
			Link[] iterAr=new Link[links.Count];
			links.Values.CopyTo(iterAr,0);
			foreach(Link link in iterAr)
			{
				if (link.source==deletedNode | link.target==deletedNode)
				{
					DeleteLink(link,null);
				}
			}
			GameObject.Destroy(deletedNode.gameObject);
		}
		
		//For node copy-pasting
		void CopyNodes()
		{
			//print ("nodes copied!");
			if (selectedNodes.Count>0)
			{
				if (nodeCopyBuffer.Count>0) {nodeCopyBuffer.Clear();}
				//selectedNodes.CopyTo(nodeCopyBuffer);
				
				foreach (Node copiedNode in selectedNodes)
				{
					//nodeCopyBuffer
					//nodeCopyBuffer.AddRange(selectedNodes);
					nodeCopyBuffer.Add (copiedNode);
				}
			}
		}
		
		void PasteNodes()
		{
			//print ("nodes pasted!");
			if (nodeCopyBuffer.Count>0)
			{
				//Find center of rectangle formed by copied node formation
				//Start with first buffered node
				float upperLeftX=nodeCopyBuffer[0].transform.position.x;
				float upperLeftY=nodeCopyBuffer[0].transform.position.y;
				float lowerRightX=nodeCopyBuffer[0].transform.position.x;
				float lowerRightY=nodeCopyBuffer[0].transform.position.y;
			
				//if more than one node is being copied
				if (nodeCopyBuffer.Count>1)
				{
					//Find upper left and bottom right conrner of the rectangle
					foreach(Node copiedNode in nodeCopyBuffer)
					{
						if (copiedNode.transform.position.x<upperLeftX) {upperLeftX=copiedNode.transform.position.x;}
						if (copiedNode.transform.position.x>lowerRightX) {lowerRightX=copiedNode.transform.position.x;}
						if (copiedNode.transform.position.y>upperLeftY) {upperLeftY=copiedNode.transform.position.y;}
						if (copiedNode.transform.position.y<lowerRightY) {lowerRightY=copiedNode.transform.position.y;}
					}
					//Find center point of rectangle
					Vector2 centerPoint=Vector2.zero;
					centerPoint.x=upperLeftX+((lowerRightX-upperLeftX)*0.5f);
					centerPoint.y=lowerRightY+((upperLeftY-lowerRightY)*0.5f);
				
					//Find all copied nodes relative to center point
					Vector2 deltaFromCenter=Vector2.zero;
					foreach(Node copiedNode in nodeCopyBuffer)
					{
						deltaFromCenter=(Vector2)copiedNode.transform.position-centerPoint;
						Vector2 pastedNodePos=(Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition)+deltaFromCenter;
						CreateNewNode (pastedNodePos,copiedNode.nodeText.text);
					}
				}
				else if (nodeCopyBuffer.Count==1)
				{
					Vector3 pastedNodePos=Camera.main.ScreenToWorldPoint(Input.mousePosition);
					CreateNewNode (pastedNodePos);
				}
			}
			//nodeCopyBuffer.Clear();
		}
		
		//Method for adding new links
		void CreateNewLink(string newLinkSourceId, string newLinkTargetId)
		{
			//Check if link already exists
			bool exists=false;
			foreach (Link link in links.Values)
			{
				if ((link.sourceId==newLinkSourceId && link.targetId==newLinkTargetId) 
				 | (link.sourceId==newLinkTargetId && link.targetId==newLinkSourceId))
				{
					exists=true;
					//print ("Error: Link already Exists!");
					break;
				}
			}
			if (!exists)
			//Create link
			{
				
				Link linkObject = new Link();//Instantiate(linkPrefab, new Vector3(0,0,0), Quaternion.identity) as Link;
				//find free link id
				int i=0;
				while (links.ContainsKey("link_"+i.ToString()))
				{
					i++;
				}
				
				linkObject.id = "link_"+i.ToString();
				linkObject.color="black";
				linkObject.controller=this;
				//Map links
				linkObject.source = nodes[newLinkSourceId] as Node;
				linkObject.target = nodes[newLinkTargetId] as Node;
				linkObject.sourceId = newLinkSourceId;
				linkObject.targetId = newLinkTargetId;
				//linkObject.status = "Up";
				links.Add(linkObject.id, linkObject);
				linkPoints.Add(linkObject,new Vector3Pair(linkObject.source.transform.position,linkObject.target.transform.position));
				linkColors.Add (linkObject,linkObject.GetColorFromString(linkObject.color));
				
				//Raise count
				linkCount++;
				linkCountText.text = "Ребер: " + linkCount;
				//print ("Link created!");
			}
		}
		
		void CreateNewLink(string newLinkSourceId, string newLinkTargetId, string newLinkId, string newLinkColor)
		{
			//Check if link already exists
			bool exists=false;
			foreach (Link link in links.Values)
			{
				if ((link.sourceId==newLinkSourceId && link.targetId==newLinkTargetId) 
				    | (link.sourceId==newLinkTargetId && link.targetId==newLinkSourceId))
				{
					exists=true;
					//print ("Error: Link already Exists!");
					break;
				}
			}
			if (!exists)
				//Create link
			{
				
				Link linkObject = new Link();//Instantiate(linkPrefab, new Vector3(0,0,0), Quaternion.identity) as Link;
				
				linkObject.id = newLinkId;
				if (newLinkColor!="") {linkObject.color=newLinkColor;}
				else {linkObject.color="black";}
				linkObject.controller=this;
				//Map links
				linkObject.source = nodes[newLinkSourceId] as Node;
				linkObject.target = nodes[newLinkTargetId] as Node;
				linkObject.sourceId = newLinkSourceId;
				linkObject.targetId = newLinkTargetId;
				//linkObject.status = "Up";
				links.Add(linkObject.id, linkObject);
				//Raise count
				
				linkCount++;
				linkCountText.text = "Ребер: " + linkCount;
			}
		
		}
		
		//Remove one link duplex
		void DeleteLink(Link deletedLink, Link sibling)
		{
			links.Remove(deletedLink.id);
			linkPoints.Remove(deletedLink);
			linkColors.Remove(deletedLink);
			//GameObject.Destroy(deletedLink.gameObject);
			linkCount--;
			if (sibling!=null) 
			{
				links.Remove(sibling.id);
				linkPoints.Remove(sibling);
				linkColors.Remove(sibling);
				//GameObject.Destroy(sibling.gameObject);
				linkCount--;
			}
			//UpdateLinkPoints();
			//UpdateLinkColors();
			UpdateLinkVector();
		}
		
		//Remove selected links
		void DeleteSelectedLinks()
		{
			if (selectedLinks.Count>0)
			{
				foreach(Link selectedLink in selectedLinks)
				{
					//Link sibling=GetLinkSibling(selectedLink);
					Link sibling=null;
					DeleteLink(selectedLink,sibling);
				}
				selectedLinks.Clear();
			}
		}
		
		void DeleteSelectedNodes()
		{
			if (selectedNodes.Count>0)
			{
				foreach(Node selectedNode in selectedNodes)
				{
					DeleteNode(selectedNode);
				}
				selectedNodes.Clear();
			}
		}
		
		Link GetLinkSibling(Link checkedLink)
		{
			Link sibling=null;
			string startId=checkedLink.sourceId;
			string endId=checkedLink.targetId;
			if (links.Values.Count>0)
			{
				foreach (Link link in links.Values)
				{
					if (link.targetId==startId && link.sourceId==endId) 
					{
						sibling=link;
						break;
					}
				}
			}
			return sibling; 
		}
		
		//Link click action
		public void ClickLink(Link clickedLink)
		{
			if (ClickApplicable())
			{
				//get sibling (if one exists)
				Link sibling=null;
				//Link sibling=GetLinkSibling(clickedLink);
				//If sibling doesn't exist, pass null
				ClickedLinkAction(clickedLink,sibling);
			}
		}
		
		public void ClickNode(Node clickedNode)
		{
			if (ClickApplicable()) {ClickedNodeAction(clickedNode);}
		}
		/*
		public void ClickNode(Node clickedNode, bool dragged)
		{
			if (dragged) {selectMode=1;}
			ClickedNodeAction(clickedNode);
		}*/
		
		//Called by node when its dragged
		//public void StartDragNode() {draggingNode=true;}
		//public void StopDragNode() {draggingNode=false;}
		
		public void DragNode(Node draggedNode, Vector3 moveDelta)
		{
			foreach (Node node in selectedNodes)
			{
				if (node!=draggedNode) {node.DragAlong(moveDelta);}
				//call all connected links to realign
				foreach (Link link in links.Values)
				{
					if (link.source==node) 
					{//link.loaded=false;
						dragConnectLinkPoints[link]=new Vector3Pair(node.transform.position,dragConnectLinkPoints[link].p2);
					}
					if (link.target==node){dragConnectLinkPoints[link]=new Vector3Pair(dragConnectLinkPoints[link].p1,node.transform.position);}
				}
				//UpdateLinkVector();
			}
			UpdateSwapLinkVector();	
		}
		
		public void NodeDragStart()
		{
			//Swap links in main linkvector for a separate swap object
			foreach(Node node in selectedNodes)
			{
				foreach (Link link in links.Values)
				{
					if (link.source==node | link.target==node) 
					{
						if (!dragConnectLinkPoints.ContainsKey(link))
						{
							dragConnectLinkPoints.Add(link,linkPoints[link]);
							dragConnectLinkColors.Add (link,linkColors[link]);
							linkPoints.Remove(link);
							linkColors.Remove(link);
						}
					}
					if (link.target==node){}//linkPoints[link]=new Vector3Pair(linkPoints[link].p1,node.transform.position);}
				}
			}
			UpdateLinkVector();
			UpdateSwapLinkVector();
			/*
			//Create new swap line drawer
			VectorLine.Destroy(ref swapLinksVector);
			Vector3[] linksDrawArray=new Vector3[dragConnectLinkPoints.Count*2];
			int k=0;
			foreach(Vector3Pair points in dragConnectLinkPoints.Values)
			{
				linksDrawArray[k]=points.p1;
				k++;
				linksDrawArray[k]=points.p2;
				k++;
			}
			Color[] arColors=new Color[dragConnectLinkColors.Values.Count];
			linkColors.Values.CopyTo(arColors,0);
			//If there is a vector to draw
			if (linksDrawArray.Length>1)
			{
				
				//float lineWidth=vectorLineThickness*(Screen.height/2)*Camera.main.orthographicSize;
				float lineWidth=vectorLineThickness*Screen.height/(Camera.main.orthographicSize*2);
				swapLinksVector=new VectorLine("All Link Line",linksDrawArray,arColors,linksMat,lineWidth,LineType.Discrete);
				//linksVector.SetColor(Color.black);
				swapLinksVector.sortingOrder=-5;
				swapLinksVector.Draw3D();
				//linksVector.SetWidth=vectorLineThickness;
			}*/
		}
		
		//Put connecting link drawing back into the main renderer
		public void NodeDragComplete()
		{
			VectorLine.Destroy(ref swapLinksVector);
			foreach(Node node in selectedNodes)
			{
				foreach (Link link in links.Values)
				{
					if (link.source==node | link.target==node) 
					{
						if (dragConnectLinkColors.ContainsKey(link))
						{
							linkPoints.Add(link,dragConnectLinkPoints[link]);
							linkColors.Add (link,dragConnectLinkColors[link]);
							dragConnectLinkPoints.Remove(link);
							dragConnectLinkColors.Remove(link);
						}
					}
				}
			}
			UpdateLinkVector();
		}
		
		void UpdateSwapLinkVector()
		{
			//Create new swap line drawer
			VectorLine.Destroy(ref swapLinksVector);
			Vector3[] linksDrawArray=new Vector3[dragConnectLinkPoints.Count*2];
			int k=0;
			foreach(Vector3Pair points in dragConnectLinkPoints.Values)
			{
				linksDrawArray[k]=points.p1;
				k++;
				linksDrawArray[k]=points.p2;
				k++;
			}
			Color[] arColors=new Color[dragConnectLinkColors.Values.Count];
			dragConnectLinkColors.Values.CopyTo(arColors,0);
			//If there is a vector to draw
			if (linksDrawArray.Length>1)
			{
				float lineWidth=vectorLineThickness*Screen.height/(Camera.main.orthographicSize*2);
				swapLinksVector=new VectorLine("All Link Line",linksDrawArray,arColors,linksMat,lineWidth,LineType.Discrete);
				swapLinksVector.sortingOrder=-5;
				swapLinksVector.Draw3D();
			}
		
		}
		
		void HandleSelectMode()
		{
			selectMode=0;
			if (Input.GetKey("left shift")) {selectMode=1;}
			if (Input.GetKey("left ctrl")) {selectMode=2;}
			if (Input.GetKey("left alt"))  {selectMode=3;}
			//if (draggingNode) {selectMode=1;}
		
		}
		
		/*
		void HashtableAppendNum( Hashtable apTable, Object apObj)
		{
			int i=0;
			while(apTable.ContainsKey(i))
			{
				i++;
			}
			apTable.Add(i,apObj);
		}
		
		int HashtableFindKeyOfValue(Hashtable searchedTable, Object value)
		{
			int ret=0;
			foreach (int key in searchedTable.Keys)
			{
				if(searchedTable[key]==value) {ret=key; break;}
			}
			return ret;
		}
		
		//N starts at 1
		Object HashtableGetNthElement(Hashtable iterTable, int n)
		{
			if (iterTable.Count<n) return -1;
			int i=-1;
			int j=0;
			while(j<n)
			{
				i++;
				if (iterTable.ContainsKey(i)) {j++;}
			}
			return iterTable[i];
		}
		
		//Orders hasthbale values numerically
		Object[] HashtableToArrayInOrder(Hashtable hashToAr)
		{
			Object[] returnAr=new Object[hashToAr.Count];
			int i=0;
			int j=0;
			while(j<hashToAr.Count)
			{
				if (iterTable.ContainsKey(i)) 
				{
					returnAr[j]=hashToAr[i];
					j++;
				}
				i++;
			}
			
			return iterTable[j];
		}
		*/
		//Do action based on select mode
		void ClickedLinkAction(Link actionLink, Link sibling)
		{
			selectionMade=true;
			DeselectAllNodes();
			//drawTooltip=true;
			tooltipMode=1;
			switch (selectMode)
			{
				//Single select
				case 0:
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
				case 1:
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
				case 2:
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
	
		void ClearXmlNodes()
		{
			string filepath = sourceFile;
			XmlDocument xmlDoc = new XmlDocument();
			
			if(File.Exists (filepath))
			{
				xmlDoc.Load(filepath);
				
				XmlNode elmRoot = xmlDoc.DocumentElement.FirstChild;
				List<XmlNode> linkList=new List<XmlNode>();
				//Find link nodes in children of main node
				foreach (XmlNode childLink in elmRoot.ChildNodes) 
				{
					if (childLink.Name=="node") {linkList.Add(childLink);}
				}
				//Remove all link nodes from xml file
				foreach (XmlNode link in linkList)
				{
					elmRoot.RemoveChild(link);
				}
				xmlDoc.Save(filepath);
			}
		}
		
		void WriteNodesToXml()
		{
			string filepath = sourceFile;
			XmlDocument xmlDoc = new XmlDocument();
			
			if(File.Exists (filepath))
			{
				xmlDoc.Load(filepath);
				
				XmlNode elmRoot = xmlDoc.DocumentElement.FirstChild;
				
				if (nodes.Count>0)
				{
					foreach (Node node in nodes.Values)
					{
						XmlElement savedNode=xmlDoc.CreateElement("node");
						savedNode.SetAttribute("id",node.id);
						savedNode.SetAttribute("name",node.nodeText.text);
						Vector3 savedNodePos=node.gameObject.transform.position;
						savedNode.SetAttribute("x",savedNodePos.x.ToString());
						savedNode.SetAttribute("y",savedNodePos.y.ToString());
						savedNode.SetAttribute("z",savedNodePos.z.ToString());
						savedNode.SetAttribute("icon",node.GetSpriteIndex().ToString());
						savedNode.SetAttribute("xmlns","http://graphml.graphdrawing.org/xmlns");
						elmRoot.AppendChild(savedNode);
					}
				}
				xmlDoc.Save(filepath); // save file.
			}
		}
		
		//Method for saving new links
		void SaveAll()
		{
			ClearXmlLinks();
			WriteLinksToXml();
			ClearXmlNodes();
			WriteNodesToXml();
		}
		
		void ClearXmlLinks()
		{
			string filepath = sourceFile;
			XmlDocument xmlDoc = new XmlDocument();
			
			if(File.Exists (filepath))
			{
				xmlDoc.Load(filepath);
				
				XmlNode elmRoot = xmlDoc.DocumentElement.FirstChild;
				List<XmlNode> linkList=new List<XmlNode>();
				//Find link nodes in children of main node
				foreach (XmlNode childLink in elmRoot.ChildNodes) 
				{
					if (childLink.Name=="edge") {linkList.Add(childLink);}
				}
				//Remove all link nodes from xml file
				foreach (XmlNode link in linkList)
				{
					elmRoot.RemoveChild(link);
				}
				xmlDoc.Save(filepath);
			}
		
		}
		
		void WriteLinksToXml()
		{
			
			string filepath = sourceFile;
			XmlDocument xmlDoc = new XmlDocument();
			
			if(File.Exists (filepath))
			{
				xmlDoc.Load(filepath);
				
				XmlNode elmRoot = xmlDoc.DocumentElement.FirstChild;
				
				if (links.Count>0)
				{
					foreach (Link link in links.Values)
					{
						XmlElement savedLink= xmlDoc.CreateElement("edge");
						savedLink.SetAttribute("id",link.id);
						savedLink.SetAttribute("source",link.sourceId);
						savedLink.SetAttribute("target",link.targetId);	
						savedLink.SetAttribute("color",link.color);
						savedLink.SetAttribute("label","1000");
						savedLink.SetAttribute("status",link.status);
						savedLink.SetAttribute("type","Half");
						//savedLink.SetAttribute("xmlns",elmRoot.GetPrefixOfNamespace(elmRoot.NamespaceURI));
						savedLink.SetAttribute("xmlns","http://graphml.graphdrawing.org/xmlns");
						elmRoot.AppendChild(savedLink);
					}
				}
				xmlDoc.Save(filepath); // save file.
			}
			
		}
		
		public void ClickedNodeAction(Node clickedNode, bool dragged)
		{
			if (dragged) {selectMode=1;}
			ClickedNodeAction(clickedNode);
		}
		
		public void ClickedNodeAction(Node clickedNode)
		{
			selectionMade=true;
			DeselectAllLinks();
			tooltipMode=0;
			// single select mode
			if (selectMode==0)
			{	
				DeselectAllNodes();
				selectedNodes.Add(clickedNode);//HashtableAppendNum(selectedNodes,clickedNode);//selectedNodes.Add(clickedNode);
				clickedNode.selected=true;
			}
			
			//shift select mode
			if (selectMode==1)
			{ 
				//selectedNodes.Add(clickedNode);
				if (!selectedNodes.Contains(clickedNode))
				{
					selectedNodes.Add(clickedNode);//HashtableAppendNum(selectedNodes,clickedNode);
					clickedNode.selected=true;
				}
			}
			
			// ctrl(alt) select mode
			if (selectMode==2) 
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
			if (selectMode==3)
			{
				if (selectedNodes.Count>0) 
				{
					foreach (Node selectedNode in selectedNodes)
					{
						if (selectedNode.id!=clickedNode.id)
						{
							
							CreateNewLink(selectedNode.id,clickedNode.id);
							//CreateNewLink(clickedNode.id,selectedNode.id);
						}
					}
					UpdateLinkVector();
				}
			}
		}
		
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
				GUI.Box(contextMenuPosNodes,"");
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
				GUIContent[] iconDroplistContent=new GUIContent[4];
				iconDroplistContent[0]=new GUIContent("WinXP",nodeTextures[0]);
				iconDroplistContent[1]=new GUIContent("Win07",nodeTextures[1]);
				iconDroplistContent[2]=new GUIContent("2008",nodeTextures[2]);
				iconDroplistContent[3]=new GUIContent("2003",nodeTextures[3]);
				//select icon droplist
				selectIconDroplist.List(new Rect(leftColumnStartX+elementSizeX*0.5f,leftColumnStartY+elementSizeY*2+vPad*2,elementSizeX,elementSizeY)
				 ,iconDroplistContent,"box",droplistSkin.customStyles[0]);
				//Set new icon for all selected nodes
				foreach (Node node in selectedNodes) {node.SetSprite(selectIconDroplist.GetSelectedItemIndex());}
				//currentNode.SetSprite(selectIconDroplist.GetSelectedItemIndex());
				
				//select obj menu (must be last item rendered)
				selectItemDroplist.List(new Rect(leftColumnStartX,leftColumnStartY+elementSizeY,elementSizeX,elementSizeY)
				 ,droplistContent,"box",droplistSkin.customStyles[0]);
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
				int droplistPick=selectColorDroplist.List(new Rect(rightColumnStartX,rightColumnStartY+elementSizeY,elementSizeX,elementSizeY)
				 ,droplistColorContent,"box",droplistSkin.customStyles[0]); 
				switch (droplistPick)
				{
					case 0:{coloredLink.color="black"; break;}
					case 1:{coloredLink.color="red"; break;}
					case 2:{coloredLink.color="green"; break;}
					case 3:{coloredLink.color="yellow"; break;}
					case 4:{coloredLink.color="cyan"; break;}
				}
				
				//select obj menu (must be after endgroup rendered)
				selectItemDroplist.List(new Rect(leftColumnStartX,leftColumnStartY+elementSizeY,elementSizeX,elementSizeY)
				 ,droplistContent,"box",droplistSkin.customStyles[0]);
				//lastSelectedLinkNode=selectedLinks[selectItemDroplist.GetSelectedItemIndex()];
				lastSelectedLink=selectedLinks[selectItemDroplist.GetSelectedItemIndex()];
			}
		}
		
		void Start () 
		{
			//nodes = new Hashtable();
			//links = new Hashtable();
			//linksVector = new VectorLine("poop",new Vector3[],
			
			//selectedLinks=new List<Link>();
			//selectedNodes=new List<Node>();
			
			//selectedLinks=new Hashtable();
			//selectedNodes=new Hashtable();
			//initial stats
			nodeCountText = GameObject.Find("NodeCount").guiText;
			nodeCountText.text = "Вершин: 0";
			linkCountText = GameObject.Find("LinkCount").guiText;
			linkCountText.text = "Ребер: 0";
			statusText = GameObject.Find("StatusText").guiText;
			statusText.text = "";
		}
		
		void ClearScene()
		{
			StopAllCoroutines();
			foreach (Node node in nodes.Values) {GameObject.Destroy(node.gameObject);}
			nodes.Clear();
			//print ("cleared nodes, nodes:"+nodes.Count);
			links.Clear();
			selectedLinks.Clear();
			selectedNodes.Clear();
			nodeCountText.text = "Вершин: 0";
			linkCountText.text = "Ребер: 0";
			statusText.text = "";
			VectorLine.Destroy(ref linksVector);
			linkPoints.Clear();
			linkColors.Clear();
			//awaitingPath=true;
			sceneLoaded=false;
			
		}
		
		//Deselect current selection on clicking empty space
		void ManageClickDeselect()
		{
			if (Input.GetMouseButtonDown(0) && sceneLoaded) 
			{
				Vector2 mousePosInGUICoords = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
				if (ClickApplicable())
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
				if (ClickApplicable())
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
			//if (Input.GetMouseButton(0)) {print ("Select box primed");}
			while(Input.GetMouseButton(0))
			{	
				selectionBoxLine.MakeRect (originalPos, Input.mousePosition);
				selectionBoxLine.Draw();
				selectionBoxLine.textureOffset = -Time.time*2.0f % 1;
				
				//screenSelectRect.x=Mathf.Max(originalPos.x,Input.mousePosition.x);
				//screenSelectRect.y=Mathf.Max(originalPos.y,Input.mousePosition.y);
				//screenSelectRect.xMax=Mathf.Max(originalPos.x,Input.mousePosition.x);
				//screenSelectRect.yMax=Mathf.Max(originalPos.x,Input.mousePosition.x);
				//screenSelectRect.x=originalPos.x;
				selectMode=1;
				screenSelectRect.position=originalPos;
				screenSelectRect.xMax=Input.mousePosition.x;
				screenSelectRect.yMax=Input.mousePosition.y;
				
				//DeselectAllNodes();
				
				foreach (Node node in nodes.Values)
				{
					if (screenSelectRect.Contains(Camera.main.WorldToScreenPoint(node.transform.position),true)) 
					{
						ClickedNodeAction(node);
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
			if (Input.GetMouseButtonDown(0) && sceneLoaded) 
			{
				//Vector2 mousePosInGUICoords = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
				if (ClickApplicable())
				{	
					if (dclickTimer==null) {StartCoroutine(ManageDoubleclickTimer());}
				}
		
			}
		}
		
		//checks if select mode is right and the click doesn't land on GUI elements
		bool ClickApplicable()
		{
			bool clickApplicable=true;
			Vector2 mousePosInGUICoords = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
			if (selectMode==0 && !selectionMade && sceneLoaded && (fb==null | !fileBrowserWindowRect.Contains(mousePosInGUICoords))
			    && !openButtonRect.Contains(mousePosInGUICoords)
			    && !saveButtonRect.Contains(mousePosInGUICoords)
			    && !contextMenuPosNodes.Contains(mousePosInGUICoords) 
			    && !contextMenuPosLinks.Contains(mousePosInGUICoords)
			    && !selectItemDroplist.GetCurrentDimensions().Contains(mousePosInGUICoords) 
			    && !selectColorDroplist.GetCurrentDimensions().Contains(mousePosInGUICoords)
			    && !selectIconDroplist.GetCurrentDimensions().Contains(mousePosInGUICoords))
			    {clickApplicable=true;} else {clickApplicable=false;}
			return clickApplicable;
		}
		
		IEnumerator ManageDoubleclickTimer()
		{
			//if (Input.GetMouseButtonUp(0))
			//{
			dclickTimer=new TimerDetector(0.22f);
			yield return new WaitForFixedUpdate();
			while (!dclickTimer.UpdateTimer())
			{
				if (Input.GetMouseButtonDown(0) && sceneLoaded) 
				{
					Vector2 mousePosInGUICoords = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
					//make sure it doesn't fire while over gui elements
					if (ClickApplicable())
					{CreateNewNode(); break;}
				}
				yield return new WaitForFixedUpdate();	
			}
			yield return new WaitForFixedUpdate();
			dclickTimer=null;
			yield break;
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
		
		/*
		Link GetLinkFromPointIndex(int index)
		{
			Link ret=null;
			foreach (Link link in links.Values)
			{
				if (link.sourcePointIndex==index) {ret=link; break;}
			}
			return ret;
		}*/
		
		//fires before physics clicks and Update
		void FixedUpdate()
		{
			HandleSelectMode();
				
		}		
		
		//fires after physics clicks
		void Update()
		{
			//manage input
			ManageLinkSelection();
			
			ManageClickDeselect();
			ManageDoubleclick();
			ManageSelectionBox();
			
			ManageObjectDeletion();
			ManageObjectCopying();
			//Prepare selection var for next frame
			selectionMade=false;
			if (Input.GetKeyDown(KeyCode.O)) {ClearScene();}
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
		
		void ManageObjectDeletion()
		{
			if (Input.GetKeyDown (KeyCode.Delete))
			{
				if (selectedNodes.Count>0){DeleteSelectedNodes();}
				if (selectedLinks.Count>0){DeleteSelectedLinks();}
			}
		}
		
		void ManageObjectCopying()
		{
			if (Input.GetKeyDown (KeyCode.C))
			{
				CopyNodes();
				//Event.KeyboardEvent
			}
			if (Input.GetKeyDown (KeyCode.V))//if (Input.GetKey(KeyCode.LeftControl) &&Input.GetKeyDown(KeyCode.V))
			{
				PasteNodes();
			}
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
			if (sceneLoaded) {if (GUI.Button(saveButtonRect,"Сохранить")) {SaveAll();}}
			ManageTooltip();
		}
		
		/*
		void DrawMenu()
		{
			string[] menuButtons=new string[2];
			menuButtons[0]="Открыть...";
			menuButtons[1]="Сохранить";
			
			int menuSelect=GUI.SelectionGrid(new Rect(0,0,1024,50), 2, menuButtons, menuButtons.Length,droplistSkin.customStyles[0]); 
			print ("selected on menu:"+menuSelect);
			
		}*/
		
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
			
			sourceFile = path;
			if (sourceFile!=null)
			{
				if (sceneLoaded) {ClearScene();}
				StartCoroutine( LoadLayout() );
			}
			fb=null;
		}

	}

}
