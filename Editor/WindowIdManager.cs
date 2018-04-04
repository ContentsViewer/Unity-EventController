
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;


namespace EventControl.EventControllerEditor
{


    //ノードID生成クラス
    public static class WindowIdManager
    {
        static int id = 0;
        public static int Create()
        {
            return id++;
        }
    }

}