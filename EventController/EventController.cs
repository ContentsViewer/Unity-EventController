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


namespace EventControl
{


    public class EventController : MonoBehaviour
    {



        //===パラメータ==============================================================
        public bool save = false;
        public string label = "(non)";

        //パラメータリスト
        //public List<EventControllerParameter> paramList = new List<EventControllerParameter>();
        public EventControllerParameterManager parameterManager = new EventControllerParameterManager();
        public List<Layer> layerList = new List<Layer>();




        //遷移で消費されるTriggerリスト
        List<string> resetTriggerList = new List<string>();

        //===コード==========================================================================================
        void Awake()
        {
            SetController();

        }

        void Start()
        {
            //初回起動時SetAsEntry設定されたEventNode.EventClipのStartEventを実行する
            foreach (var layer in layerList)
            {
                if (layer.currentEventNode != null && layer.currentEventNode.eventClip != null)
                {
                    layer.currentEventNode.eventClip.StartEvent();
                }
            }
        }

        void Update()
        {
            resetTriggerList.Clear();

            //各レイヤーごとの処理
            foreach (var layer in layerList)
            {
                switch (layer.state)
                {
                    case Layer.STATE.NON:
                        break;

                    //遷移処理
                    case Layer.STATE.TRANSITION:
                        if (layer.currentEventNode.eventClip != null)
                        {
                            layer.currentEventNode.eventClip.EndEvent();
                        }

                        layer.currentEventNode = layer.nextEventNode;

                        if (layer.currentEventNode.eventClip != null)
                        {
                            layer.currentEventNode.eventClip.StartEvent();
                        }

                        layer.state = Layer.STATE.ON_EVENT;
                        break;

                    //イベント中
                    case Layer.STATE.ON_EVENT:
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
                                    if (!condition.Compare(parameterManager))
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
                                        if (condition.parameterType == EventControllerParameter.Type.Trigger)
                                        {
                                            //消費されるTriggerリストに追加する
                                            resetTriggerList.Add(condition.parameterName);
                                        }
                                    }

                                    layer.nextEventNode = output.toEventNode;
                                    layer.state = Layer.STATE.TRANSITION;
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
            if (parameterManager.triggerParameterMap.ContainsKey(name))
            {
                parameterManager.triggerParameterMap[name].valBool = false;
            }
            else
            {
                Debug.LogWarning("EventController >> " + name +"(Trigger) was called, but it does not exist.");
            }
        }

        public void SetTrigger(string name)
        {
            if (parameterManager.triggerParameterMap.ContainsKey(name))
            {
                parameterManager.triggerParameterMap[name].valBool = true;
            }
            else
            {
                Debug.LogWarning("EventController >> " + name + "(Trigger) was called, but it does not exist.");
            }
        }

        public void SetBool(string name, bool value)
        {
            if (parameterManager.boolParameterMap.ContainsKey(name))
            {
                parameterManager.boolParameterMap[name].valBool = value;
            }
            else
            {
                Debug.LogWarning("EventController >> " + name + "(Bool) was called, but it does not exist.");
            }
        }

        public void SetInteger(string name, int value)
        {
            if (parameterManager.intParameterMap.ContainsKey(name))
            {
                parameterManager.intParameterMap[name].valInt = value;
            }
            else
            {
                Debug.LogWarning("EventController >> " + name + "(Integer) was called, but it does not exist.");
            }
        }

        public void SetFloat(string name, float value)
        {
            if (parameterManager.floatParameterMap.ContainsKey(name))
            {
                parameterManager.floatParameterMap[name].valFloat = value;
            }
            else
            {
                Debug.LogWarning("EventController >> " + name + "(Float) was called, but it does not exist.");
            }
        }

        public bool GetBool(string name)
        {
            if (parameterManager.boolParameterMap.ContainsKey(name))
            {
                return parameterManager.boolParameterMap[name].valBool;
            }

            Debug.LogWarning("EventController >> " + name + "(Bool) was called, but it does not exist.");
            return false;
        }

        public int GetInteger(string name)
        {
            if (parameterManager.intParameterMap.ContainsKey(name))
            {
                return parameterManager.intParameterMap[name].valInt;
            }


            Debug.LogWarning("EventController >> " + name + "(Integer) was called, but it does not exist.");
            return 0;
        }

        public float GetFloat(string name)
        {

            if (parameterManager.floatParameterMap.ContainsKey(name))
            {
                return parameterManager.floatParameterMap[name].valInt;
            }


            Debug.LogWarning("EventController >> " + name + "(Float) was called, but it does not exist.");

            return 0.0f;
        }

        

        /// <summary>
        /// 編集中のパラメータをランタイム用のパラメータに設定する.
        /// </summary>
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


            // layerの状態設定
            foreach (var layer in layerList)
            {
                var currentEventNode = layer.currentEventNode;
                if (currentEventNode != null)
                {
                    layer.state = Layer.STATE.ON_EVENT;
                }
                else
                {
                    layer.state = Layer.STATE.NON;
                }
            }


            // parameterの設定
            parameterManager.SetMap();
        }



    }
}