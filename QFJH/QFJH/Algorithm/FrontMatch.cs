using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Math;
using QFJH.DataStruct;

namespace QFJH.Algorithm
{
    /// <summary>
    /// 前方交汇
    /// </summary>
    public class FrontMatch
    {
        /// <summary>
        /// 是否计算过了
        /// </summary>
        public bool HasProcessed { get; private set; }

        #region 杂七杂八的私有变量，用反射者死
        private readonly List<DataList> _targetList;
        private readonly List<Dictionary<string, double>> _targetMatch = new List<Dictionary<string, double>>();
        private readonly CameraPara _cam;
        private readonly BackMatch _l, _r;
        private readonly double _l0, _h0;
        
        // 左右图像旋转矩阵
        private readonly Matrix[] _r1r2;
        // 左右图像摄影基线分量
        private readonly double[,] _bUvw;
        #endregion

        /// <summary>
        /// 返回计算用的字典，用于数据保存（临时添加）
        /// </summary>
        public List<Dictionary<string, double>> GetDictForSave()
        {
            return this._targetMatch;
        }

        /// <summary>
        /// 前方交会
        /// </summary>
        /// <param name="left">左影像的后方交会结果</param>
        /// <param name="right">右影像的后方交会结果</param>
        /// <param name="tar">要匹配的点</param>
        /// <param name="cam">相机参数</param>
        public FrontMatch(BackMatch left, BackMatch right, List<DataList> tar, CameraPara cam)
        {
            this.HasProcessed = false;
            this._l = left;
            this._r = right;
            this._cam = cam;
            this._targetList = tar;

            // PPT:4-1.P9
            _l0 = (cam.WidthPix - 1) / 2.0 + cam.MainPosX / cam.PixSize;
            _h0 = (cam.HeightPix - 1) / 2.0 + cam.MainPosY / cam.PixSize;

            MakeTargetList(tar);
            this._r1r2 = GetR1R2();
            this._bUvw = CalcBaselineB();
        }

        /// <summary>
        /// 左右图像的旋转矩阵
        /// </summary>
        private Matrix[] GetR1R2()
        {
            return new[]
            {
                MatrixR(this._l),
                MatrixR(this._r)
            };
        }

        /// <summary>
        /// 摄影基线B
        /// 书：P77-(b)
        /// </summary>
        private double[,] CalcBaselineB()
        {
            return new[,]
            {
                {this._r.Xs - this._l.Xs},
                {this._r.Ys - this._l.Ys},
                {this._r.Zs - this._l.Zs}
            };
        }

        /// <summary>
        /// 开始计算
        /// </summary>
        public void Process()
        {
            MainLoop();
            this.HasProcessed = true;
        }

        private void MainLoop()
        {
            for (int i = 0; i < _targetMatch.Count; i++)
            {
                var asst = AssistantUvw(i);
                var prjConst = CalcPrjConstant(asst);
                var newAsst = NewAssistant(prjConst, asst);
                UpdateCoord(i, newAsst);
            }
        }

        /// <summary>
        /// 更新前方交会的结果
        /// 书：P77-(5-13)
        /// </summary>
        private void UpdateCoord(int idx, Matrix[] t)
        {
            var mA = t[0].Data;
            var mB = t[1].Data;
            double
                x = this._l.Xs + mA[0, 0],
                y = 0.5 * (this._l.Ys + mA[1, 0] + this._r.Ys + mB[1, 0]),
                z = this._r.Zs + mB[2, 0];
            _targetList[idx].SetX(x);
            _targetList[idx].SetY(y);
            _targetList[idx].SetZ(z);
        }

        /// <summary>
        /// 投影系数以后的
        /// 书：P77-(d)
        /// </summary>
        private Matrix[] NewAssistant(double[] N, Matrix[] asst)
        {
            var s1 = N[0] * asst[0];
            var s2 = N[1] * asst[1];

            return new[] {s1, s2};
        }

        /// <summary>
        /// 投影系数
        /// 书：P78-(5-14)
        /// </summary>
        /// <returns></returns>
        private double[] CalcPrjConstant(Matrix[] asst)
        {
            var mA = asst[0].Data;
            var mB = asst[1].Data;
            double
                fm = mA[0, 0] * mB[2, 0] - mB[0, 0] * mA[2, 0],
                n1 = (this._bUvw[0, 0] * mB[2, 0] - this._bUvw[2, 0] * mB[0, 0]) / fm,
                n2 = (this._bUvw[0, 0] * mA[2, 0] - this._bUvw[2, 0] * mA[0, 0]) / fm;
            return new[] {n1, n2};
        }

        /// <summary>
        /// 返回左右图像的辅助坐标
        /// 书：P76-(a)
        /// </summary>
        private Matrix[] AssistantUvw(int idx)
        {
            var f = this._cam.f;
            double x1 = this._targetMatch[idx]["lXa"],
                y1 = this._targetMatch[idx]["lYa"],
                x2 = this._targetMatch[idx]["rXa"],
                y2 = this._targetMatch[idx]["rYa"];

            double[,]
                tmp1 =
                {
                    {x1}, {y1}, {-f}
                },
                tmp2 =
                {
                    {x2}, {y2}, {-f}
                };

            return new[] {
                this._r1r2[0] * new Matrix(tmp1, true),
                this._r1r2[1] * new Matrix(tmp2, true)
            };
        }

        /// <summary>
        /// 计算旋转矩阵R
        /// PPT:2-2.P31（P32为展开后的结果）
        /// </summary>
        /// <returns>R阵</returns>
        private Matrix MatrixR(BackMatch b)
        {
            double[,]
                Rp =
                {
                    {Cos(b.p), 0, -Sin(b.p)},
                    {0, 1, 0},
                    {Sin(b.p), 0, Cos(b.p)}
                },
                Rw =
                {
                    {1, 0, 0},
                    {0, Cos(b.w), -Sin(b.w)},
                    {0, Sin(b.w), Cos(b.w)}
                },
                Rk =
                {
                    {Cos(b.k), -Sin(b.k), 0},
                    {Sin(b.k), Cos(b.k), 0},
                    {0, 0, 1}
                };

            Matrix mp = new Matrix(Rp, true),
                mw = new Matrix(Rw, true),
                mk = new Matrix(Rk, true);
            return mp * mw * mk;
        }

        /// <summary>
        /// 影像上任一像点a(h行,l列)处的像平面坐标(xa, ya)
        /// </summary>
        private void RowColtoImgPaneCoord(double h, double l, out double xa, out double ya)
        {
            // PPT:4-1.P9 
            xa = (l - _l0) * _cam.PixSize;
            ya = (_h0 - h) * _cam.PixSize;
        }

        private void MakeTargetList(List<DataList> data)
        {
            foreach (var t in data)
            {
                double xa, ya;
                var tmp = new Dictionary<string, double>
                {
                    {"ID", t.PointNumber},
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
