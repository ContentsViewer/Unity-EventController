using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EventControl
{

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
}
