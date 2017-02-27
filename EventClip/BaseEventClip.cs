using UnityEngine;
using System.Collections;


public abstract class BaseEventClip : MonoBehaviour
{

    abstract public void StartEvent();
    abstract public void OnEvent();
    abstract public void EndEvent();
}
