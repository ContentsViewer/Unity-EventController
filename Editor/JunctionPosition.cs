
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;


namespace EventControl.EventControllerEditor
{


    //接続点の設定のためのクラス
    public class JunctionPosition
    {
        public JunctionSide side = JunctionSide.Left;
        public int top;

        public JunctionPosition(JunctionSide side, int top)
        {
            this.side = side;
            this.top = top;
        }
    }

}