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


    

    public partial class SFI_Simulation : Form
    {
        public delegate void FormSendDataHandler(STATISTICAL_FI_SECNARIO_SET[] obj, int count);
        public event FormSendDataHandler FormSaveEvent;


        // 시험 설정 완료 플래그
        bool Fault_Model_Check = false;
        // 전체 결함주입 공간
        int fault_space = 0;

        int set_s_time;
        int set_e_time;
        int fault_min_val;
        int fault_max_val;
        string fault_duration;
        int fault_min_duration;
        int fault_max_duration;


        STATISTICAL_FI_SECNARIO_SET[] data;


        public SFI_Simulation()
        {
            InitializeComponent();
        }

        public SFI_Simulation(full_fault_block [] Block_data, int Block_num)
        {
            InitializeComponent();


            // 결함 모델 리스트 박스 초기화
            for (int i=0; i< Block_num; i++ )
            {
                if (Block_data[i].num_dstport >= 2)
                {
                    for(int k = 0; k < Block_data[i].num_dstport; k++)
                    {
                        if(k == 0)
                        {
                            this.listBox1.Items.Add(Block_data[i].full_path_fault_block_name);
                        }
                        else
                        {
                            string temp = Block_data[i].full_path_fault_block_name + "_" + (k+1).ToString();
                            this.listBox1.Items.Add(temp);
                        }
                    }
                }
                else
                    this.listBox1.Items.Add(Block_data[i].full_path_fault_block_name);
            }


            // 결함 유형 초기화
            string[] data = { "Loss of Function", "More Than Requested",
                "Less Than Requested", "Wrong Direction", "Unintended Activation", "Locked Function" ,
                "Early Timing", "Late Timing"};
            comboBox1.Items.AddRange(data);
            comboBox1.SelectedIndex = 0;


            // 결함발생시간(시작 - 종료)
            textBox1.Text = "3 - 12";
            // 결함 값 구간(최소 - 최대)(%)
            textBox3.Text = "50 - 90";
            // 결함유지 시간(최소-최대)(초)
            textBox4.Text = "1 - 3";

        }

        // 결함을 주입할 위치의 모든 포인트를 선택하여 오른쪽 리스트 박스로 이동함
        private void Selected_ALL_Click(object sender, EventArgs e)
        {
        
            for(int i =0; i < listBox1.Items.Count; i++)
            {
                this.listBox2.Items.Add(listBox1.Items[i].ToString());
            }


        }

        /*
        public string Block_lib;
        public string Occur_type;
        public string F_enable;
        public string F_disable;
        public string F_duration;
        public string F_value;
        */

        private void SFI_VAL_Click(object sender, EventArgs e)
        {
            // 결함주입 시나리오를 작성할 수 있는 구조체를 생성한다.
            // 구조체 생성을 완료한후, SFI 버턴을 활성화 하고, 메시지 박스를 출력한다
            // 결함주입 시나리오 구조체 생성중 필요한 정보가 없으면, 경고 메시지를 출력한다.

            if (Fault_Model_Check == true && textBox2.Text != "")
            {
                int SFI_NUM = Convert.ToInt32(textBox2.Text);
                Random r = new Random();
                int temp_d = 0;
                double temp_double =0;

                data = new STATISTICAL_FI_SECNARIO_SET[SFI_NUM];

                for (int i = 0; i < SFI_NUM; i++)
                {
                    // 결함 주입 모델
                    data[i].Block_lib = comboBox1.Text;

                    // 결함주입 위치 선택
                    temp_d = r.Next(0, listBox2.Items.Count - 1);
                    data[i].Fault_location = listBox2.Items[temp_d].ToString();

                    // 결함주입 시간 선택
                    data[i].F_enable = set_s_time.ToString();
                    data[i].F_disable = set_e_time.ToString();

                    // 결함 값
                    if(comboBox1.Text == "Unintended Activation")
                    {
                        int rand_f_value = r.Next(fault_min_val, fault_max_val);
                        data[i].F_value = rand_f_value.ToString();
                    }
                    else
                    { 
                        temp_double = r.Next(fault_min_val /10, fault_max_val/10) / (double)10;
                        Math.Round(temp_double, 2);
                        data[i].F_value = temp_double.ToString();
                    }

                    // 결함 유지시간
                    temp_d = r.Next(fault_min_duration, fault_max_duration);
                    data[i].F_duration = temp_d.ToString();

                }

                SFI_Ready.Enabled = true;
            }



        }

        private void SFI_Ready_Click(object sender, EventArgs e)
        {
            // 결함주입 시나리오 생성 구조체를 메인 다이알로그로 전송

            this.FormSaveEvent(data, Convert.ToInt32(textBox2.Text));
            this.Close();

        }

        int num_of_sim_i = 0;
        double num_of_sim_d = 0;

        //CI 95.0%
        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (Fault_Model_Check == true)
            {
                double N = fault_space; //(전체 스페이스)Convert.ToDouble(Fautl_Model_Info.F_disable) - Convert.ToDouble(Fautl_Model_Info.F_enable);
                double error_magine = 0.01;
                double t_value = 1.96;
                double init_N = 0.5;

                num_of_sim_d = N / (1 + (error_magine * error_magine) * ((N - 1) / ((t_value * t_value) * init_N * (1 - init_N))));
                num_of_sim_i = Convert.ToInt32(num_of_sim_d);

                textBox2.Text = Convert.ToString(num_of_sim_i);

            }
            else
            {

            }
        }

        // CI 99.0%
        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (Fault_Model_Check == true)
            {
                double N = fault_space; //(전체 스페이스)Convert.ToDouble(Fautl_Model_Info.F_disable) - Convert.ToDouble(Fautl_Model_Info.F_enable);
                double error_magine = 0.01;
                double t_value = 2.5758;
                double init_N = 0.5;

                num_of_sim_d = N / (1 + (error_magine * error_magine) * ((N - 1) / ((t_value * t_value) * init_N * (1 - init_N))));
                num_of_sim_i = Convert.ToInt32(num_of_sim_d);

                textBox2.Text = Convert.ToString(num_of_sim_i);

            }
            else
            {

            }
        }


        //CI 99.9%
        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            if (Fault_Model_Check == true)
            {
                double N = fault_space; //(전체 스페이스)Convert.ToDouble(Fautl_Model_Info.F_disable) - Convert.ToDouble(Fautl_Model_Info.F_enable);
                double error_magine = 0.01;
                double t_value = 3.0902;
                double init_N = 0.5;

                num_of_sim_d = N / (1 + (error_magine * error_magine) * ((N - 1) / ((t_value * t_value) * init_N * (1 - init_N))));
                num_of_sim_i = Convert.ToInt32(num_of_sim_d);

                textBox2.Text = Convert.ToString(num_of_sim_i);

            }
            else
            {

            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int num_location = listBox2.Items.Count;

            // 결함 발생 시간은 1씩 설정
            char[] delimiterChars = { '-', ' ' };
            string[] t_words = textBox1.Text.Split(delimiterChars);
            set_s_time = Convert.ToInt32(t_words[0]);
            set_e_time = Convert.ToInt32(t_words[3]);

            // 결함 값 구간은 10씩 설정
            t_words = textBox3.Text.Split(delimiterChars);
            fault_min_val = Convert.ToInt32(t_words[0]);
            fault_max_val = Convert.ToInt32(t_words[3]);
                       

            // 결함 유지시간은 1씩 설정
            t_words = textBox4.Text.Split(delimiterChars);
            fault_min_duration = Convert.ToInt32(t_words[0]);
            fault_max_duration = Convert.ToInt32(t_words[3]);




            // 결함발생시간(시작 - 종료)
            // textBox1.Text = "3 - 12";
            // 결함 값 구간(최소 - 최대)(%)
            //textBox3.Text = "50 - 90";
            // 결함유지 시간(최소-최대)(초)
            //textBox4.Text = "1 - 3";

            int interval_time = set_e_time - set_s_time + 1;
            int interval_val = (fault_max_val - fault_min_val + 10) / 10;
            int interval_duration = fault_max_duration - fault_min_duration + 1;

            fault_space = num_location * interval_time * interval_val * interval_duration;

            // 설정 완료 확인 버튼 
            Fault_Model_Check = true;
        }
    }
}
