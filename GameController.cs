
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using Vectrosity;
using Npgsql;

namespace Topology {

	public class GameController : MonoBehaviour {

		public Node nodePrefab;
		
		Hashtable nodes=new Hashtable();
		Hashtable links=new Hashtable();
		
		
		public LinkDrawManager linkDrawManager;
		public InputManager myInputManager;
		
		GUIText nodeCountText;
		GUIText linkCountText;
		public GUIText statusText;
		
		
		int nodeCount = 0;
		int linkCount = 0;

		public Texture2D[] nodeIconTextures;
		
		string sourceFile;
		bool sceneLoaded=false;
		bool unsavedChanges=false;
		public bool unsavedChangesExist 
		{
			get {return unsavedChanges;}
			set {unsavedChanges=value;}
		}
		
		public void SetSourceFile(string filePath) 
		{
			if (filePath!=null) {sourceFile=filePath;}
		}
		public bool HasSourceFile() 
		{
			bool hasFile=false;
			if (sourceFile!=null) {hasFile=true;}
			return hasFile;
		}
		//public bool GetSourceFile() {return sourceFile;}	
		
		public Hashtable GetLinks() {return links;}
		public Hashtable GetNodes() {return nodes;}
		public List<Node> GetRootNodes() {return rootNodes;}
		public Dictionary<Node,List<Node>> GetNodeTrees() {return nodeTrees;}
		List<Node> rootNodes=new List<Node>();
		Dictionary<Node,List<Node>> nodeTrees=new Dictionary<Node, List<Node>>();
		
		public Texture2D[] GetNodeTextures () {return nodeIconTextures;}
		
		public enum OSTypes {WinXP,WinVista,Win7,Win8,Server2000,Server2003,Server2008,Server2012,Linux,MacOS};
		
		List<Node> nodeCopyBuffer=new List<Node>();
		
		public static GameController mainController;
		
		public void StartLayoutLoad() 
		{
			StartCoroutine(LoadLayout());
		}
		
		public void StartDBLoad() 
		{
			sourceFile=null;
			LoadLayoutFromDB();
		}
		
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
						DownwardRecursiveNodeLoad(xmlNode);
					}

					//create links
					if(xmlNode.Name == "edge")
					{
						 //Nodes are mapped in later in MapLinkNodes. It makes sure all the nodes are loaded in and created first.
						 //It is added to link draw manager in MapLinkNodes
						CreateNewLinkWithUnmappedConnectNodes(xmlNode.Attributes["source"].Value,xmlNode.Attributes["target"].Value
						 ,xmlNode.Attributes["id"].Value,xmlNode.Attributes["color"].Value);
						statusText.text = "Загрузка топологии: Ребро " + xmlNode.Attributes["id"].Value;
						//linkCountText.text = "Ребер: " + linkCount;
					}

					//every 100 cycles return control to unity
					if(j % 1000 == 0)
						yield return true;
				}
			}

			SetUpNodeFormations();
			//map node edges
			MapLinkNodes();
			statusText.text = "";
			LoadCameraPos();
//			myInputManager.StartDrawnNodeList();
			sceneLoaded=true;
			unsavedChanges=false;
		}
		
		
		//Loads nodes from file and creates them in the scene
		Node LoadNode(XmlElement xmlNode, Vector2 loadedNodePos)
		{
			
			//create nodes
			/*
			float x = float.Parse(xmlNode.Attributes["x"].Value);
			float y = float.Parse (xmlNode.Attributes["y"].Value);
			float z = 3000;//float.Parse(xmlNode.Attributes["z"].Value);
			*/
			int iconIndex=0;
			//set icon
			if (xmlNode.HasAttribute("icon")) {iconIndex=int.Parse(xmlNode.Attributes["icon"].Value);}
			//create root node
			Node retNode=CreateNewNode (loadedNodePos,xmlNode.Attributes["name"].Value,xmlNode.Attributes["id"].Value,iconIndex);
			statusText.text = "Загрузка топологии: Вершина " + nodeCount;//nodeObject.id;
			nodeCountText.text = "Вершин: " + nodeCount;
			return retNode;		
		}
		
		//For loading root nodes
		Node DownwardRecursiveNodeLoad(XmlElement xmlNode)
		{
			float x = float.Parse(xmlNode.Attributes["x"].Value);
			float y = float.Parse (xmlNode.Attributes["y"].Value);
			//float z = 3000;//float.Parse(xmlNode.Attributes["z"].Value);
			return DownwardRecursiveNodeLoad(xmlNode,new Vector2(x,y));
		}
		
		//For loading node children and children of children
		Node DownwardRecursiveNodeLoad(XmlElement xmlNode,Vector2 loadedNodePos)
		{
			Node retNode=LoadNode(xmlNode, loadedNodePos);
			if (xmlNode.HasChildNodes)
			{
				/*
				float yOffsetFromParent=-512f;
				float yRowPad=-384f;
				float xPad=256;
				float elementSizeX=256;
				float currentXOffset=0;
				float currentYOffset=yOffsetFromParent;
				int rowCounter=0;
				int rowTotalLength=1;
				int rowCursor=0;
				//int i=0;
				*/
				XmlNodeList childNodes=xmlNode.ChildNodes;
				foreach (XmlElement child in childNodes)
				{			
					//i++;
					float childX=float.Parse(child.Attributes["x"].Value);//loadedNodePos.x+currentXOffset;
					float childY=float.Parse(child.Attributes["y"].Value);//loadedNodePos.y+currentYOffset;
					//print ("Step no:"+i);
					//print ("Parent pos is:"+loadedNodePos);
					//print ("x offset is:"+currentXOffset);
					//print ("y offset is:"+currentYOffset);
					//Vector2 childPos=new Vector2();
					Node newNode=DownwardRecursiveNodeLoad(child, new Vector2(childX,childY));//LoadNode(child);
					/*
					rowCursor++;
					if (rowCursor==rowTotalLength)
					{
						rowCounter++;
						rowTotalLength+=2;
						rowCursor=0;
						currentYOffset+=yRowPad;
						currentXOffset=-xPad*rowCounter;
					}
					else
					{
						currentXOffset+=xPad;
					}*/
					SetNodeAsChild(newNode,retNode);
				}
			}
			return retNode;
		}
		
		
		void SetUpNodeFormations()
		{
			foreach(Node rootNode in rootNodes)
			{
				if (nodeTrees.ContainsKey(rootNode))
				{
					float downwardRefX=0;
					float downwardRefY=0;
					FormHierarchyTriangle(rootNode, rootNode,ref downwardRefX,ref downwardRefY);
				}
			}
		}
		
		void FormHierarchyTriangle(Node parent, Node rootParent, ref float occupiedWidthToLeft, ref float occupiedWidthToRight)
		{
			if (nodeTrees.ContainsKey(parent))
			{
				float yOffsetFromParent=-896f;
				float yRowPad=-512f;
				float xPad=512f;
				//float elementSizeX=256;
				float currentXOffset=0;
				float currentYOffset=yOffsetFromParent;
				int rowCounter=0;
				int rowTotalLength=1;
				int rowCursor=0;
				
				//Determine which children go underneath the parent and
				//which get their own pyramid to the side
				List<Node> pyramidChildren=new List<Node>();
				List<Node> childrenWidthOwnPyramids=new List<Node>();
				foreach(Node child in nodeTrees[parent])
				{
					if (!nodeTrees.ContainsKey(child))
					{
						pyramidChildren.Add(child);
					}
					else {childrenWidthOwnPyramids.Add(child);}
				}
				
				foreach(Node pyramidChild in pyramidChildren)
				{
					float pyramidChildX=parent.transform.position.x+currentXOffset;
					float pyramidChildY=parent.transform.position.y+currentYOffset;
					//Node newNode=DownwardRecursiveNodeLoad(pyramidChild, new Vector2(childX,childY));//LoadNode(child);
					pyramidChild.transform.position=new Vector3(pyramidChildX,pyramidChildY,3000);
					
					rowCursor++;
					if (rowCursor==rowTotalLength)
					{
						rowCounter++;
						rowTotalLength+=2;
						rowCursor=0;
						currentYOffset+=yRowPad;
						currentXOffset=-xPad*rowCounter;
					}
					else
					{
						currentXOffset+=xPad;
					}
					//Prepare for offset from parent's parent
					pyramidChild.gameObject.transform.parent=parent.gameObject.transform;
				}
				float pyramidHalfWidth=xPad*rowCounter;
				
				//See if offseting from parent pyramid is necessary
				if (parent!=rootParent)
				{
					bool goingLeft=true;
					if (occupiedWidthToLeft>occupiedWidthToRight) 
					{goingLeft=false;}
				
					float pyramidXOffsetFromParent=0;
					float pyramidYOFfsetFromParent=parent.parentNode.transform.position.y+yOffsetFromParent;
					if (goingLeft)
					{
						pyramidXOffsetFromParent=rootParent.transform.position.x-(occupiedWidthToLeft+pyramidHalfWidth);
						//Update left width if going elft as child
						occupiedWidthToLeft+=pyramidHalfWidth*2;
					}
					else
					{
						pyramidXOffsetFromParent=rootParent.transform.position.x+occupiedWidthToRight+pyramidHalfWidth;
						//Update right width if going right as child
						occupiedWidthToRight+=pyramidHalfWidth*2;
					}	
					parent.transform.position=new Vector3(pyramidXOffsetFromParent,pyramidYOFfsetFromParent,3000);
				}
				else 
				{
					//Update occupied width for root node
					occupiedWidthToLeft+=pyramidHalfWidth;
					occupiedWidthToRight+=pyramidHalfWidth;
				}
				//Unanchor children
				foreach (Node anchoredChild in pyramidChildren)
				{
					anchoredChild.gameObject.transform.parent=null;
				}
				
				//Recursively form up child trees
				foreach (Node childTreeParent in childrenWidthOwnPyramids)
				{
					FormHierarchyTriangle(childTreeParent,rootParent,ref occupiedWidthToLeft,ref occupiedWidthToRight);
				}
			}
		}
		
		//Method for mapping links to nodes
		void MapLinkNodes()
		{
			//Prepare all drawn links
			Link[] drawnLinks=new Link[links.Count];
			int i=0;
			foreach(string key in links.Keys)
			{
				Link link = links[key] as Link;
				link.source = nodes[link.sourceId] as Node;
				link.target = nodes[link.targetId] as Node;
				
				//Put this here to save iterations, might move later
				//Setup link drawing
				drawnLinks[i]=link;
				i++;
				//linkPoints.Add(link,new Vector3Pair(link.source.transform.position,link.target.transform.position));
				//linkColors.Add (link,link.GetColorFromString(link.color));
			}
			linkDrawManager.AddDrawnLinks(drawnLinks);
		}
		
		public bool SceneIsLoaded() {return sceneLoaded;}
		
		
		public void LinkChangeColor(Link changedLink, Color c)
		{
			//linkColors[changedLink]=c;
			//UpdateLinkColors();
			linkDrawManager.LinkChangeColor(changedLink,c);
		}
		/*
		void Update()
		{
			statusText = GameObject.Find("StatusText").guiText;
			statusText.text = Time.deltaTime.ToString();
		}*/
		
		public void CreateNewNode()
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
		
		void CreateNewNode(Vector2 newNodePosition,string newNodeText,int newNodeSpriteIndex)
		{
			int i=0;
			while (nodes.ContainsKey("node_"+i.ToString()))
			{
				i++;
			}
			string generatedId="node_"+i.ToString();
			CreateNewNode(newNodePosition,newNodeText,generatedId,newNodeSpriteIndex);
		}
		
		void CreateNewNode(Vector2 newNodePosition,string newNodeText, string newNodeId)
		{
			CreateNewNode (newNodePosition,newNodeText,newNodeId,0);
		}
		
		Node CreateNewNode(Vector2 newNodePosition,string newNodeText, string newNodeId, int newNodeSpriteIndex)
		{
			Vector3 newNodePos=(Vector3)newNodePosition;//Camera.main.transform.forward*40;;//Camera.main.transform.position+Camera.main.transform.forward*40;
			newNodePos.z=3000;
			
			Node nodeObject = Instantiate(nodePrefab, newNodePos, Quaternion.identity) as Node;
			
			nodeObject.id = newNodeId;
			nodeObject.text=newNodeText;//+"\nballs";
			//			nodeObject.myPos=newNodePos;
			nodeObject.controller=this;
			nodeObject.SetSprite(newNodeSpriteIndex);
			nodes.Add(nodeObject.id, nodeObject);
			rootNodes.Add(nodeObject);
			nodeCount++;
			unsavedChanges=true;
			return nodeObject;
		}
		
		public bool NodeExists(Node node)
		{
			bool exists=false;
			foreach(Node iterNode in nodes.Values)
			{
				if (iterNode==node) {exists=true; break;}
			}
			return exists;
		}
		
		/*
		public void ToggleNodeHierarchy(Node child, Node parent)
		{
			if (child.parentNode==parent) {UnchildNode(child);}
			else 
			{
				UnchildNode(child);
				SetNodeAsChild(child,parent);
			}		
		}*/
		
		
		public void SetNodeAsChild(Node child, Node parent)
		{
				//Make sure the new child is not his parent's parent or grandparent
				Node upwardIteratorParent=parent;
				bool childIsNotParentInTree=true;
				while (upwardIteratorParent!=null)
				{
					if (upwardIteratorParent.parentNode==child) 
					{
						childIsNotParentInTree=false;
						break;
					} 
					else {upwardIteratorParent=upwardIteratorParent.parentNode;}
				}
				
				if (childIsNotParentInTree)
				{
					UnchildNode(child);
					RemoveNodeFromRoot(child);
					if (!nodeTrees.ContainsKey(parent)) {nodeTrees.Add(parent, new List<Node>());}
					nodeTrees[parent].Add (child);
					child.parentNode=parent;
					parent.hasChildren=true;
					CreateNewLink(child.id,parent.id);
				}
		}
		
		//Remove node from each tree it is a child of
		public void UnchildNode(Node child)
		{
			if (child.parentNode!=null)
			{
				nodeTrees[child.parentNode].Remove(child);
				if (nodeTrees[child.parentNode].Count==0) 
				{
					nodeTrees.Remove(child.parentNode);
					child.parentNode.hasChildren=false;
				}
				List<Link> connectedLinks=new List<Link>();
				foreach (Link link in links.Values)
				{
					if (link.source==child | link.target==child)
					{
						connectedLinks.Add(link);
					}
				}
				foreach (Link connectedLink in connectedLinks)
				{
					DeleteLink(connectedLink,null);
				}
				//DeleteLinkBetweenNodes(child,child.parentNode);
				child.parentNode=null;
				rootNodes.Add(child);
			}
		}
		
		void RemoveNodeFromRoot(Node removedNode)
		{
			if (rootNodes.Contains(removedNode)) {rootNodes.Remove(removedNode);}
		}
		
		//Put all children in root and remove tree
		void RemoveParentedTree(Node parent)
		{
			if (nodeTrees.ContainsKey(parent)) 
			{
				//Return children of deleted node to root
				foreach (Node childNode in nodeTrees[parent]) 
				{
					rootNodes.Add(childNode);
					childNode.parentNode=null;
				}
				parent.hasChildren=false;
				nodeTrees.Remove(parent);
			}
		}
		
		public Node FindParentOfChild(Node child)
		{
			Node retNode=null;
			List<Node> foundTree=null;
			foreach (List<Node> tree in nodeTrees.Values)
			{
				if (tree.Contains(child)) {foundTree=tree; break;}
			}
			
			foreach (Node keyNode in nodeTrees.Keys)
			{
				if (nodeTrees[keyNode]==foundTree) {retNode=keyNode; break;}
			}
			return retNode;
		}
		
		
		//For node copy-pasting
		public void CopyNodes(List<Node> copiedNodes)
		{
			//print ("nodes copied!");
			if (copiedNodes.Count>0)
			{
				if (nodeCopyBuffer.Count>0) {nodeCopyBuffer.Clear();}
				//selectedNodes.CopyTo(nodeCopyBuffer);
				
				foreach (Node copiedNode in copiedNodes)
				{
					//nodeCopyBuffer
					//nodeCopyBuffer.AddRange(selectedNodes);
					nodeCopyBuffer.Add (copiedNode);
				}
			}
		}
		
		public void PasteCopiedNodes()
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
						CreateNewNode (pastedNodePos,copiedNode.text,copiedNode.GetSpriteIndex());
					}
				}
				else if (nodeCopyBuffer.Count==1)
				{
					Node singleCopiedNode=nodeCopyBuffer[0];
					Vector3 pastedNodePos=Camera.main.ScreenToWorldPoint(Input.mousePosition);
					CreateNewNode (pastedNodePos,singleCopiedNode.text,singleCopiedNode.GetSpriteIndex());
				}
			}
		}
		
		// currently unused
		public void ToggleLink(string newLinkSourceId, string newLinkTargetId)
		{
			//bool exists=false;
			Link toggledLink=null;
			foreach (Link link in links.Values)
			{
				if ((link.sourceId==newLinkSourceId && link.targetId==newLinkTargetId) 
				    | (link.sourceId==newLinkTargetId && link.targetId==newLinkSourceId))
				{
					toggledLink=link;
					//print ("Error: Link already Exists!");
					break;
				}
			}
			if (toggledLink==null) {CreateNewLink(newLinkSourceId,newLinkTargetId);}
			else {DeleteLink(toggledLink,null);}
		}
		
		void DeleteNode(Node deletedNode)
		{
			RemoveParentedTree(deletedNode);
			UnchildNode(deletedNode);
			RemoveNodeFromRoot(deletedNode);
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
			unsavedChanges=true;
		}
		
		
		//LINKS///
		public void CreateNewLink(string newLinkSourceId, string newLinkTargetId)
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
				
				//Link linkObject = new Link();
				//find free link id
				int i=0;
				while (links.ContainsKey("link_"+i.ToString())) {i++;}
				
				string generatedLinkId = "link_"+i.ToString();
				CreateNewLink(newLinkSourceId,newLinkTargetId,generatedLinkId,"black");
			}
		}
		
		void CreateNewLink(string newLinkSourceId, string newLinkTargetId, string newLinkId, string newLinkColor)
		{
			Link newLink=CreateNewLinkWithUnmappedConnectNodes(newLinkSourceId,newLinkTargetId,newLinkId,newLinkColor);
			if (newLink!=null)
			{
				//Map links
				newLink.source = nodes[newLinkSourceId] as Node;
				newLink.target = nodes[newLinkTargetId] as Node;
				linkDrawManager.AddDrawnLink(newLink);
			}
			unsavedChanges=true;
		
		}
		
		//Method for adding new links on layout load
		//Pass Link to add in node mappings later
		Link CreateNewLinkWithUnmappedConnectNodes(string newLinkSourceId, string newLinkTargetId, string newLinkId, string newLinkColor)
		{
			Link linkObject=null;
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
				linkObject = new Link();//Instantiate(linkPrefab, new Vector3(0,0,0), Quaternion.identity) as Link;
					
				linkObject.id = newLinkId;
				if (newLinkColor!="") {linkObject.color=newLinkColor;}
				else {linkObject.color="black";}
				linkObject.controller=this;

				linkObject.sourceId = newLinkSourceId;
				linkObject.targetId = newLinkTargetId;
				links.Add(linkObject.id, linkObject);
				//Raise count
				
				linkCount++;
				linkCountText.text = "Ребер: " + linkCount;
			}
			
			return linkObject;
		}
		
		public void DeleteLinkBetweenNodes(Node end1, Node end2)
		{
			Link deletedLink=null;
			foreach (Link link in links.Values)
			{
				if ((link.source==end1 && link.target==end2) 
				    | (link.source==end2 && link.target==end1))
				{
					deletedLink=link;
					//print ("Error: Link already Exists!");
					break;
				}
			}
			if (deletedLink!=null) {DeleteLink(deletedLink,null);}
		}
		
		//Remove one link duplex
		void DeleteLink(Link deletedLink, Link sibling)
		{
			links.Remove(deletedLink.id);
//			linkPoints.Remove(deletedLink);
//			linkColors.Remove(deletedLink);
			linkDrawManager.RemoveDrawnLink(deletedLink);
			//GameObject.Destroy(deletedLink.gameObject);
			linkCount--;
			if (sibling!=null) 
			{
				links.Remove(sibling.id);
				//linkPoints.Remove(sibling);
				//linkColors.Remove(sibling);
				linkDrawManager.RemoveDrawnLink(sibling);
				//GameObject.Destroy(sibling.gameObject);
				linkCount--;
			}
			unsavedChanges=true;
		}
		
		//Remove links
		public void DeleteLinks(List<Link> deletedLinks)
		{
			if (deletedLinks.Count>0)
			{
				foreach(Link deletedLink in deletedLinks)
				{
					//Link sibling=GetLinkSibling(selectedLink);
					Link sibling=null;
					DeleteLink(deletedLink,sibling);
				}
				//selectedLinks.Clear();
			}
		}
		
		public void DeleteNodes(List<Node> deletedNodes)
		{
			if (deletedNodes.Count>0)
			{
				foreach(Node deletedNode in deletedNodes)
				{
					DeleteNode(deletedNode);
				}
				//selectedNodes.Clear();
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
		
		//Method for saving all changes
		public void SaveAll()
		{
			ClearXmlLinks();
			WriteLinksToXml();
			ClearXmlNodes();
			WriteAllNodesToXml();
			SaveCameraPosToXml();
			unsavedChanges=false;
		}
		
		void SaveCameraPosToXml()
		{
			string filepath = sourceFile;
			XmlDocument xmlDoc = new XmlDocument();
			xmlDoc.Load(filepath);
			
			XmlElement elmRoot = (XmlElement)xmlDoc.DocumentElement.FirstChild;
			if (!elmRoot.HasAttribute("camerax")) {elmRoot.SetAttributeNode("camerax","");}
			elmRoot.SetAttribute("camerax",Camera.main.transform.position.x.ToString());
			if (!elmRoot.HasAttribute("cameray")) {elmRoot.SetAttributeNode("cameray","");}
			elmRoot.SetAttribute("cameray",Camera.main.transform.position.y.ToString());
			if (!elmRoot.HasAttribute("cameraZoomlvl")) {elmRoot.SetAttributeNode("cameraZoomlvl","");}
			elmRoot.SetAttribute("cameraZoomlvl",CameraControlZeroG.mainCameraControl.GetZoomLvl().ToString());
			//if (elmRoot)
			xmlDoc.Save(filepath);
		}
		
		void LoadCameraPos()
		{
			string filepath = sourceFile;
			XmlDocument xmlDoc = new XmlDocument();
			xmlDoc.Load(filepath);
			
			XmlElement elmRoot = (XmlElement) xmlDoc.DocumentElement.FirstChild;
			Vector3 cameraPos=Vector3.zero;
			if (elmRoot.HasAttribute("camerax")) {cameraPos.x=float.Parse(elmRoot.GetAttribute("camerax"));}
			else {cameraPos.x=2000;}
			if (elmRoot.HasAttribute("cameray")) {cameraPos.y=float.Parse(elmRoot.GetAttribute("cameray"));}
			else {cameraPos.y=2000;}
			if (elmRoot.HasAttribute ("cameraZoomlvl")) {CameraControlZeroG.mainCameraControl.SetZoomLvl(int.Parse(elmRoot.GetAttribute("cameraZoomlvl")));}
			else {CameraControlZeroG.mainCameraControl.SetZoomLvl(0);}
			
			Camera.main.transform.position=cameraPos;	
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
		
		void WriteAllNodesToXml()
		{
			string filepath = sourceFile;
			XmlDocument xmlDoc = new XmlDocument();
			
			if(File.Exists (filepath))
			{
				xmlDoc.Load(filepath);
				
				XmlNode elmRoot = xmlDoc.DocumentElement.FirstChild;
				
				if (nodes.Count>0)
				{
					foreach (Node node in rootNodes)
					{
						DownwardRecursiveNodeWrite(elmRoot,node,xmlDoc);
					}
				}
				xmlDoc.Save(filepath); // save file.
			}
		}
		
		void DownwardRecursiveNodeWrite(XmlNode rootNode, Node writtenNode, XmlDocument xmlDoc)
		{
			XmlNode newRoot=WriteNodetoXml(rootNode,writtenNode,xmlDoc);
			if (nodeTrees.ContainsKey(writtenNode))
			{
				foreach (Node child in nodeTrees[writtenNode])
				{
					DownwardRecursiveNodeWrite(newRoot,child,xmlDoc);
				}
			}
			
		}
		
		XmlNode WriteNodetoXml(XmlNode rootNode, Node writtenNode, XmlDocument xmlDoc)
		{
			XmlElement savedNode=xmlDoc.CreateElement("node");
			savedNode.SetAttribute("id",writtenNode.id);
			savedNode.SetAttribute("name",writtenNode.text);
			Vector3 savedNodePos=writtenNode.gameObject.transform.position;
			savedNode.SetAttribute("x",savedNodePos.x.ToString());
			savedNode.SetAttribute("y",savedNodePos.y.ToString());
			savedNode.SetAttribute("z",savedNodePos.z.ToString());
			savedNode.SetAttribute("icon",writtenNode.GetSpriteIndex().ToString());
			savedNode.SetAttribute("xmlns","http://graphml.graphdrawing.org/xmlns");
			rootNode.AppendChild(savedNode);
			return (XmlNode)savedNode;
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
		
		void LoadIconTextures()
		{
			//Object[] tempAr=Resources.LoadAll("");
			Texture2D[] rawTextures=new Texture2D[10];
			
			string mainPath=Application.dataPath+"/../";
			
			string texturesPath="file://"+mainPath+"/Skin/WinXP.png";
			WWW www=new WWW(texturesPath);
			//www.texture;
			rawTextures[0]=www.texture;
			//print ("text0");
			//print ("enum is:"+(int)OSTypes.WinXP);
			
			texturesPath="file://"+mainPath+"/Skin/WinVista.png";
			www=new WWW(texturesPath);
			//www.texture;
			rawTextures[1]=www.texture;
			//print ("text1");
			//print ("enum is:"+(int)OSTypes.WinVista);
			
			
			texturesPath="file://"+mainPath+"/Skin/Win7.png";
			www=new WWW(texturesPath);
			//www.texture;
			rawTextures[2]=www.texture;
			
			texturesPath="file://"+mainPath+"/Skin/Win8.png";
			www=new WWW(texturesPath);
			//www.texture;
			rawTextures[3]=www.texture;
			
			texturesPath="file://"+mainPath+"/Skin/WinServer 2000.png";
			www=new WWW(texturesPath);
			//www.texture;
			rawTextures[4]=www.texture;
			
			texturesPath="file://"+mainPath+"/Skin/WinServer 2003.png";
			www=new WWW(texturesPath);
			//www.texture;
			rawTextures[5]=www.texture;
			
			texturesPath="file://"+mainPath+"/Skin/WinServer 2008.png";
			www=new WWW(texturesPath);
			//www.texture;
			rawTextures[6]=www.texture;
			
			texturesPath="file://"+mainPath+"/Skin/WinServer 2012.png";
			www=new WWW(texturesPath);
			//www.texture;
			rawTextures[7]=www.texture;
			
			texturesPath="file://"+mainPath+"/Skin/Linux.png";
			www=new WWW(texturesPath);
			//www.texture;
			rawTextures[8]=www.texture;
			
			texturesPath="file://"+mainPath+"/Skin/Mac OS.png";
			www=new WWW(texturesPath);
			//www.texture;
			rawTextures[9]=www.texture;
			
			nodeIconTextures=new Texture2D[rawTextures.Length];
			int i=0;
			foreach (Texture2D tex in rawTextures) 
			{
				nodeIconTextures[i]=new Texture2D(tex.width,tex.height);
				nodeIconTextures[i].SetPixels(tex.GetPixels());
				nodeIconTextures[i].filterMode=FilterMode.Trilinear;
				nodeIconTextures[i].Apply(true);
				i++;
			}
			
		}
		
		void LoadLayoutFromDB()
		{
			NpgsqlConnection dbConnection=new NpgsqlConnection("Server=37.220.6.166;Port=5432;User Id=msf3;Password=KtrFeVtk9Y7#L;Database=msf3;");
			dbConnection.Open();
			
			NpgsqlCommand Command = new NpgsqlCommand("select cast(address as varchar(255)),os_name from hosts", dbConnection);
			
			NpgsqlDataReader result = Command.ExecuteReader();
			
			//Row index
			int i=0;
			//Col index
			int j=0;
			
			int loadLimiter=300;
			int loadCounter=0;
			
			while (result.Read() && loadCounter<loadLimiter)
				
			{	
				string osName="Null";
				if (!result.IsDBNull(result.GetOrdinal("os_name")))//.GetOrdinal("os_name"))!=null) 
				{osName = result.GetString(result.GetOrdinal("os_name"));}
				string address=result.GetString(result.GetOrdinal("address"));
				
				float startNodeRectanglex=-200;
				float startNodeRectangley=200;
				float nodeXOffset=784;
				float nodeYOffset=528;
				int rowCount=10;
				
				
				//Reset row index
				if (i==rowCount) 
				{
					i=0;
					j++;
				}
				
				Vector3 newNodePos=new Vector3(startNodeRectanglex+nodeXOffset*i,-(startNodeRectangley+nodeYOffset*j),3000);
				
				int osIndex=(int)OSTypes.WinXP;
				if (osName.Contains("XP")) {osIndex=(int)OSTypes.WinXP;}
				if (osName.Contains("7")) {osIndex=(int)OSTypes.Win7;}
				if (osName.Contains("8")) 
				{
					if (osName.Contains("2008")) {osIndex=(int)OSTypes.Server2008;}
					else {osIndex=(int)OSTypes.Win8;}
				}
				if (osName.Contains("2000")) {osIndex=(int)OSTypes.Server2000;}
				if (osName.Contains("2003")) {osIndex=(int)OSTypes.Server2003;}
				if (osName.Contains("2012")) {osIndex=(int)OSTypes.Server2012;}
				if (osName.Contains("Linux")) {osIndex=(int)OSTypes.Linux;}
				if (osName.Contains("IOS")) {osIndex=(int)OSTypes.MacOS;}
				
				CreateNewNode(newNodePos, address,osIndex);	
				i++;
				loadCounter++;
			}
			
			
			dbConnection.Close();
			dbConnection=null;
			
			sceneLoaded=true;
		}
		
		
		void Start () 
		{
			mainController=this;
			LoadIconTextures();
			//initial stats
			linkDrawManager=gameObject.GetComponent<LinkDrawManager>();
			myInputManager=gameObject.GetComponent<InputManager>();
			
			nodeCountText = GameObject.Find("NodeCount").guiText;
			nodeCountText.text = "Вершин: 0";
			linkCountText = GameObject.Find("LinkCount").guiText;
			linkCountText.text = "Ребер: 0";
			statusText = GameObject.Find("StatusText").guiText;
			statusText.text = "";
			
			#if UNITY_EDITOR
			string defaultFilepath=Application.dataPath+"/Data/layout_small.xml";
			#else
			string defaultFilepath=Application.dataPath+"/../layout_small.xml";
			#endif
			
			
			if (File.Exists(defaultFilepath)) 
			{
				sourceFile=defaultFilepath;
				StartLayoutLoad();
			}
			//LoadLayoutFromDB();
			//if () {}
		}
		
		public void ClearScene()
		{
			StopAllCoroutines();
			Node[] cachedNodes=new Node[nodes.Values.Count];
			nodes.Values.CopyTo(cachedNodes,0);
			//List<Node> cachedNodes=(List<Node>)nodes.Values;
			foreach (Node deletedNode in cachedNodes) {DeleteNode(deletedNode);}
			//DeleteNodes(cachedNodes);
			//foreach (Node node in cachedNodes) {DeleteN}//}
			nodes.Clear();
			links.Clear();
			nodeCountText.text = "Вершин: 0";
			linkCountText.text = "Ребер: 0";
			statusText.text = "";

			linkDrawManager.ClearAllLinks();
			//awaitingPath=true;
			sceneLoaded=false;
			
		}
		
		
}
}