using UnityEngine;
using System.Collections;


using EventControl;

public abstract class BaseEventClip : MonoBehaviour
{
    public EventController eventController;
    //public Layer layer;
    

    abstract public void StartEvent();
    abstract public void OnEvent();
    abstract public void EndEvent();
}
