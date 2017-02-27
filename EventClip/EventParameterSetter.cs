/*
*   最終更新日:
*       7.3.2016
*
*   説明:
*       EventControllerのパラメータを設定するEventClipです
*   
*   必須:
*       Component:
*           EventController:
*
*   更新履歴:
*       7.3.2016:
*           プログラムの完成
*
*/using UnityEngine;
using System.Collections;

public class EventParameterSetter : BaseEventClip
{
    //int
    [System.Serializable]
    public class IntParameter
    {
        public string name;
        public int value;
    }

    //float
    [System.Serializable]
    public class FloatParameter
    {
        public string name;
        public float value;
    }

    //bool
    [System.Serializable]
    public class BoolParameter
    {
        public string name;
        public bool value;
    }

    //trigger
    [System.Serializable]
    public class TriggerParameter
    {
        public string name;
    }

    [System.Serializable]
    public class ParameterList
    {
        public IntParameter[] intParameterList;
        public FloatParameter[] floatParameterList;
        public BoolParameter[] boolParameterList;
        public TriggerParameter[] triggerParameterList;
    }
    public EventController eventCtrl;

    [Space(10)]
    //StartEvent時に設定するパラメータリスト
    public ParameterList parameterListStartEvent;

    //OnEvent時に設定するパラメータリスト
    public ParameterList parameterListOnEvent;

    //EndEvent時に設定するパラメータリスト
    public ParameterList parameterListEndEvent;

    public override void StartEvent()
    {
        SetParameters(parameterListStartEvent);
    }

    public override void OnEvent()
    {
        SetParameters(parameterListOnEvent);
    }

    public override void EndEvent()
    {
        SetParameters(parameterListEndEvent);
    }

    //パラメータを設定する
    void SetParameters(ParameterList parameterList)
    {
        foreach (var parameter in parameterList.intParameterList)
        {
            eventCtrl.SetInteger(parameter.name, parameter.value);
        }

        foreach (var parameter in parameterList.floatParameterList)
        {
            eventCtrl.SetFloat(parameter.name, parameter.value);
        }

        foreach (var parameter in parameterList.boolParameterList)
        {
            eventCtrl.SetBool(parameter.name, parameter.value);
        }

        foreach (var parameter in parameterList.triggerParameterList)
        {
            eventCtrl.SetTrigger(parameter.name);
        }
    }
}
