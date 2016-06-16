using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QFJH.DataStruct;

namespace QFJH.Algorithm
{
    /// <summary>
    /// 前方交汇
    /// </summary>
    public class FrontMatch
    {

        private readonly List<Dictionary<string, double>> _targetMatch = new List<Dictionary<string, double>>();
        private readonly CameraPara _cam;
        private readonly BackMatch _l, _r;
        private readonly double _l0, _h0;

        /// <summary>
        /// 前方交会
        /// </summary>
        /// <param name="left">左影像的后方交会结果</param>
        /// <param name="right">右影像的后方交会结果</param>
        /// <param name="tar">要匹配的点</param>
        /// <param name="cam">相机参数</param>
        public FrontMatch(BackMatch left, BackMatch right, List<DataList> tar, CameraPara cam)
        {
            this._l = left;
            this._r = right;
            this._cam = cam;

            // PPT:4-1.P9
            _l0 = (cam.WidthPix - 1) / 2.0 + cam.MainPosX / cam.PixSize;
            _h0 = (cam.HeightPix - 1) / 2.0 + cam.MainPosY / cam.PixSize;

            MakeTargetList(tar);
        }

        /// <summary>
        /// 开始计算
        /// </summary>
        /// <param name="tar">直接在目标List中修改X,Y,Z数值</param>
        public void Process(List<DataList> tar)
        {
            
        }

        /// <summary>
        /// 影像上任一像点a(h行,l列)处的像平面坐标(xa, ya)
        /// </summary>
        private void RowColtoImgPaneCoord(double h, double l, out double xa, out double ya)
        {
            // PPT:4-1.P9 
            xa = (l - _l0) * _cam.PixSize / 1000;
            ya = (_h0 - h) * _cam.PixSize / 1000;
        }

        private void MakeTargetList(List<DataList> data)
        {
            foreach (var t in data)
            {
                double xa, ya;
                var tmp = new Dictionary<string, double>
                {
                    {"lCol", t.LeftColNumber},
                    {"lRow", t.LeftRowNumber},
                    {"rCol", t.RightColNumber},
                    {"rRow", t.RightRowNumber}
                };
                
                RowColtoImgPaneCoord(t.LeftRowNumber, t.LeftColNumber, out xa, out ya);
                tmp.Add("lXa", xa);
                tmp.Add("lYa", ya);

                RowColtoImgPaneCoord(t.RightRowNumber, t.RightColNumber, out xa, out ya);
                tmp.Add("rXa", xa);
                tmp.Add("rYa", ya);
                
                _targetMatch.Add(tmp);
            }
        }

    }
}
