using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QFJH.DataStruct
{
    /// <summary>
    /// 数字摄影测量图像-基类
    /// </summary>
    public class DigitalImage
    {
        private readonly Graphics _graph;

        private readonly Pen _markRed = new Pen(Color.Red, 2f);
        private readonly Brush _redSoild = new SolidBrush(Color.Red);
        
        /// <summary>
        /// 文件地址
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// 图像类
        /// </summary>
        public Image ImgData { get; private set; }
        
        /// <summary>
        /// 在画中X,Y处画一个十字
        /// </summary>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        /// <param name="id">ID号</param>
        public void DrawMark(int x, int y, int id)
        {
            _graph.DrawLine(_markRed, x - 7, y, x + 7, y);
            _graph.DrawLine(_markRed, x, y - 7, x, y + 7);
            _graph.DrawString(id.ToString(), new Font("Times New Roman", 1.2f, FontStyle.Bold),
                _redSoild, x + 2, y + 2);
        }

        /// <summary>
        /// 构建新的图像
        /// </summary>
        /// <param name="path">文件地址</param>
        public DigitalImage(string path)
        {
            this.FilePath = path;
            this.ImgData = new Bitmap(path);
            this._graph = Graphics.FromImage(this.ImgData);
        }

    }
}
