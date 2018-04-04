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
using System.Collections.Generic;

using EventControl;

public class ParameterSetEventClip : BaseEventClip, IUndetachableEventClip
{

    [System.Serializable]
    public class SerializableParameter
    {
        public enum Update
        {
            OnEvent,
            StartEvent,
            EndEvent
        }
        public Update update;
        public string name;
        public EventControllerParameter.Type type;
        public float valFloat;
        public int valInt;
        public bool valBool;
    }


    public SerializableParameter[] serializableParameterList;

    
    public class IntParameter
    {
        public IntParameter(string name, int value)
        {
            this.name = name;
            this.value = value;
        }

        public string name;
        public int value;
    }
    
    public class FloatParameter
    {
        public FloatParameter(string name, float value)
        {
            this.name = name;
            this.value = value;
        }
        public string name;
        public float value;
    }
    
    public class BoolParameter
    {
        public BoolParameter(string name, bool value)
        {
            this.name = name;
            this.value = value;
        }
        public string name;
        public bool value;
    }
    
    public class TriggerParameter
    {
        public TriggerParameter(string name)
        {
            this.name = name;
        }
        public string name;
    }
    
    public class ParameterList
    {
        public List<IntParameter> intParameterList = new List<IntParameter>();
        public List<FloatParameter> floatParameterList = new List<FloatParameter>();
        public List<BoolParameter> boolParameterList = new List<BoolParameter>();
        public List<TriggerParameter> triggerParameterList= new List<TriggerParameter>();
    }
    
    //StartEvent時に設定するパラメータリスト
    public ParameterList parameterListStartEvent = new ParameterList();

    //OnEvent時に設定するパラメータリスト
    public ParameterList parameterListOnEvent = new ParameterList();

    //EndEvent時に設定するパラメータリスト
    public ParameterList parameterListEndEvent = new ParameterList();


    private void Awake()
    {
        foreach(var parameter in serializableParameterList)
        {
            if(parameter.update == SerializableParameter.Update.StartEvent)
            {
                if(parameter.type == EventControllerParameter.Type.Bool)
                {
                    parameterListStartEvent.boolParameterList.Add(new BoolParameter(parameter.name, parameter.valBool));
                }
                else if(parameter.type == EventControllerParameter.Type.Float)
                {
                    parameterListStartEvent.floatParameterList.Add(new FloatParameter(parameter.name, parameter.valFloat));
                }
                else if(parameter.type == EventControllerParameter.Type.Int)
                {
                    parameterListStartEvent.intParameterList.Add(new IntParameter(parameter.name, parameter.valInt));
                }
                else if(parameter.type == EventControllerParameter.Type.Trigger)
                {
                    parameterListStartEvent.triggerParameterList.Add(new TriggerParameter(parameter.name));
                }
            }
            else if(parameter.update == SerializableParameter.Update.OnEvent)
            {
                if (parameter.type == EventControllerParameter.Type.Bool)
                {
                    parameterListOnEvent.boolParameterList.Add(new BoolParameter(parameter.name, parameter.valBool));
                }
                else if (parameter.type == EventControllerParameter.Type.Float)
                {
                    parameterListOnEvent.floatParameterList.Add(new FloatParameter(parameter.name, parameter.valFloat));
                }
                else if (parameter.type == EventControllerParameter.Type.Int)
                {
                    parameterListOnEvent.intParameterList.Add(new IntParameter(parameter.name, parameter.valInt));
                }
                else if (parameter.type == EventControllerParameter.Type.Trigger)
                {
                    parameterListOnEvent.triggerParameterList.Add(new TriggerParameter(parameter.name));
                }

            }
            else if(parameter.update == SerializableParameter.Update.EndEvent)
            {
                if (parameter.type == EventControllerParameter.Type.Bool)
                {
                    parameterListEndEvent.boolParameterList.Add(new BoolParameter(parameter.name, parameter.valBool));
                }
                else if (parameter.type == EventControllerParameter.Type.Float)
                {
                    parameterListEndEvent.floatParameterList.Add(new FloatParameter(parameter.name, parameter.valFloat));
                }
                else if (parameter.type == EventControllerParameter.Type.Int)
                {
                    parameterListEndEvent.intParameterList.Add(new IntParameter(parameter.name, parameter.valInt));
                }
                else if (parameter.type == EventControllerParameter.Type.Trigger)
                {
                    parameterListEndEvent.triggerParameterList.Add(new TriggerParameter(parameter.name));
                }
            }
        }
    }

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
            eventController.SetInteger(parameter.name, parameter.value);
        }

        foreach (var parameter in parameterList.floatParameterList)
        {
            eventController.SetFloat(parameter.name, parameter.value);
        }

        foreach (var parameter in parameterList.boolParameterList)
        {
            eventController.SetBool(parameter.name, parameter.value);
        }

        foreach (var parameter in parameterList.triggerParameterList)
        {
            eventController.SetTrigger(parameter.name);
        }
    }
}
