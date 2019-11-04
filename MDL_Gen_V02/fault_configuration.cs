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
    public partial class fault_configuration : Form
    {
        // (ADD) 결함 설정 값을 저장하기 위한 델리게이션 이벤트 함수 정의
        public delegate void FormSendDataHandler(string[] obj);
        public event FormSendDataHandler FormSaveEvent;

        public fault_configuration()
        {
            InitializeComponent();

            // 리스트 박스 초기화
            string[] data = { "Loss of Function", "More Than Requested",
                "Less Than Requested", "Wrong Direction", "Unintended Activation", "Locked Function" ,
                "Early Timing", "Late Timing"};
            comboBox1.Items.AddRange(data);
            comboBox1.SelectedIndex = 0;

            // Fault enable 초기화
            textBox1.Text = "1";
            // Fault disable 초기화
            textBox2.Text = "10";
            // Fautl value 초기화
            textBox3.Text = "1";
        }
                
        public fault_configuration(string Block_lib, string Occur_type, string F_enable, 
            string F_disable, string F_duration, string F_value)
        {
            InitializeComponent();

            string[] data = { "Loss of Function", "More Than Requested",
                "Less Than Requested", "Wrong Direction", "Unintended Activation", "Locked Function" ,
                "Early Timing", "Late Timing"};
            comboBox1.Items.AddRange(data);

            for (int i=0; i < comboBox1.Items.Count; i++)
            {
                if(comboBox1.Items[i].ToString() == Block_lib)
                {
                    comboBox1.SelectedIndex = i;
                }

            }


            if(Occur_type == "Permenent") { radioButton1.Checked = true;    }
            else if(Occur_type == "Transient")  {   radioButton2.Checked = true;    }
            else if(Occur_type == "Intermittent")   {   radioButton3.Checked = true;    }
            else  {     }


            textBox1.Text = F_enable;
            textBox2.Text = F_disable;
            textBox3.Text = F_duration;
            textBox4.Text = F_value;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // cancel 처리


            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Save and Cancel 처리

            string Block_lib = comboBox1.Text;
            string Occur_type = null;
            if (radioButton1.Checked == true) Occur_type = "Permenent";
            else if (radioButton2.Checked == true) Occur_type = "Transient";
            else if (radioButton3.Checked == true) Occur_type = "Intermittent";

            string F_enable = textBox1.Text;
            string F_disable = textBox2.Text;
            string F_duration = textBox3.Text;
            string F_value = textBox4.Text;

            string[] strs = new string[] { Block_lib, Occur_type, F_enable, F_disable, F_duration, F_value };

            this.FormSaveEvent(strs);
            
            this.Close();

        }
    }
}
