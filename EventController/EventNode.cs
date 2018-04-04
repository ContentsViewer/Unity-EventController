using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EventControl
{

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

}
