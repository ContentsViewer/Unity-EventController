using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EventControl
{

    //レイヤー
    [System.Serializable]
    public class Layer
    {

        public enum STATE
        {
            NON,
            TRANSITION,
            ON_EVENT
        }

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


}