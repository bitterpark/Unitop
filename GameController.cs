
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using Vectrosity;



namespace Topology {

	public class GameController : MonoBehaviour {

		public Node nodePrefab;
		
		Hashtable nodes=new Hashtable();
		Hashtable links=new Hashtable();
		
		
		public LinkDrawManager linkDrawManager;
		public InputManager myInputManager;
		
		GUIText nodeCountText;
		GUIText linkCountText;
		GUIText statusText;
		
		int nodeCount = 0;
		int linkCount = 0;

		public Texture[] nodeIconTextures;
		
		string sourceFile;
		bool sceneLoaded=false;
		
		
		public void SetSourceFile(string filePath) {sourceFile=filePath;}
		//public bool GetSourceFile() {return sourceFile;}	
		
		public Hashtable GetLinks() {return links;}
		public Hashtable GetNodes() {return nodes;}
		public List<Node> GetRootNodes() {return rootNodes;}
		public Dictionary<Node,List<Node>> GetNodeTrees() {return nodeTrees;}
		List<Node> rootNodes=new List<Node>();
		Dictionary<Node,List<Node>> nodeTrees=new Dictionary<Node, List<Node>>();
		
		public Texture[] GetNodeTextures () {return nodeIconTextures;}
		List<Node> nodeCopyBuffer=new List<Node>();
		
		public void StartLayoutLoad() {StartCoroutine(LoadLayout());}
		
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

			//map node edges
			MapLinkNodes();
			statusText.text = "";
			LoadCameraPos();
//			myInputManager.StartDrawnNodeList();
			sceneLoaded=true;
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
		
		void CreateNewNode(Vector2 newNodePosition,string newNodeText, string newNodeId)
		{
			CreateNewNode (newNodePosition,newNodeText,newNodeId,0);
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
			rootNodes.Add(nodeObject);
			nodeCount++;
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
				}
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
				child.parentNode=null;
				rootNodes.Add(child);
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
						CreateNewNode (pastedNodePos,copiedNode.nodeText.text);
					}
				}
				else if (nodeCopyBuffer.Count==1)
				{
					Vector3 pastedNodePos=Camera.main.ScreenToWorldPoint(Input.mousePosition);
					CreateNewNode (pastedNodePos);
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
			WriteNodesToXml();
			SaveCameraPosToXml();
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
			string mainPath=Application.dataPath+"/../";
			
			string texturesPath="file://"+mainPath+"/Skin/WinXP.png";
			WWW www=new WWW(texturesPath);
			//www.texture;
			nodeIconTextures=new Texture[5];
			
			nodeIconTextures[0]=www.texture;
			
			texturesPath="file://"+mainPath+"/Skin/Win7.png";
			www=new WWW(texturesPath);
			//www.texture;
			nodeIconTextures[1]=www.texture;
			
			texturesPath="file://"+mainPath+"/Skin/Win8.png";
			www=new WWW(texturesPath);
			//www.texture;
			nodeIconTextures[2]=www.texture;
			
			texturesPath="file://"+mainPath+"/Skin/WinServer2008.png";
			www=new WWW(texturesPath);
			//www.texture;
			nodeIconTextures[3]=www.texture;
			
			texturesPath="file://"+mainPath+"/Skin/WinServer2012.png";
			www=new WWW(texturesPath);
			//www.texture;
			nodeIconTextures[4]=www.texture;
			
			foreach (Texture tex in nodeIconTextures) 
			{
				tex.filterMode=FilterMode.Trilinear;
			}
		}
		
		void Start () 
		{
			
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