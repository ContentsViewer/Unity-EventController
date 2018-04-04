using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomTriggerEventClip : BaseEventClip, IUndetachableEventClip {

    public int rate = 20;

    public string nextTriggerName = "Next";
    public string backTriggerName = "Back";

    bool next;

    public override void EndEvent()
    {

    }

    public override void OnEvent()
    {
        if (next)
        {

            eventController.SetTrigger(nextTriggerName);
        }
        else
        {

            eventController.SetTrigger(backTriggerName);
        }
    }

    public override void StartEvent()
    {
        if(Random.Range(0, 100) < rate)
        {
            //Debug.Log("12");
            next = true;
        }
        else
        {
            next = false;
        }
    }
}
