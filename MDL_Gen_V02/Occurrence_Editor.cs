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
    public partial class Occurrence_Editor : Form
    {

        // (ADD) 규칙을 리스트에 추가하기 위한 델리게이션 이벤트 함수 정의
        public delegate void FormSendDataHandler(string[] obj);
        public event FormSendDataHandler FormOccurenceADDEvent;

        public Occurrence_Editor()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // 편집한 발생도 정보를 메인 프레임 리스트 박스와 구조체 배열 자료에 전달하는 기능 수행
            
            string SFI_occur_rate   = textBox1.Text;
            string Occur            = textBox2.Text;

            string[] strs = new string[] { SFI_occur_rate, Occur};

            // 델리게이션 이벤트 함수를 호출하여, Form1 다이알로그에 전달
            FormOccurenceADDEvent(strs);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // 해당 다이아로그를 종료함

            this.Close();
        }
    }
}
