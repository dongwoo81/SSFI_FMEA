using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MDL_Gen_V02
{
    public partial class Create_failure_mode : Form
    {

        public delegate void FormSendDataHandler(string obj1, string obj2);
        public event FormSendDataHandler FormFailureModeADDEvent;

        public Create_failure_mode()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Text box 의 문자열을 메인 뷰로 전달하여, 저장한다.
            // 고장영향 정보를 메인 폼에 전달
            string STR = textBox1.Text;
            string STR_NODE_NUM = textBox2.Text;

            // 델리게이션 이벤트 함수를 호출하여, Form1 다이알로그에 전달
            FormFailureModeADDEvent(STR, STR_NODE_NUM);

            this.Close();
        }
    }
}
