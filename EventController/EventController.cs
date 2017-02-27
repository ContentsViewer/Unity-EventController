/*
*   最終更新日:
*       8.6.2016
*
*   用語:
*       EventNode:
*           各状態のこと
*           NodeにEventClipを登録することができる
*
*       EventClip:
*           実際にEventを実行するもの
*
*       EventController:
*           各EventNodeへの遷移をコントロールするもの
*
*       Transition:
*           あるEventNodeから別のEventNodeに移ること
*
*       Layer:
*           EventNodeとTransitionを含んだ一つの集まり
*           LayerごとにEventNodeは独立に遷移する
*
*   更新履歴:
*       6.1.2016:
*           プログラムの完成
*
*       6.7.2016:
*           Layerの概念を取り入れた
*
*       6.29.2016:
*           一つのレイヤーでTriggerを消費したときその下のレイヤーでTriggerがかかっていなくなっている問題を修正
*
*       7.6.2016:
*           比較オプション追加
*
*       8.6.2016:
*           SetAsEntry設定されたEventNode.EventClipのStartEventを実行しない問題を修正
*       
*/


using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class EventController : MonoBehaviour
{
    public enum COMPARE_OPTIONS
    {
        GREATER,
        LESS,
        EQUALS,
        NOT_EQUAL
    }

    public enum PARAM_TYPE
    {
        INT,
        FLOAT,
        BOOL,
        TRIGGER //遷移で消費される
    }

    public enum STATE
    {
        NON,
        TRANSITION,
        ON_EVENT
    }
    //===パラメータ==============================================================
    public bool save = false;
    public string label = "(non)";

    //パラメータリスト
    public List<Param> paramList = new List<Param>();

    public List<Layer> layerList = new List<Layer>();

    //===コード==========================================================================================
    void Awake()
    {
        SetController();

        foreach (var layer in layerList)
        {
            var currentEventNode = layer.currentEventNode;
            if (currentEventNode != null)
            {
                layer.state = STATE.ON_EVENT;
            }
            else
            {
                layer.state = STATE.NON;
            }
        }
    }

    void Start()
    {
        //初回起動時SetAsEntry設定されたEventNode.EventClipのStartEventを実行する
        foreach(var layer in layerList)
        {
            if(layer.currentEventNode != null && layer.currentEventNode.eventClip != null)
            {
                layer.currentEventNode.eventClip.StartEvent();
            }
        }
    }

    void Update()
    {
        //遷移で消費されるTriggerリスト
        List<string> resetTriggerList = new List<string>();

        //各レイヤーごとの処理
        foreach (var layer in layerList)
        {
            switch (layer.state)
            {
                case STATE.NON:
                    break;

                //遷移処理
                case STATE.TRANSITION:
                    if (layer.currentEventNode.eventClip != null)
                    {
                        layer.currentEventNode.eventClip.EndEvent();
                    }

                    layer.currentEventNode = layer.nextEventNode;

                    if (layer.currentEventNode.eventClip != null)
                    {
                        layer.currentEventNode.eventClip.StartEvent();
                    }

                    layer.state = STATE.ON_EVENT;
                    break;

                //イベント中
                case STATE.ON_EVENT:
                    if (layer.currentEventNode.eventClip != null)
                    {
                        layer.currentEventNode.eventClip.OnEvent();
                    }

                    if (layer.currentEventNode != null)
                    {
                        //各遷移に対する処理
                        foreach (var output in layer.currentEventNode.outputTransitions)
                        {
                            bool allTrue = true;
                            foreach (var condition in output.conditions)
                            {
                                if (!condition.Compare(paramList))
                                {
                                    allTrue = false;
                                    break;
                                }
                            }

                            //すべての条件を満たしかつ次のノードが存在するとき遷移を開始する
                            if (allTrue && (output.toEventNode != null))
                            {
                                //Triggerを取得する
                                foreach (var condition in output.conditions)
                                {
                                    if (condition.paramType == PARAM_TYPE.TRIGGER)
                                    {
                                        //消費されるTriggerリストに追加する
                                        resetTriggerList.Add(condition.paramName);
                                    }
                                }

                                layer.nextEventNode = output.toEventNode;
                                layer.state = STATE.TRANSITION;
                            }
                        }
                    }
                    break;
            }

            /*
            if (layer.currentEventNode != null)
            {
                Debug.Log("[EventController] Layer: " + layer.name + "; currentEvent: " + layer.currentEventNode.name);
            }
            */
        }

        //Triggerを消費する
        foreach (var trigger in resetTriggerList)
        {
            ResetTrigger(trigger);
        }
    }

    public void ResetTrigger(string name)
    {
        foreach (var param in paramList)
        {
            if (param.name == name && param.type == PARAM_TYPE.TRIGGER)
            {
                param.valBool = false;
            }
        }
    }

    public void SetTrigger(string name)
    {
        foreach (var param in paramList)
        {
            if (param.name == name && param.type == PARAM_TYPE.TRIGGER)
            {
                param.valBool = true;
            }
        }
    }

    public void SetBool(string name, bool value)
    {
        foreach (var param in paramList)
        {
            if (param.name == name && param.type == PARAM_TYPE.BOOL)
            {
                param.valBool = value;
            }
        }
    }

    public void SetInteger(string name, int value)
    {
        foreach (var param in paramList)
        {
            if (param.name == name && param.type == PARAM_TYPE.INT)
            {
                param.valInt = value;
            }
        }
    }

    public void SetFloat(string name, float value)
    {
        foreach (var param in paramList)
        {
            if (param.name == name && param.type == PARAM_TYPE.FLOAT)
            {
                param.valFloat = value;
            }
        }
    }

    public bool GetBool(string name)
    {
        foreach (var param in paramList)
        {
            if (param.name == name && param.type == PARAM_TYPE.BOOL)
            {
                return param.valBool;
            }
        }

        return false;
    }

    public int GetInteger(string name)
    {
        foreach (var param in paramList)
        {
            if (param.name == name && param.type == PARAM_TYPE.INT)
            {
                return param.valInt;
            }
        }

        return 0;
    }

    public float GetFloat(string name)
    {
        foreach (var param in paramList)
        {
            if (param.name == name && param.type == PARAM_TYPE.FLOAT)
            {
                return param.valFloat;
            }
        }

        return 0.0f;
    }


    //===各サポートクラス================================================================================

    //レイヤー
    [System.Serializable]
    public class Layer
    {
        public string name = "NewLayer";

        //EventNodeリスト
        //各イベントが登録されている
        public List<EventNode> eventNodeList = new List<EventNode>();

        //シリアライズ用のイベントリスト
        //Editor編集の際はここが変わる
        public List<SerializableEventNode> serializableEventNodeList = new List<SerializableEventNode>();

        public EventNode currentEventNode = null;
        public EventNode nextEventNode = null;
        public STATE state = STATE.NON;

        public Layer(string name)
        {
            this.name = name;
        }

    }


    //シリアライズに使用
    //各EventNode用のクラス
    [System.Serializable]
    public class SerializableEventNode
    {
        public string name = "NewEvent";
        public BaseEventClip eventClip;
        public bool entry = false;
        public List<SerializableTransition> outputTransitions;
        public Rect rect;

        public SerializableEventNode(BaseEventClip clip)
        {
            this.eventClip = clip;
        }

        public SerializableEventNode()
        {

        }
    }

    //ランタイムに使用
    //各EventNode用のクラス
    public class EventNode
    {
        public string name = "NewEvent";
        public BaseEventClip eventClip;
        public bool entry = false;
        public List<Transition> outputTransitions = new List<Transition>();
        public Rect rect = new Rect();

        public EventNode(BaseEventClip clip)
        {
            this.eventClip = clip;
        }

        public EventNode()
        {

        }
    }

    //シリアライズに使用
    //各Transition用のクラス
    [System.Serializable]
    public class SerializableTransition
    {
        public List<Condition> conditions = new List<Condition>();
        public int indexOfToEventNode = -1;

        public SerializableTransition(List<Condition> conditions, int indexOfToEventNode)
        {
            this.conditions = conditions;
            this.indexOfToEventNode = indexOfToEventNode;
        }

        public SerializableTransition()
        {

        }
    }

    //ランタイムに使用
    //各Transition用のクラス
    public class Transition
    {
        public List<Condition> conditions = new List<Condition>();
        public EventNode toEventNode = new EventNode();

        public Transition(List<Condition> conditions, EventNode toEventNode)
        {
            this.conditions = conditions;
            this.toEventNode = toEventNode;
        }

        public Transition()
        {

        }
    }


    //
    //関数
    //  説明:
    //      編集用の各パラメータをランタイム用のパラメータに設定する
    //
    public void SetController()
    {
        foreach (var layer in layerList)
        {
            var serializableEventNodeList = layer.serializableEventNodeList;

            //作成
            layer.eventNodeList = new List<EventNode>();

            //ノードを追加
            foreach (var serializableEventNode in serializableEventNodeList)
            {
                var eventNode = new EventNode(serializableEventNode.eventClip)
                {
                    name = serializableEventNode.name,
                    entry = serializableEventNode.entry,
                    rect = serializableEventNode.rect
                };
                layer.eventNodeList.Add(eventNode);


                if (eventNode.entry)
                {
                    layer.currentEventNode = eventNode;
                }
            }


            //各ノードの遷移を設定する
            for (int i = 0; i < layer.eventNodeList.Count; i++)
            {
                var eventNode = layer.eventNodeList[i];
                var serializableEventNode = serializableEventNodeList[i];
                var tranditions = new List<Transition>();
                foreach (SerializableTransition serializableTrandition in serializableEventNode.outputTransitions)
                {
                    var trandition = new Transition(serializableTrandition.conditions, layer.eventNodeList[serializableTrandition.indexOfToEventNode]);
                    tranditions.Add(trandition);
                }

                eventNode.outputTransitions = tranditions;
            }
        }
    }



    //Conditionクラス
    [System.Serializable]
    public class Condition
    {
        public string paramName;
        public PARAM_TYPE paramType;
        public COMPARE_OPTIONS compareOption;
        public int judgeInt;
        public float judgeFloat;
        public bool judgeBool;

        public Condition(string paramName, PARAM_TYPE paramType)
        {
            this.paramName = paramName;
            this.paramType = paramType;
        }

        //条件を満たしているか確認します
        public bool Compare(List<Param> paramList)
        {
            foreach (var param in paramList)
            {
                if (param.name == paramName && param.type == paramType)
                {
                    switch (paramType)
                    {
                        case PARAM_TYPE.INT:
                            switch (compareOption)
                            {
                                case COMPARE_OPTIONS.GREATER:
                                    return param.valInt > judgeInt;

                                case COMPARE_OPTIONS.LESS:
                                    return param.valInt < judgeInt;

                                case COMPARE_OPTIONS.EQUALS:
                                    return param.valInt == judgeInt;

                                case COMPARE_OPTIONS.NOT_EQUAL:
                                    return param.valInt != judgeInt;
                            }
                            break;

                        case PARAM_TYPE.FLOAT:
                            switch (compareOption)
                            {
                                case COMPARE_OPTIONS.GREATER:
                                    return param.valFloat > judgeFloat;

                                case COMPARE_OPTIONS.LESS:
                                    return param.valFloat < judgeFloat;

                                case COMPARE_OPTIONS.EQUALS:
                                    return param.valFloat == judgeFloat;

                                case COMPARE_OPTIONS.NOT_EQUAL:
                                    return param.valFloat != judgeFloat;
                            }
                            break;

                        case PARAM_TYPE.BOOL:
                            return param.valBool == judgeBool;

                        case PARAM_TYPE.TRIGGER:
                            return param.valBool;
                    }
                }
            }

            return false;
        }

    }

    //パラメータクラス
    [System.Serializable]
    public class Param
    {
        public string name;
        public PARAM_TYPE type;
        public int valInt = 0;
        public float valFloat = 0.0f;
        public bool valBool = false;

        public Param(string name, PARAM_TYPE type)
        {
            this.name = name;
            this.type = type;
        }
    }

}
