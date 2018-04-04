using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EventControl
{

    //Conditionクラス
    [System.Serializable]
    public class Condition
    {
        public string parameterName;
        public EventControllerParameter.Type parameterType;
        public CompareOptions compareOption;
        public int judgeInt;
        public float judgeFloat;
        public bool judgeBool;

        public Condition(string parameterName, EventControllerParameter.Type parameterType)
        {
            this.parameterName = parameterName;
            this.parameterType = parameterType;
        }
        
        /// <summary>
        /// 条件を満たしているか確認します
        /// </summary>
        /// <param name="parameterManager"></param>
        /// <returns></returns>
        public bool Compare(EventControllerParameterManager parameterManager)
        {
            switch (parameterType)
            {
                case EventControllerParameter.Type.Bool:
                    if (parameterManager.boolParameterMap.ContainsKey(parameterName))
                    {
                        var parameter = parameterManager.boolParameterMap[parameterName];
                        return parameter.valBool == judgeBool;
                    }
                    break;

                case EventControllerParameter.Type.Float:
                    if (parameterManager.floatParameterMap.ContainsKey(parameterName))
                    {
                        var parameter = parameterManager.floatParameterMap[parameterName];
                        switch (compareOption)
                        {
                            case CompareOptions.Greater:
                                return parameter.valFloat > judgeFloat;

                            case CompareOptions.Less:
                                return parameter.valFloat < judgeFloat;

                            case CompareOptions.Equals:
                                return parameter.valFloat == judgeFloat;

                            case CompareOptions.NotEquals:
                                return parameter.valFloat != judgeFloat;
                        }
                    }
                    break;

                case EventControllerParameter.Type.Int:
                    if (parameterManager.intParameterMap.ContainsKey(parameterName))
                    {

                        var parameter = parameterManager.intParameterMap[parameterName];
                        switch (compareOption)
                        {
                            case CompareOptions.Greater:
                                return parameter.valInt > judgeInt;

                            case CompareOptions.Less:
                                return parameter.valInt < judgeInt;

                            case CompareOptions.Equals:
                                return parameter.valInt == judgeInt;

                            case CompareOptions.NotEquals:
                                return parameter.valInt != judgeInt;
                        }
                    }
                    break;


                case EventControllerParameter.Type.Trigger:
                    if (parameterManager.triggerParameterMap.ContainsKey(parameterName))
                    {
                        var parameter = parameterManager.triggerParameterMap[parameterName];
                        return parameter.valBool;
                    }

                    break;
            }
            return false;
        }

    }
}
