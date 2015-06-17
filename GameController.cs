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
using System.Xml;
using System.IO;

namespace Topology {

	public class GameController : MonoBehaviour {

		public Node nodePrefab;
		public Link linkPrefab;

		private Hashtable nodes;
		private Hashtable links;
		private GUIText statusText;
		private int nodeCount = 0;
		private int linkCount = 0;
		private GUIText nodeCountText;
		private GUIText linkCountText;
		
		private Hashtable createdLinks;
		string sourceFile;
		bool awaitingPath=true;
		
		FileBrowser fb;
		public GUISkin fbSkin;
		public Texture2D file,folder,back,drive;

		//Method for loading the GraphML layout file
		private IEnumerator LoadLayout(){

			//while (awaitingPath) {yield return WaitForFixedUpdate();}
			//sourceFile = EditorUtility.OpenFilePanel("Select source file","","xml");
			//string sourceFile = Application.dataPath + "/Data/layout.xml";
			statusText.text = "Loading file: " + sourceFile;

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

			statusText.text = "Loading Topology";

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

						statusText.text = "Loading Topology: Node " + nodeObject.id;
						nodeCount++;
						nodeCountText.text = "Nodes: " + nodeCount;
					}

					//create links
					if(xmlNode.Name == "edge"){
						Link linkObject = Instantiate(linkPrefab, new Vector3(0,0,0), Quaternion.identity) as Link;
						linkObject.id = xmlNode.Attributes["id"].Value;
						linkObject.sourceId = xmlNode.Attributes["source"].Value;
						linkObject.targetId = xmlNode.Attributes["target"].Value;
						linkObject.status = xmlNode.Attributes["status"].Value;
						links.Add(linkObject.id, linkObject);

						statusText.text = "Loading Topology: Edge " + linkObject.id;
						linkCount++;
						linkCountText.text = "Edges: " + linkCount;
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
				linkObject.id = ("link_"+(linkCount).ToString());//+strNum);//xmlNode.Attributes["id"].Value;
				
				linkObject.sourceId = newLinkSourceId; //xmlNode.Attributes["source"].Value;
				linkObject.targetId = newLinkTargetId;//xmlNode.Attributes["target"].Value;
				linkObject.status = "Up";//xmlNode.Attributes["status"].Value;
				createdLinks.Add (linkObject.id, linkObject);
				links.Add(linkObject.id, linkObject);
				//Map links
				linkObject.source = nodes[newLinkSourceId] as Node;
				linkObject.target = nodes[newLinkTargetId] as Node;
				//Raise count
				linkCount++;
				linkCountText.text = "Edges: " + linkCount;
				//print ("Link created!");
			}
		
		}
		
		public void WriteCreatedLinks()
		{
			
			string filepath = sourceFile;
			XmlDocument xmlDoc = new XmlDocument();
			
			if(File.Exists (filepath))
			{
				xmlDoc.Load(filepath);
				
				XmlNode elmRoot = xmlDoc.DocumentElement.FirstChild;
				
				if (createdLinks.Count>0)
				{
					foreach (Link link in createdLinks.Values)
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
						elmRoot.AppendChild(savedLink);
					}
				}
				createdLinks.Clear();
				
				xmlDoc.Save(filepath); // save file.
				print ("saved!");
			}
		}
		
		void Start () {
			nodes = new Hashtable();
			links = new Hashtable();
			createdLinks=new Hashtable();
			//initial stats
			nodeCountText = GameObject.Find("NodeCount").guiText;
			nodeCountText.text = "Nodes: 0";
			linkCountText = GameObject.Find("LinkCount").guiText;
			linkCountText.text = "Edges: 0";
			statusText = GameObject.Find("StatusText").guiText;
			statusText.text = "";
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
			else {if (GUI.Button(new Rect(5,60,80,20),"Save")) {WriteCreatedLinks();}}
		}
		
		protected void OnGUIMain() {
			
			GUILayout.BeginHorizontal();
			//GUILayout.Label("Xml File", GUILayout.Width(100));
			GUILayout.FlexibleSpace();
			//GUILayout.Label(sourceFile ?? "none selected");
			if (GUI.Button(new Rect(5,60,80,20),"Open..."))
			{//GUILayout.ExpandWidth(false))) {
				fb = new FileBrowser(
					new Rect(100, 100, 600, 500),
					"Choose Xml File",
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
