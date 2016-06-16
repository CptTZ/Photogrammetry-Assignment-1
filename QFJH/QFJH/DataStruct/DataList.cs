using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QFJH.DataStruct
{
    /// <summary>
    /// 数据表
    /// </summary>
    public class DataList
    {
        /// <summary>
        /// 点号
        /// </summary>
        public int PointNumber { get; private set; }

        /// <summary>
        /// 左影像列号
        /// </summary>
        public double LeftColNumber { get; private set; }

        /// <summary>
        /// 右影像列号
        /// </summary>
        public double RightColNumber { get; private set; }

        /// <summary>
        /// 左影像行号
        /// </summary>
        public double LeftRowNumber { get; private set; }

        /// <summary>
        /// 右影像行号
        /// </summary>
        public double RightRowNumber { get; private set; }

        /// <summary>
        /// Y坐标
        /// </summary>
        public double Y { get; private set; }

        /// <summary>
        /// Z坐标
        /// </summary>
        public double Z { get; private set; }

        /// <summary>
        /// X坐标
        /// </summary>
        public double X { get; private set; }

        /// <summary>
        /// 初始化数据表
        /// </summary>
        /// <param name="num">点号</param>
        /// <param name="lc">左影像列号</param>
        /// <param name="lr">左影像行号</param>
        /// <param name="rc">右影像列号</param>
        /// <param name="rr">右影像行号</param>
        public DataList(int num, double lc, double lr, double rc, double rr)
        {
            this.PointNumber = num;
            this.LeftColNumber = lc;
            this.RightColNumber = rc;
            this.LeftRowNumber = lr;
            this.RightRowNumber = rr;
        }

        /// <summary>
        /// 设置X坐标
        /// </summary>
        /// <param name="val">设置值</param>
        public void SetX(double val)
        {
            this.X = val;
        }

        /// <summary>
        /// 设置Y坐标
        /// </summary>
        /// <param name="val">设置值</param>
        public void SetY(double val)
        {
            this.Y = val;
        }

        /// <summary>
        /// 设置Z坐标
        /// </summary>
        /// <param name="val">设置值</param>
        public void SetZ(double val)
        {
            this.Z = val;
        }

    }
}
