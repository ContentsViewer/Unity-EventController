using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EventControl
{


    //パラメータクラス
    [System.Serializable]
    public class EventControllerParameter
    {

        public enum Type
        {
            Int,
            Float,
            Bool,
            Trigger //遷移で消費される
        }


        public string name;
        public Type type;
        public int valInt = 0;
        public float valFloat = 0.0f;
        public bool valBool = false;

        public EventControllerParameter(string name, Type type)
        {
            this.name = name;
            this.type = type;
        }
    }

}