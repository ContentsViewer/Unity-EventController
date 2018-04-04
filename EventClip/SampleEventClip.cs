/*
*   最終更新日:
*       6.1.2016
*
*
*
*
*   更新履歴:
*       6.1.2016:
*           プログラムの完成
*/
using UnityEngine;
using System.Collections;
using System;

public class SampleEventClip : BaseEventClip
{

    public override void StartEvent()
    {
        Debug.Log("StartEvent");
    }

    public override void OnEvent()
    {
        Debug.Log("OnEvent");
    }

    public override void EndEvent()
    {
        Debug.Log("EndEvent");
    }
}
