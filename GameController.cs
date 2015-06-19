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
//using UnityEditor;
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
		
		//Hashtable createdLinks;
		List<Link> selectedLinks;
		
		string sourceFile;
		bool awaitingPath=true;
		
		FileBrowser fb;
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

			int scale = 2;

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

						statusText.text = "Загрузка топологии: Вершина " + nodeObject.id;
						nodeCount++;
						nodeCountText.text = "Вершин: " + nodeCount;
					}

					//create links
					if(xmlNode.Name == "edge"){
						Link linkObject = Instantiate(linkPrefab, new Vector3(0,0,0), Quaternion.identity) as Link;
						linkObject.id = xmlNode.Attributes["id"].Value;
						linkObject.sourceId = xmlNode.Attributes["source"].Value;
						linkObject.targetId = xmlNode.Attributes["target"].Value;
						linkObject.status = xmlNode.Attributes["status"].Value;
						links.Add(linkObject.id, linkObject);

						statusText.text = "Загрузка топологии: Ребро " + linkObject.id;
						linkCount++;
						linkCountText.text = "Ребер: " + linkCount;
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
		
		//Method for adding new links
		public void CreateNewLink(string newLinkSourceId, string newLinkTargetId)
		{
			//Check if link already exists
			bool exists=false;
			foreach (Link link in links.Values)
			{
				if (link.sourceId==newLinkSourceId && link.targetId==newLinkTargetId)
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
				
				linkObject.sourceId = newLinkSourceId;
				linkObject.targetId = newLinkTargetId;
				linkObject.status = "Up";
				links.Add(linkObject.id, linkObject);
				//Map links
				linkObject.source = nodes[newLinkSourceId] as Node;
				linkObject.target = nodes[newLinkTargetId] as Node;
				//Raise count
				linkCount++;
				linkCountText.text = "Ребер: " + linkCount;
				//print ("Link created!");
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
					Link sibling=GetLinkSibling(selectedLink);
					if (sibling!=null) 
					{
						DeleteLink(selectedLink,sibling);
					}
				}
				selectedLinks.Clear();
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
			int mode=0;
			if (Input.GetKey("left shift")) {mode=1;}
			if (Input.GetKey("left ctrl")) {mode=2;}
			
			string startId=clickedLink.sourceId;
			string endId=clickedLink.targetId;
			//get sibling (if one exists)
			Link sibling=GetLinkSibling(clickedLink);
			//If sibling doesn't exist, pass null
			ClickedLinkAction(clickedLink,sibling,mode);
		}
		
		//Do action based on select mode
		void ClickedLinkAction(Link actionLink, Link sibling,int selectMode)
		{
			
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

		//Method for saving new links
		void SaveLinks()
		{
			ClearXmlLinks();
			WriteLinksToXml();
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
						savedLink.SetAttribute("color","");
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
		
		void Start () {
			nodes = new Hashtable();
			links = new Hashtable();
			selectedLinks=new List<Link>();
			//initial stats
			nodeCountText = GameObject.Find("NodeCount").guiText;
			nodeCountText.text = "Вершин: 0";
			linkCountText = GameObject.Find("LinkCount").guiText;
			linkCountText.text = "Ребер: 0";
			statusText = GameObject.Find("StatusText").guiText;
			statusText.text = "";
		}
		
		void Update()
		{
			//manage keys
			if (Input.GetKeyDown (KeyCode.Delete)){DeleteSelectedLinks();}
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
			else {if (GUI.Button(new Rect(5,60,80,20),"Сохранить")) {SaveLinks();}}//WriteCreatedLinks();}}
		}
		
		protected void OnGUIMain() {
			
			GUILayout.BeginHorizontal();
			//GUILayout.Label("Xml File", GUILayout.Width(100));
			GUILayout.FlexibleSpace();
			//GUILayout.Label(sourceFile ?? "none selected");
			if (GUI.Button(new Rect(5,60,80,20),"Открыть..."))
			{//GUILayout.ExpandWidth(false))) {
				fb = new FileBrowser(
					new Rect(100, 100, 600, 500),
					"Выберите xml файл",
					FileSelectedCallback
					);
				
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
