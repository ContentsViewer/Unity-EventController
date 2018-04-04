using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EventControl
{

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
}