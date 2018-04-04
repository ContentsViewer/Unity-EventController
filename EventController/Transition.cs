using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EventControl
{



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
}