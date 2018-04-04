using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EventControl
{
    [System.Serializable]
    public class EventControllerParameterManager
    {
        public List<EventControllerParameter> serializableParameterList = new List<EventControllerParameter>();


        public Dictionary<string, EventControllerParameter> intParameterMap = new Dictionary<string, EventControllerParameter>();
        public Dictionary<string, EventControllerParameter> floatParameterMap = new Dictionary<string, EventControllerParameter>();
        public Dictionary<string, EventControllerParameter> boolParameterMap = new Dictionary<string, EventControllerParameter>();
        public Dictionary<string, EventControllerParameter> triggerParameterMap = new Dictionary<string, EventControllerParameter>();


        public EventControllerParameterManager()
        {

        }

        /// <summary>
        /// serializableParameterListの内容をmapに設定します.
        /// </summary>
        public void SetMap()
        {
            intParameterMap.Clear();
            floatParameterMap.Clear();
            boolParameterMap.Clear();
            triggerParameterMap.Clear();


            foreach(var parameter in serializableParameterList)
            {
                if(parameter.type == EventControllerParameter.Type.Bool)
                {
                    if (!boolParameterMap.ContainsKey(parameter.name))
                    {
                        boolParameterMap.Add(parameter.name, parameter);
                    }
                }
                else if(parameter.type == EventControllerParameter.Type.Float)
                {
                    if (!floatParameterMap.ContainsKey(parameter.name))
                    {
                        floatParameterMap.Add(parameter.name, parameter);
                    }
                }
                else if(parameter.type == EventControllerParameter.Type.Int)
                {
                    if (!intParameterMap.ContainsKey(parameter.name))
                    {
                        intParameterMap.Add(parameter.name, parameter);
                    }
                }
                else if(parameter.type == EventControllerParameter.Type.Trigger)
                {
                    if (!triggerParameterMap.ContainsKey(parameter.name))
                    {
                        triggerParameterMap.Add(parameter.name, parameter);
                    }
                }
            }
        }
        
    }

}