using UnityEngine;
using System.Collections;
using Vectrosity;
using Topology;
using System.Collections.Generic;

public class LinkDrawManager : MonoBehaviour
{
	VectorLine linksVector;
	VectorLine swapLinksVector;
	
	Dictionary<Link,Vector3Pair> linkPoints=new Dictionary<Link, Vector3Pair>();
	Dictionary<Link,Color> linkColors=new Dictionary<Link, Color>();
	Dictionary<Link,Vector3Pair> dragConnectLinkPoints= new Dictionary<Link, Vector3Pair >();
	Dictionary<Link,Color> dragConnectLinkColors= new Dictionary<Link, Color>();
	
	public Material linksMat;
	public float vectorLineThickness=4f;
	
	public void ClearAllLinks()
	{
		VectorLine.Destroy(ref linksVector);
		linkPoints.Clear();
		linkColors.Clear();
	}
	
	public Dictionary<Link,Vector3Pair> GetLinkPoints()
	{return linkPoints;}
	
	public Link PointDictionaryFindKey(Vector3Pair value)
	{
		Link retLink=null;
		foreach(Link key in linkPoints.Keys)
		{
			if (linkPoints[key].p1 == value.p1 && linkPoints[key].p2==value.p2) 
			{retLink=key; break;}
		}
		return retLink;	
	}
	
	//Add multiple
	public void AddDrawnLinks(Link[] links) 
	{
		foreach (Link link in links) {AddLink(link);}
		UpdateLinkVector();
	}
	//Add one
	public void AddDrawnLink(Link link) 
	{
		AddLink(link);
		UpdateLinkVector();
	}
	//Private add func
	void AddLink(Link link)
	{
		linkPoints.Add(link,new Vector3Pair(link.source.transform.position,link.target.transform.position));
		linkColors.Add (link,link.GetColorFromString(link.color));
	}
	
	//Remove multiple
	public void RemoveDrawnLinks(Link[] links) 
	{
		foreach (Link link in links) {RemoveLink(link);}
		UpdateLinkVector();
	}
	//Remove one
	public void RemoveDrawnLink(Link link) 
	{
		RemoveLink(link);
		UpdateLinkVector();
	}
	//Private remove func
	void RemoveLink(Link deletedLink)
	{
		linkPoints.Remove(deletedLink);
		linkColors.Remove(deletedLink);
	}
	
	//Swap link drawing into the swap linevector, so it can be updated without redrawing the whole map
	
	//Swap multiple
	public void SwapDrawnLinks(Link[] links)
	{
		foreach (Link link in links) {SwapLink(link);}
		UpdateLinkVector();
		UpdateSwapLinkVector();
	}
	
	//Swap one
	public void SwapDrawnLink(Link link)
	{
		SwapLink(link);
		UpdateLinkVector();
		UpdateSwapLinkVector();
	}
	
	//Private swap func
	void SwapLink(Link link)
	{
		if (!dragConnectLinkPoints.ContainsKey(link))
		{
			dragConnectLinkPoints.Add(link,linkPoints[link]);
			dragConnectLinkColors.Add (link,linkColors[link]);
			linkPoints.Remove(link);
			linkColors.Remove(link);
		}
	}
	
	//Swap links back into main vector
	
	public void UnswapDrawnLinks(Link[] links)
	{
		foreach (Link link in links) {UnswapLink(link);}
		UpdateLinkVector();
		UpdateSwapLinkVector();
	}
	
	public void UnswapDrawnLink(Link link)
	{
		UnswapLink(link);
		UpdateLinkVector();
		UpdateSwapLinkVector();
	}
	
	void UnswapLink(Link link)
	{
		if (dragConnectLinkPoints.ContainsKey(link))
		{
			linkPoints.Add(link,dragConnectLinkPoints[link]);
			linkColors.Add (link,dragConnectLinkColors[link]);
			dragConnectLinkPoints.Remove(link);
			dragConnectLinkColors.Remove(link);
		}
	}

	
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
	
	//Swap Link Vector gets udpated externally
	public void UpdateSwappedLinkPositions(List<Link> swappedLinks)
	{
		foreach(Link link in swappedLinks) 
		{
			dragConnectLinkPoints[link]=new Vector3Pair(link.source.transform.position,link.target.transform.position);
		}
		UpdateSwapLinkVector();
		//foreach(Vect
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
	
	void UpdateLinkColors()
	{
		Color[] arColors=new Color[linkColors.Values.Count];
		linkColors.Values.CopyTo(arColors,0);
		linksVector.SetColors(arColors);
	}
	
	public void LinkChangeColor(Link changedLink, Color c)
	{
		linkColors[changedLink]=c;
		UpdateLinkColors();
	}
	
	public void LinksChangeColors(Link[] changedLinks, Color c)
	{
		foreach(Link changedLink in changedLinks) {linkColors[changedLink]=c;}
		UpdateLinkColors();
	}
	
	public void LinksPosRefresh(Link[] updatedLinks)
	{
		foreach (Link updatedLink in updatedLinks)
		{
			linkPoints[updatedLink]=new Vector3Pair(updatedLink.source.transform.position,updatedLink.target.transform.position);
		}
		UpdateLinkVector();
	}
}
