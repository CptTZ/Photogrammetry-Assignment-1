using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using QFJH.Algorithm;
using QFJH.Properties;

namespace QFJH.UI
{
    public partial class ViewOuter : Form
    {
        private readonly BackMatch _bm;

        public ViewOuter(BackMatch b, string p)
        {
            this._bm = b;
            InitializeComponent();
            this.Text = Resources.Prog_Name+" - 外方位元素查看";
            this.textBox9.Text = p;

            FillData();
        }

        private void FillData()
        {
            this.textBox1.Text = _bm.Xs.ToString("0.########");
            this.textBox2.Text = _bm.Ys.ToString("0.########");
            this.textBox3.Text = _bm.Zs.ToString("0.########");
            this.textBox4.Text = _bm.p.ToString("0.########");
            this.textBox5.Text = _bm.w.ToString("0.########");
            this.textBox6.Text = _bm.k.ToString("0.########");
            this.textBox7.Text = _bm.ItCount.ToString();
            this.textBox8.Text = _bm.GetLimit().ToString("e2");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
