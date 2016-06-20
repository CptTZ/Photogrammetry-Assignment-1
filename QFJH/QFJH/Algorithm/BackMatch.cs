using System;
using System.Collections.Generic;
using System.Linq;
using QFJH.DataStruct;
using static System.Math;

namespace QFJH.Algorithm
{
    /// <summary>
    /// 后方交汇
    /// 本实现方法易于理解，算法效率一般
    /// </summary>
    public class BackMatch
    {
        #region 后方交会公开属性
        /// <summary>
        /// 是否计算过了
        /// </summary>
        public bool HasProcessed { get; private set; }

        /// <summary>
        /// S在地面摄影坐标系中的X坐标
        /// </summary>
        public double Xs { get; private set; }

        /// <summary>
        /// S在地面摄影坐标系中的Y坐标
        /// </summary>
        public double Ys { get; private set; }

        /// <summary>
        /// S在地面摄影坐标系中的Z坐标
        /// </summary>
        public double Zs { get; private set; }

        /// <summary>
        /// 航向倾角
        /// </summary>
        public double p { get; private set; }

        /// <summary>
        /// 旁向倾角
        /// </summary>
        public double w { get; private set; }

        /// <summary>
        /// 像片旋角
        /// </summary>
        public double k { get; private set; }

        /// <summary>
        /// 航高(m)
        /// </summary>
        public double FlightHeight { get; private set; }
        
        /// <summary>
        /// 比例尺(m)
        /// </summary>
        public double M { get; private set; }

        /// <summary>
        /// 迭代次数
        /// </summary>
        public int ItCount { get; private set; }

        #endregion

        private readonly double _l0, _h0;
        private double _limits = 0.001;
        private readonly List<Dictionary<string, double>> _existMatch = new List<Dictionary<string, double>>();
        private readonly CameraPara _camData;

        /// <summary>
        /// 设置限差
        /// </summary>
        public void SetLimit(double a)
        {
            this._limits = a;
        }

        public double GetLimit()
        {
            return this._limits;
        }

        /// <summary>
        /// 返回计算用的字典，用于数据保存（临时添加）
        /// </summary>
        public List<Dictionary<string, double>> GetDictForSave()
        {
            return this._existMatch;
        }

        /// <summary>
        /// 后方交会处理
        /// </summary>
        /// <param name="data">匹配线数据</param>
        /// <param name="cam">相机参数</param>
        /// <param name="dir">左影像或者右影像</param>
        public BackMatch(List<DataList> data, CameraPara cam, string dir)
        {
            if (data.Count < 4)
                throw new FormatException("进行单张像片的空间后方交会，至少应有三个已知三维坐标的地面控制点！");

            this.HasProcessed = false;
            this._camData = cam;
            // PPT:4-1.P9
            _l0 = (_camData.WidthPix - 1) / 2.0 + _camData.MainPosX / _camData.PixSize;
            _h0 = (_camData.HeightPix - 1) / 2.0 + _camData.MainPosY / _camData.PixSize;
            MakeMatchList(data, dir);
        }

        /// <summary>
        /// 处理计算过程
        /// </summary>
        public void Process()
        {
            CalcScale();
            InitialOuter();
            MainLoop();
            this.HasProcessed = true;
        }

        /// <summary>
        /// 计算全图比例尺
        /// </summary>
        private void CalcScale()
        {
            List<double> scale = new List<double>();
            for (int i = 0; i < _existMatch.Count; i++)
            {
                for (int j = i + 1; j < _existMatch.Count; j++)
                {
                    double x1 = _existMatch[i]["x"],
                        y1 = _existMatch[i]["y"],
                        x2 = _existMatch[j]["x"],
                        y2 = _existMatch[j]["y"],
                        xa1 = _existMatch[i]["xa"],
                        ya1 = _existMatch[i]["ya"],
                        xa2 = _existMatch[j]["xa"],
                        ya2 = _existMatch[j]["ya"];
                    // 真实单位是m，相片单位是mm
                    double lenReal = Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2)),
                        lenImg = Sqrt((xa1 - xa2) * (xa1 - xa2) + (ya1 - ya2) * (ya1 - ya2));
                    scale.Add(lenReal / (lenImg / 1000));
                }
            }
            this.M = scale.Average(); //米
            this.FlightHeight = _camData.f / 1000 * this.M; // PPT:2-1.P17
        }
        
        /// <summary>
        /// 初始化外方位元素
        /// </summary>
        private void InitialOuter()
        {
            // 书.P75
            this.Zs = this.FlightHeight;
            this.p = 0;
            this.w = 0;
            this.k = 0;
            int count = _existMatch.Count;
            double xs = 0, ys = 0;

            //TODO: 何为四个角上的控制点，直接取所有的可不可以
            for (int i = 0; i < count; i++)
            {
                xs += _existMatch[i]["x"];
                ys += _existMatch[i]["y"];
            }

            this.Xs = xs / count;
            this.Ys = ys / count;
        }

        /// <summary>
        /// 主迭代计算
        /// </summary>
        private void MainLoop()
        {
            #region 需要的变量申明
            this.ItCount = 0;
            double[] xNear = new double[_existMatch.Count],
                    yNear = new double[_existMatch.Count],
                    zNear = new double[_existMatch.Count];
            Matrix[] errA = new Matrix[_existMatch.Count],
                errL = new Matrix[_existMatch.Count];
            double[,] dFinal = null;
            #endregion
            // PPT:4-1.P25
            do
            {
                var R = MatrixR();
                GXFC(R, xNear, yNear, zNear);
                ErrorEqu(R, xNear, yNear, zNear, errA, errL);
                dFinal = SolveEqu(errA, errL);

                this.Xs += dFinal[0, 0];
                this.Ys += dFinal[1, 0];
                this.Zs += dFinal[2, 0];
                this.p += dFinal[3, 0];
                this.w += dFinal[4, 0];
                this.k += dFinal[5, 0];

                this.ItCount++;
            } while (!HasLimited(dFinal));
        }

        /// <summary>
        /// 解方程，求出本次迭代的外方位元素
        /// 书P72,(5-6)
        /// </summary>
        private double[,] SolveEqu(Matrix[] aMat, Matrix[] lMat)
        {
            Matrix mergeA = MergeMatrix(aMat), mergeL = MergeMatrix(lMat);

            // 变量名看书上公式就懂
            var AT = MatrixOperation.MatrixTrans(mergeA);
            var final = (1 / (AT * mergeA)) * AT * mergeL;

            // 结果一定是6*1的矩阵
            return final.Data;
        }

        /// <summary>
        /// 合并矩阵
        /// </summary>
        private Matrix MergeMatrix(Matrix[] aMat)
        {
            int count = aMat.Length,
                rowCount = aMat[0].Data.GetLength(0),
                colCount = aMat[0].Data.GetLength(1);
            double[,] tmp = new double[rowCount * count, colCount];

            for (int i = 0; i < count; i++)
            {
                var A = aMat[i].Data;
                for (int j = 0; j < colCount; j++)
                {
                    for (int l = 0; l < rowCount; l++)
                    {
                        tmp[rowCount * i + l, j] = A[l, j];
                    }
                }
            }

            return new Matrix(tmp, true);
        }

        /// <summary>
        /// 计算误差方程式（A与L）
        /// PPT:4-1.P19-22，概念见PPT:4-1.P17
        /// </summary>
        private void ErrorEqu(Matrix r, double[] xGx, double[] yGx, double[] zGx, Matrix[] A, Matrix[] L)
        {
            var R = r.Data;
            var f = this._camData.f;

            for (int i = 0; i < _existMatch.Count; i++)
            {
                double[,] tmpA = new double[2, 6],
                    tmpL = new double[2, 1];
                double x = _existMatch[i]["xa"],
                    y = _existMatch[i]["ya"];

                tmpA[0, 0] = (R[0, 0] * f + R[0, 2] * x) / zGx[i];
                tmpA[0, 1] = (R[1, 0] * f + R[1, 2] * x) / zGx[i];
                tmpA[0, 2] = (R[2, 0] * f + R[2, 2] * x) / zGx[i];
                tmpA[0, 3] = y * Sin(this.w) - ((x / f) * (x * Cos(this.k) - y * Sin(this.k)) + f * Cos(this.k)) * Cos(this.w);
                tmpA[0, 4] = -f * Sin(this.k) - (x / f) * (x * Sin(this.k) + y * Cos(this.k));
                tmpA[0, 5] = y;
                // PPT:4-1.P19公式错误，参考书P72,75
                tmpA[1, 0] = (R[0, 1] * f + R[0, 2] * y) / zGx[i];
                tmpA[1, 1] = (R[1, 1] * f + R[1, 2] * y) / zGx[i];
                tmpA[1, 2] = (R[2, 1] * f + R[2, 2] * y) / zGx[i];
                tmpA[1, 3] = -x * Sin(this.w) - ((y / f) * (x * Cos(this.k) - y * Sin(this.k)) - f * Sin(this.k)) * Cos(this.w);
                tmpA[1, 4] = -f * Cos(this.k) - (y / f) * (x * Sin(this.k) + y * Cos(this.k));
                tmpA[1, 5] = -x;

                tmpL[0, 0] = x - xGx[i];
                tmpL[1, 0] = y - yGx[i];

                A[i] = new Matrix(tmpA, true);
                L[i] = new Matrix(tmpL, true);
            }
        }

        /// <summary>
        /// 共线方程X,Y解算，其中分母均为z
        /// PPT:4-1.P13
        /// </summary>
        private void GXFC(Matrix r, double[] x, double[] y, double[] z)
        {
            var R = r.Data;
            var f = this._camData.f;
            for (int i = 0; i < _existMatch.Count; i++)
            {
                double dX = (_existMatch[i]["x"] - this.Xs),
                    dY = (_existMatch[i]["y"] - this.Ys),
                    dZ = (_existMatch[i]["z"] - this.Zs);
                z[i] = R[0, 2] * dX + R[1, 2] * dY + R[2, 2] * dZ;

                x[i] = -f * (R[0, 0] * dX + R[1, 0] * dY + R[2, 0] * dZ) / z[i];
                y[i] = -f * (R[0, 1] * dX + R[1, 1] * dY + R[2, 1] * dZ) / z[i];
            }
        }

        /// <summary>
        /// 计算旋转矩阵R
        /// PPT:2-2.P31（P32为展开后的结果）
        /// </summary>
        /// <returns>R阵</returns>
        private Matrix MatrixR()
        {
            double[,]
                Rp =
                {
                    {Cos(this.p), 0, -Sin(this.p)},
                    {0, 1, 0},
                    {Sin(this.p), 0, Cos(this.p)}
                },
                Rw =
                {
                    {1, 0, 0},
                    {0, Cos(this.w), -Sin(this.w)},
                    {0, Sin(this.w), Cos(this.w)}
                },
                Rk =
                {
                    {Cos(this.k), -Sin(this.k), 0},
                    {Sin(this.k), Cos(this.k), 0},
                    {0, 0, 1}
                };
            
            Matrix mp = new Matrix(Rp, true),
                mw = new Matrix(Rw, true),
                mk = new Matrix(Rk, true);
            return mp * mw * mk;
        }

        /// <summary>
        /// 是否收敛，以结束计算
        /// </summary>
        private bool HasLimited(double[,] final)
        {
            if (this.ItCount > 500000) throw new Exception("迭代超过50W次无结果，该方程很可能不收敛");

            for (int i = 0; i < final.GetLength(0); i++)
            {
                if (Abs(final[i, 0]) > _limits) 
                {
                    return false;
                }
            }

            return true;
        }
        /// <summary>
        /// 影像上任一像点a(h行,l列)处的像平面坐标(xa, ya)
        /// </summary>
        private void RowColtoImgPaneCoord(double h, double l, out double xa, out double ya)
        {
            // PPT:4-1.P9 
            xa = (l - _l0) * _camData.PixSize;
            ya = (_h0 - h) * _camData.PixSize;
        }

        private void MakeMatchList(List<DataList> data, string dir)
        {
            var s = dir.ToLower();
            foreach (var t in data)
            {
                var tmp = new Dictionary<string, double>
                {
                    {"ID", t.PointNumber},
                    {"x", t.X},
                    {"y", t.Y},
                    {"z", t.Z}
                };
                double xa, ya;
                switch (s)
                {
                    case "left":
                        tmp.Add("col", t.LeftColNumber);
                        tmp.Add("row", t.LeftRowNumber);
                        RowColtoImgPaneCoord(t.LeftRowNumber, t.LeftColNumber, out xa, out ya);
                        break;

                    case "right":
                        tmp.Add("col", t.RightColNumber);
                        tmp.Add("row",t.RightRowNumber);
                        RowColtoImgPaneCoord(t.RightRowNumber, t.RightColNumber, out xa, out ya);
                        break;

                    default:
                        throw new Exception("数据格式错误");
                }
                tmp.Add("xa", xa);
                tmp.Add("ya", ya);
                _existMatch.Add(tmp);
            }
        }
         
    }
}
