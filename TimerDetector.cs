using UnityEngine;
using System.Collections;

public class TimerDetector 
{

	float time;
	bool timeUp=false;
	//
	public TimerDetector(float startTime) {time=startTime;}
	
	public bool UpdateTimer()
	{
		time-=Time.deltaTime;
		if (time<=0) {timeUp=true;}
		return timeUp;
	}
	
	public bool TimeIsUp () {return timeUp;}

}
