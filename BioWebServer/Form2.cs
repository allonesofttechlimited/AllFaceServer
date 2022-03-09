using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BioWebServer
{
    public partial class Form2 : Form
    {

        private int isofmt;
        private int imgfmt;

        public Form2()
        {
            InitializeComponent();
        }

        public int getImageFormat()
        {
            if (this.radioButton1.Checked)
                imgfmt = 0;
            else if (this.radioButton2.Checked)
                imgfmt = 1;
            else if (this.radioButton3.Checked)
                imgfmt = 2;
            else if (this.radioButton4.Checked)
                imgfmt = 3;
            return imgfmt;
        }

        public void setImageFormat(int img)
        {
            imgfmt = img;
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            switch(imgfmt)
            {
                case 0:
                    this.radioButton1.Checked = true;
                    break;
                case 1:
                    this.radioButton2.Checked = true;
                    break;
                case 2:
                    this.radioButton3.Checked = true;
                    break;
                case 3:
                    this.radioButton4.Checked = true;
                    break;
            }
        }

    }
}
