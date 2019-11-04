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
    public partial class Severity_Rule_Editor : Form
    {

        // (ADD) 규칙을 리스트에 추가하기 위한 델리게이션 이벤트 함수 정의
        public delegate void FormSendDataHandler(string[] obj);
        public event FormSendDataHandler FormRuleADDEvent;


        // 고장 판정 규칙 설정 변수 (3월 18일)

        int F_decision_index = 0;  


        public Severity_Rule_Editor()
        {
            InitializeComponent();

            // Continuous_error_range_check()
            //
            //
            string[] data = { "지속적 오차범위 검사(주행검사)", "특정 시간 데이터 확인(감속확인)", "특정 값 발생 확인" };

            comboBox1.Items.AddRange(data);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // 규칙추가 버튼 클릭

            string rule_f_time = textBox1.Text;
            string rule_range_val = textBox2.Text;
            string rule_specify_val = textBox3.Text;
            string rule_severity = textBox4.Text;
            string rule_sim_time = textBox5.Text;
            string rule_type = F_decision_index.ToString();

            string[] strs = new string[] {rule_type, rule_f_time, rule_range_val, rule_specify_val, rule_severity, rule_sim_time };
            

            // 델리게이션 이벤트 함수를 호출하여, Form1 다이알로그에 전달
            FormRuleADDEvent(strs);

        }

        private void button2_Click(object sender, EventArgs e)
        {
            // 다이알로그 종료 클릭

            this.Close();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.Items[comboBox1.SelectedIndex].ToString() == "지속적 오차범위 검사(주행검사)")
            {
                textBox1.ReadOnly = true;           // 시간 
                textBox1.Text = "NONE";
                textBox2.ReadOnly = false;          // 범위
                textBox2.Text = "0";
                textBox3.ReadOnly = true;           // 이벤트 값
                textBox3.Text = "NONE";
                F_decision_index = 1;
            }
            else if(comboBox1.Items[comboBox1.SelectedIndex].ToString() == "특정 시간 데이터 확인(감속확인)")
            {
                textBox1.ReadOnly = false;           // 시간           0-0 시간 범위
                textBox1.Text = "0";
                textBox2.ReadOnly = true;          // 범위
                textBox2.Text = "NONE";
                textBox3.ReadOnly = false;           // 이벤트 값       0-0 값 범위
                textBox3.Text = "0";
                F_decision_index = 2;
            }
            else if (comboBox1.Items[comboBox1.SelectedIndex].ToString() == "특정 값 발생 확인")
            {
                textBox1.ReadOnly = true;           // 시간 
                textBox1.Text = "NONE";
                textBox2.ReadOnly = true;          // 범위
                textBox2.Text = "NONE";
                textBox3.ReadOnly = false;           // 이벤트 값
                textBox3.Text = "0";
                F_decision_index = 3;

            }
            else
            {

            }
            
        }
    }
}
