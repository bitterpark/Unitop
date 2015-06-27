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
using Vectrosity;

namespace Topology {

	public class GameController : MonoBehaviour {

		public Node nodePrefab;
		public Link linkPrefab;

		Hashtable nodes;
		Hashtable links;
		VectorLine linksVector;
		//List<Vector3> linksPoints=new List<Vector3>();
		Dictionary<Link,Vector3Pair> linksPoints=new Dictionary<Link, Vector3Pair>();
		Dictionary<Link,Color> linkColors=new Dictionary<Link, Color>();
		public Material linksMat;
		
		GUIText statusText;
		int nodeCount = 0;
		int linkCount = 0;
		GUIText nodeCountText;
		GUIText linkCountText;
		//GUIContainer contextMenu;
		public Rect contextMenuPosNodes;
		public Rect contextMenuPosLinks;
		
		List<Link> selectedLinks;
		List<Node> selectedNodes;
		//Object lastSelectedLinkNode=null;	
		Link lastSelectedLink=null;
		Node lastSelectedNode=null;
		
		string sourceFile;
		bool awaitingPath=true;
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
						float z = float.Parse(xmlNode.Attributes["z"].Value);

						Node nodeObject = Instantiate(nodePrefab, new Vector3(x,y,z), Quaternion.identity) as Node;
						nodeObject.nodeText.text = xmlNode.Attributes["name"].Value;

						nodeObject.id = xmlNode.Attributes["id"].Value;
						nodes.Add(nodeObject.id, nodeObject);
						nodeObject.controller=this;
						nodeObject.myPos=new Vector3(x,y,z);
						
						statusText.text = "Загрузка топологии: Вершина " + nodeObject.id;
						nodeCount++;
						nodeCountText.text = "Вершин: " + nodeCount;
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
			//Turn links dictionary into a draw array
			/*
			Vector3[] linksDrawArray=new Vector3[linksPoints.Count*2];
			int k=0;
			foreach(Vector3Pair points in linksPoints.Values)
			{
				linksDrawArray[k]=points.p1;
				k++;
				linksDrawArray[k]=points.p2;
				k++;
			}
			//Turns link colors dictionary into a draw array
			Color[] linksColorsDrawArray=new Color[linkColors.Count];
			linkColors.Values.CopyTo(linksColorsDrawArray,0);
			
			linksVector=new VectorLine("All Link Line",linksDrawArray,linksColorsDrawArray,linksMat,3f,LineType.Discrete);
			//linksVector.SetColor(Color.black);
			linksVector.sortingOrder=-5;
			linksVector.Draw3D();//Draw3DAuto();
			*/
			statusText.text = "";
			//Vector3Pair pear=new Vector3Pair(
		}

		//Method for mapping links to nodes
		private void MapLinkNodes(){
			foreach(string key in links.Keys)
			{
				Link link = links[key] as Link;
				link.source = nodes[link.sourceId] as Node;
				link.target = nodes[link.targetId] as Node;
				linksPoints.Add(link,new Vector3Pair(link.source.transform.position,link.target.transform.position));
				linkColors.Add (link,link.GetColorFromString(link.color));
				//print ("color assigned:"+link.color);
				//linksPoints.Add (link.source.myPos);
				//linksPoints.
				//linksPoints.Add (link.target.myPos);
				//link.sourcePointIndex=linksPoints.FindIndex();
			}
		}
		
		Link AttemptSelectLink() 
		{
			Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
			
			//-1 means no point selected
			//int iSelect = -1;
			Link linkSelect=null;
			float closestYet = Mathf.Infinity;
			
			float selectionThreshold=0.22f;
			
			//for (int i = 0; i < linksPoints.Count-1; i++) {
			foreach (Vector3Pair points in linksPoints.Values)
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
			foreach(Link key in linksPoints.Keys)
			{
				if (linksPoints[key].p1 == value.p1 && linksPoints[key].p2==value.p2) 
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
			Vector3[] linksDrawArray=new Vector3[linksPoints.Count*2];
			int k=0;
			foreach(Vector3Pair points in linksPoints.Values)
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
		
		//Call this when points are update to apply color properly
		void UpdateLinkVector()
		{
			VectorLine.Destroy(ref linksVector);
			Vector3[] linksDrawArray=new Vector3[linksPoints.Count*2];
			int k=0;
			foreach(Vector3Pair points in linksPoints.Values)
			{
				linksDrawArray[k]=points.p1;
				k++;
				linksDrawArray[k]=points.p2;
				k++;
			}
			Color[] arColors=new Color[linkColors.Values.Count];
			linkColors.Values.CopyTo(arColors,0);
			
			linksVector=new VectorLine("All Link Line",linksDrawArray,arColors,linksMat,3f,LineType.Discrete);
			//linksVector.SetColor(Color.black);
			linksVector.sortingOrder=-5;
			linksVector.Draw3D();
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
				linksPoints.Add(linkObject,new Vector3Pair(linkObject.source.transform.position,linkObject.target.transform.position));
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
			linksPoints.Remove(deletedLink);
			linkColors.Remove(deletedLink);
			//GameObject.Destroy(deletedLink.gameObject);
			linkCount--;
			if (sibling!=null) 
			{
				links.Remove(sibling.id);
				linksPoints.Remove(sibling);
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
			ClickedNodeAction(clickedNode);
			print ("Color black is:"+Color.black);
			print ("Color green is:"+Color.green);
		}
		
		//Called by node when its dragged
		public void DragNode(Node draggedNode)
		{
			//call all connected links to realign
			foreach (Link link in links.Values)
			{
				if (link.source==draggedNode) 
				{//link.loaded=false;
					linksPoints[link]=new Vector3Pair(draggedNode.transform.position,linksPoints[link].p2);
				}
				if (link.target==draggedNode){linksPoints[link]=new Vector3Pair(linksPoints[link].p1,draggedNode.transform.position);}
			}
			UpdateLinkVector();
		
		}
		
		void HandleSelectMode()
		{
			selectMode=0;
			if (Input.GetKey("left shift")) {selectMode=1;}
			if (Input.GetKey("left ctrl")) {selectMode=2;}
			if (Input.GetKey("left alt"))  {selectMode=3;}
		
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
				DeselectAllNodes();
				selectedNodes.Add(clickedNode);//HashtableAppendNum(selectedNodes,clickedNode);//selectedNodes.Add(clickedNode);
				clickedNode.selected=true;
			}
			
			//shift select mode
			if (selectMode==1)
			{ 
				//selectedNodes.Add(clickedNode);
				selectedNodes.Add(clickedNode);//HashtableAppendNum(selectedNodes,clickedNode);
				clickedNode.selected=true;
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
			/*
			float ttStartx=10;
			float ttStarty=100;
			float ttSizex=100;
			float ttSizey=30;
			float ttStartHPad=10;
			float ttStartVPad=10;
			float ttHPad=20;
			float ttVPad=5;
			string ttText="";
			*/
			float elementSizeX=110;
			float elementSizeY=40;
			float leftColumnStartX=30;
			float leftColumnStartY=115;
			float rightColumnStartX=170;
			float rightColumnStartY=115;
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
				currentNode.SetSprite(selectIconDroplist.GetSelectedItemIndex());
				
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
			nodes = new Hashtable();
			links = new Hashtable();
			//linksVector = new VectorLine("poop",new Vector3[],
			
			selectedLinks=new List<Link>();
			selectedNodes=new List<Node>();
			
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
		
		//Deselect current selection on clicking empty space
		void ManageClickDeselect()
		{
			if (Input.GetMouseButtonDown(0)) 
			{
				Vector2 mousePosInGUICoords = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
				if (selectMode==0 && !selectionMade && !contextMenuPosNodes.Contains(mousePosInGUICoords) 
				 && !contextMenuPosLinks.Contains(mousePosInGUICoords)
				  && !selectItemDroplist.GetCurrentDimensions().Contains(mousePosInGUICoords) 
				   && !selectColorDroplist.GetCurrentDimensions().Contains(mousePosInGUICoords)
				   	&& !selectIconDroplist.GetCurrentDimensions().Contains(mousePosInGUICoords))
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
			if (Input.GetMouseButtonDown(0))
			{
				//int selectedPointIndex=AttemptSelectLink();
				//if (selectedPointIndex!=-1) {ClickLink(GetLinkFromPointIndex(selectedPointIndex));}
				Link selectedLink=AttemptSelectLink();
				if (selectedLink!=null) {ClickLink(selectedLink);}
			}	
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
