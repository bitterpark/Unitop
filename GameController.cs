/*
 * Copyright 2014 Jason Graves (GodLikeMouse/Collaboradev)
 * http://www.collaboradev.com
 *
 * This file is part of Unity - Topology.
 *
 * Unity - Topology is free software: you can redistribute it 
 * and/or modify it under the terms of the GNU General Public 
 * License as published by the Free Software Foundation, either 
 * version 3 of the License, or (at your option) any later version.
 *
 * Unity - Topology is distributed in the hope that it will be useful, 
 * but WITHOUT ANY WARRANTY; without even the implied warranty of 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU 
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License 
 * along with Unity - Topology. If not, see http://www.gnu.org/licenses/.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;

namespace Topology {

	public class GameController : MonoBehaviour {

		public Node nodePrefab;
		public Link linkPrefab;

		Hashtable nodes;
		Hashtable links;
		GUIText statusText;
		int nodeCount = 0;
		int linkCount = 0;
		GUIText nodeCountText;
		GUIText linkCountText;
		//GUIContainer contextMenu;
		public Rect contextMenuPos;
		
		//Hashtable createdLinks;
		List<Link> selectedLinks;
		List <Node> selectedNodes;
		
		string sourceFile;
		bool awaitingPath=true;
		//0 - node mode. 1 - link mode
		int tooltipMode
		{
			get {return _tooltipMode;}
			set 
			{
				if (value!=_tooltipMode) {listSelectIndex=0;}
				_tooltipMode=value;
			}
		
		}
		int _tooltipMode=0;
		//ctrl, shift or alt select
		int selectMode;
		//something was selected this frame, so no deselect
		bool selectionMade=false;
		
		//Starting file browser object
		FileBrowser fb;
		//dropselect selection index
		int listSelectIndex=0;
		bool renderList=false;
		public GUISkin fbSkin;
		public Texture2D file,folder,back,drive;
		

		//Method for loading the GraphML layout file
		private IEnumerator LoadLayout()
		{

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

			int scale = 1;//2;

			XmlElement root = xmlDoc.FirstChild as XmlElement;
			for(int i=0; i<root.ChildNodes.Count; i++){
				XmlElement xmlGraph = root.ChildNodes[i] as XmlElement;

				for(int j=0; j<xmlGraph.ChildNodes.Count; j++){
					XmlElement xmlNode = xmlGraph.ChildNodes[j] as XmlElement;
					//create nodes
					if(xmlNode.Name == "node"){
						float x = float.Parse(xmlNode.Attributes["x"].Value)/scale;
						float y = float.Parse (xmlNode.Attributes["y"].Value)/scale;
						float z = float.Parse(xmlNode.Attributes["z"].Value)/scale;

						Node nodeObject = Instantiate(nodePrefab, new Vector3(x,y,z), Quaternion.identity) as Node;
						nodeObject.nodeText.text = xmlNode.Attributes["name"].Value;

						nodeObject.id = xmlNode.Attributes["id"].Value;
						nodes.Add(nodeObject.id, nodeObject);
						nodeObject.controller=this;
						
						statusText.text = "Загрузка топологии: Вершина " + nodeObject.id;
						nodeCount++;
						nodeCountText.text = "Вершин: " + nodeCount;
					}

					//create links
					if(xmlNode.Name == "edge"){
					/*
						Link linkObject = Instantiate(linkPrefab, new Vector3(0,0,0), Quaternion.identity) as Link;
						linkObject.id = xmlNode.Attributes["id"].Value;
						linkObject.sourceId = xmlNode.Attributes["source"].Value;
						linkObject.targetId = xmlNode.Attributes["target"].Value;
						linkObject.status = xmlNode.Attributes["status"].Value;
						string newLinkColor=xmlNode.Attributes["color"].Value;
						if (newLinkColor=="") {newLinkColor="black";}
						linkObject.color=newLinkColor;
						linkObject.controller=this;
						links.Add(linkObject.id, linkObject);
						
						statusText.text = "Загрузка топологии: Ребро " + linkObject.id;
						linkCount++;
						linkCountText.text = "Ребер: " + linkCount;
						*/
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

			statusText.text = "";
		}

		//Method for mapping links to nodes
		private void MapLinkNodes(){
			foreach(string key in links.Keys){
				Link link = links[key] as Link;
				link.source = nodes[link.sourceId] as Node;
				link.target = nodes[link.targetId] as Node;
			}
		}
		
		void CreateNewNode()
		{

			Vector3 newNodePos=Camera.main.transform.position+Camera.main.transform.forward*40;
			Node nodeObject = Instantiate(nodePrefab, newNodePos, Quaternion.identity) as Node;
			//nodeObject.nodeText.text = xmlNode.Attributes["name"].Value;
			
			int i=0;
			while (nodes.ContainsKey("node_"+i.ToString()))
			{
				i++;
			}
			nodeObject.id = "node_"+i.ToString();
			nodeObject.controller=this;
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
				Link linkObject = Instantiate(linkPrefab, new Vector3(0,0,0), Quaternion.identity) as Link;
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
				Link linkObject = Instantiate(linkPrefab, new Vector3(0,0,0), Quaternion.identity) as Link;
				
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
			GameObject.Destroy(deletedLink.gameObject);
			linkCount--;
			if (sibling!=null) 
			{
				links.Remove(sibling.id);
				GameObject.Destroy(sibling.gameObject);
				linkCount--;
			}
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
			//Determine select mode
			//int mode=0;
			//if (Input.GetKey("left shift")) {mode=1;}
			//if (Input.GetKey("left ctrl")) {mode=2;}
			
			string startId=clickedLink.sourceId;
			string endId=clickedLink.targetId;
			//get sibling (if one exists)
			Link sibling=null;
			//Link sibling=GetLinkSibling(clickedLink);
			//If sibling doesn't exist, pass null
			ClickedLinkAction(clickedLink,sibling);
		}
		
		public void ClickNode(Node clickedNode)
		{
			//Determine select mode
			//int mode=0;
			//if (Input.GetKey("left shift")) {mode=1;}
			//if (Input.GetKey("left ctrl")) {mode=2;}
			//if (Input.GetKey("left alt"))  {mode=3;}
			ClickedNodeAction(clickedNode);
		}
		
		void HandleSelectMode()
		{
			selectMode=0;
			if (Input.GetKey("left shift")) {selectMode=1;}
			if (Input.GetKey("left ctrl")) {selectMode=2;}
			if (Input.GetKey("left alt"))  {selectMode=3;}
		
		}
		
		//Do action based on select mode
		void ClickedLinkAction(Link actionLink, Link sibling)
		{
			selectionMade=true;
			DeselectAllNodes();
			//drawTooltip=true;
			tooltipMode=1;
			switch (selectMode)
			{
				case 0:
				{
					if (selectedLinks.Count>0) 
					{
						foreach (Link selectedLink in selectedLinks) {selectedLink.selected=false;}
					}
					selectedLinks.Clear();
					selectedLinks.Add (actionLink);
					actionLink.selected=true;
					//Make sure sibling is kept selected
					if (sibling!=null)
					{
						selectedLinks.Add (sibling);
						sibling.selected=true;
					}
					break;
				}
				case 1:
				{
					selectedLinks.Add (actionLink);
					actionLink.selected=true;
					if (sibling!=null)
					{
						selectedLinks.Add (sibling);
						sibling.selected=true;
					}
					break;
				}
				case 2:
				{
					if (actionLink.selected)
					{
						selectedLinks.Remove(actionLink);
						actionLink.selected=false;
						//assume sibling has the same status as main
						if (sibling!=null)
						{
							selectedLinks.Remove(sibling);
							sibling.selected=false;
						}
					}
					else
					{
						selectedLinks.Add(actionLink);
						actionLink.selected=true;
						//assume sibling has the same status as main
						if (sibling!=null)
						{
							selectedLinks.Add(sibling);
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
		
		public void ClickedNodeAction(Node clickedNode)
		{
			selectionMade=true;
			DeselectAllLinks();
			tooltipMode=0;
			// single select mode
			if (selectMode==0)
			{
				if (selectedNodes.Count>0) 
				{
					foreach (Node selectedNode in selectedNodes)
					{
						selectedNode.selected=false;
					}
					selectedNodes.Clear();
				}
			}
			
			//shift select mode and single select mode
			if (selectMode==0 | selectMode==1)
			{ 
				selectedNodes.Add(clickedNode);
				clickedNode.selected=true;
			}
			
			// ctrl(alt) select mode
			if (selectMode==2) 
			{
				if (selectedNodes.Contains(clickedNode)) 
				{
					selectedNodes.Remove(clickedNode);
					clickedNode.selected=false;
				}
				else
				{
					selectedNodes.Add(clickedNode);
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
			//contextMenu.Display();
			//print ("display request fired");
			
			//nodes tooltip
			if (mode==1)
			{
				//Generate droplist content
				GUIContent[] droplistContent=new GUIContent[selectedNodes.Count];
				for (int i=0; i<selectedNodes.Count; i++)
				{
					droplistContent[i]=new GUIContent(selectedNodes[i].id);
				}
				//float ttAreaSizex=300;
				//float ttAreaSizey=100;
				float ttStartx=0;
				float ttStarty=100;
				float ttSizex=100;
				float ttSizey=20;
				float ttPad=20;
				string ttText="";
				
				//Main box and left hand labels
				GUI.Box(contextMenuPos,"");
				GUI.BeginGroup(contextMenuPos);
				//GUI.BeginGroup(new Rect(ttStartx,ttStarty,ttAreaSizex,ttAreaSizey));
				//GUI.Box(new Rect(0,0,ttAreaSizex,ttAreaSizey),ttText);
				GUI.Label (new Rect(ttPad,ttPad,ttSizex,ttSizey),"Выбор элемента:");
				GUI.Label (new Rect(ttPad,ttPad*2+ttSizey,ttSizex*2,ttSizey),"Имя элемента:");
				
				//name edit field
				string nodeName=selectedNodes[listSelectIndex].nodeText.text;
				nodeName=GUI.TextField(new Rect(ttPad*2+ttSizex,ttPad*2+ttSizey,ttSizex*1.5f,ttSizey),nodeName);
				selectedNodes[listSelectIndex].nodeText.text=nodeName;
				GUI.EndGroup();
				//select obj menu
				if(Popup.List(new Rect(ttStartx+ttPad*2+ttSizex,ttStarty+ttPad,ttSizex,ttSizey)
				 ,ref renderList,ref listSelectIndex,new GUIContent(droplistContent[listSelectIndex]),droplistContent
				  ,fbSkin.customStyles[0])) 
				  {
				  	//print ("Selected!");
				  }
				 // if (renderList) {selectionMade=true;}
			}
			if (mode==2) //links tooltip
			{
				//Generate droplist content
				GUIContent[] droplistContent=new GUIContent[selectedLinks.Count];
				for (int i=0; i<selectedLinks.Count; i++)
				{
					droplistContent[i]=new GUIContent(selectedLinks[i].id);
				}
				//float ttAreaSizex=300;
				//float ttAreaSizey=100;
				float ttStartx=0;
				float ttStarty=100;
				float ttSizex=100;
				float ttSizey=20;
				float ttPad=20;
				string ttText="";
				
				//main box and left hand labels
				GUI.Box(contextMenuPos,"");
				GUI.BeginGroup(contextMenuPos);
				//GUI.BeginGroup(new Rect(ttStartx,ttStarty,ttAreaSizex,ttAreaSizey));
				//GUI.Box(new Rect(0,0,ttAreaSizex,ttAreaSizey),ttText);
				GUI.Label (new Rect(ttPad,ttPad,ttSizex,ttSizey),"Выбор элемента:");
				
				//Color select button
				string pickColor=selectedLinks[listSelectIndex].color;
				if (GUI.Button (new Rect(ttPad,ttPad*2+ttSizey,ttSizex*2,ttSizey),"Цвет элемента:"+pickColor))
				{
					switch (pickColor)
					{
						case "black": {pickColor="red"; break;}
						case "red": {pickColor="green"; break;}
						case "green":{pickColor="black"; break;}
					}
					selectedLinks[listSelectIndex].color=pickColor;
					//selectionMade=true;
				}
				GUI.EndGroup();
				
				//element select button
				if(Popup.List(new Rect(ttStartx+ttPad*2+ttSizex,ttStarty+ttPad,ttSizex,ttSizey)
				              ,ref renderList,ref listSelectIndex,new GUIContent(droplistContent[listSelectIndex]),droplistContent
				              ,fbSkin.customStyles[0])) 
				{
					//print ("Selected!");
				}
				//if (renderList) {selectionMade=true;}
			}
		}
		
		void Start () {
			nodes = new Hashtable();
			links = new Hashtable();
			selectedLinks=new List<Link>();
			selectedNodes=new List<Node>();
			//initial stats
			nodeCountText = GameObject.Find("NodeCount").guiText;
			nodeCountText.text = "Вершин: 0";
			linkCountText = GameObject.Find("LinkCount").guiText;
			linkCountText.text = "Ребер: 0";
			statusText = GameObject.Find("StatusText").guiText;
			statusText.text = "";
			//contextMenu=GameObject.Find("ContextMenu").GetComponent<GUIContainer>();//gameObject.GetComponentInChildren<GUIContainer>();
		}
		
		//Deselect current selection on clicking empty space
		void ManageClickDeselect()
		{
			if (Input.GetMouseButtonDown(0)) 
			{
				Vector2 mousePosInGUICoords = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
				if (selectMode==0 && !selectionMade && !contextMenuPos.Contains(mousePosInGUICoords))
				{	
					DeselectAllNodes();
					DeselectAllLinks();
				}
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
		
		//fires before physics clicks and Update
		void FixedUpdate()
		{
			HandleSelectMode();
		}
		
		//fires after physics clicks
		void Update()
		{
			//manage input
			ManageClickDeselect();
			if (Input.GetKeyDown (KeyCode.Delete))
			{
				if (selectedNodes.Count>0){DeleteSelectedNodes();}
				if (selectedLinks.Count>0){DeleteSelectedLinks();}
			}
			Vector3 newObjPos=Vector3.zero;
			
			if (Input.GetKeyDown("n")) {CreateNewNode();}
			selectionMade=false;
			//print(Input.mousePosition);
		}
		
		protected void OnGUI () 
		{
			GUI.skin=fbSkin;
			if (awaitingPath)
			{
				if (fb != null) 
				{
					fb.OnGUI();
				} 
				else 
				{
					OnGUIMain();
				}
			}
			else {if (GUI.Button(new Rect(5,60,80,20),"Сохранить")) {SaveAll();}}//WriteCreatedLinks();}}
			ManageTooltip();
		}
		
		protected void OnGUIMain() {
			
			GUILayout.BeginHorizontal();
			//GUILayout.Label("Xml File", GUILayout.Width(100));
			GUILayout.FlexibleSpace();
			//GUILayout.Label(sourceFile ?? "none selected");
			if (GUI.Button(new Rect(5,60,80,20),"Открыть..."))
			{//GUILayout.ExpandWidth(false))) {
				fb = new FileBrowser(new Rect(100, 100, 600, 500),"Выберите xml файл",FileSelectedCallback);
				
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
				awaitingPath=false;
				StartCoroutine( LoadLayout() );
			}
			else {fb = null;}
		}

	}

}
