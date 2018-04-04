using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//using UnityEditor;

public class DelayEventClip : BaseEventClip, IUndetachableEventClip, IMessageableEventClip
{

    public float delayTime = 5.0f;
    public string triggerName = "Next";


    float startTime;

    public override void EndEvent()
    {
    }

    public override void OnEvent()
    {
        if (Time.time > startTime + delayTime)
        {
            eventController.SetTrigger(triggerName);
        }
    }
    public override void StartEvent()
    {
        startTime = Time.time;
    }

    public string Message
    {
        get
        {
            float time = delayTime - (Time.time - startTime);
            if(time < 0.0f)
            {
                time = 0.0f;
            }
            return time.ToString();
        }
    }
}
