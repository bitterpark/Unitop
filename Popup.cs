using UnityEngine;
using Topology;
public class Popup{
	
	// Represents the selected index of the popup list, the default selected index is 0, or the first item
	int selectedItemIndex = 0;
	
	// Represents whether the popup selections are visible (active)
	bool isVisible = false;
	
	// Represents whether the popup button is clicked once to expand the popup selections
	bool isClicked = false;
	
	//To make sure deselect doesn't occur when list is selected
	Rect currentDimensions;
	
	Vector2 scrollPos=Vector2.zero;
	
	bool selectionMade=false;
	
	// If multiple Popup objects exist, this static variable represents the active instance, or a Popup object whose selection is currently expanded
	static Popup current;
	
	
	// This function is ran inside of OnGUI()
	// For usage, see http://wiki.unity3d.com/index.php/PopupList#Javascript_-_PopupListUsageExample.js
	public int List(Rect box, GUIContent[] items, GUIStyle boxStyle, GUIStyle listStyle) {
		
		selectionMade=false;
		// If the instance's popup selection is visible
		if(isVisible) 
		{
			Rect listRect = new Rect( 0, 0, box.width,box.height*items.Length);//width,height);// box.height * items.Length);
			Rect scrollViewRect=new Rect(box.x,box.y,box.width+20f,(box.height*10));
			scrollPos=GUI.BeginScrollView(scrollViewRect,scrollPos,listRect);
			//Set dimensions to fit the selectbox
			currentDimensions=scrollViewRect;
			//Rect boxRect=new Rect(0,0,box.width,(box.height*Mathf.Min(10,items.Length)));
			//GUI.Box( boxRect, "", boxStyle );
			//GUI.Beg
			/*
			scrollPos=GUILayout.BeginScrollView(scrollPos,false,true
			                                    ,GUI.skin.horizontalScrollbar,GUI.skin.verticalScrollbar,GUI.skin.box
			                                    );*/
			// Draw a SelectionGrid and listen for user selection
			
			int selectIndex= GUI.SelectionGrid( listRect, -1, items, 1, listStyle );
			
			GUI.EndScrollView();
			/*
			selectedItemIndex=GUILayout.SelectionGrid(selectedItemIndex,items,1,listStyle
			, GUILayout.Width(box.width),GUILayout.Height(box.height* items.Length), GUILayout.MaxWidth(box.width));
			GUILayout.EndScrollView();
			GUILayout.EndArea();*/
			// If the user makes a selection, make the popup list disappear and the button reappear
			if (selectIndex!=-1)
			{
				//if(GUI.changed) {
					selectedItemIndex=selectIndex;
					current = null;
					isClicked=false;
					currentDimensions=box;
					//Must be set to true for one frame
					selectionMade=true;
				//}
			}
			//InputManager.DebugPrint("selection is:"+selectionMade.ToString());
		}
		
		// Get the control ID
		int controlID = GUIUtility.GetControlID( FocusType.Passive );
		
		// Listen for controls
		switch( Event.current.GetTypeForControl(controlID) )
		{
		// If mouse button is clicked, set all Popup selections to be retracted
		case EventType.mouseUp:
		{
			current = null;
			break;
		}	
		}	
		
		// Draw a button. If the button is clicked
		if (!isClicked)
		{
			if(GUI.Button(new Rect(box.x,box.y,box.width,box.height),items[selectedItemIndex])) {
				
				// If the button was not clicked before, set the current instance to be the active instance
				if(!isClicked) {
					current = this;
					isClicked = true;
					scrollPos=Vector2.zero;
				}
				// If the button was clicked before (it was the active instance), reset the isClicked boolean
				else {
					isClicked = false;
				}
			}
		}
		
		// If the instance is the active instance, set its popup selections to be visible
		if(current == this) {
			isVisible = true;
			//scrollPos=Vector2.zero;
		}
		
		// These resets are here to do some cleanup work for OnGUI() updates
		else {
			isVisible = false;
			isClicked = false;
		}
		
		// Return the selected item's index
		return selectedItemIndex;
	}
	
	// Get the instance variable outside of OnGUI()
	public int GetSelectedItemIndex()
	{
		return selectedItemIndex;
	}
	
	//Must return seleciton once with the current Context Menu setup
	public bool SelectionWasMade() {return selectionMade;}
	
	public Rect GetCurrentDimensions()
	{
		return currentDimensions;
	}
	
	public void SetSelectedItemIndex(int newIndex)
	{
		selectedItemIndex=newIndex;
	}
}