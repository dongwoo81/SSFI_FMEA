using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms.DataVisualization.Charting;

//[2월 21일] 
using Excel = Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;

namespace MDL_Gen_V02
{

    
    //고장분석 정보를 저장하기 위한 구조체 선언
    struct Failure_Sim_Result_INFO
    {
        public double Max_Failure_Value;       // 최대 고장값 크기
        public double AVG_Failure_Value;
        public int Max_Failure_Duration;    // 최대 고장값 유지시간
        public int Failure_NUM;             // 고장 발생 빈도
        public int Severity;
        public int Detection;
        public int Occurrence;
        public int RPN;
        
    }

    // 결함주입 위치의 노드를 저장하기 위한 구조체 
   public struct Position_SET
    {
        public int X_pos;
        public int X_width;
        public int Y_pos;
        public int Y_hight;
    }

    struct Selected_SET
    {
        public String BlockType;
        public String Name;
        public String [] Parent_Block;
        public int depth;
        public bool have_child;
        public int ID_Num;
        public Position_SET Position;

    }

    public struct Block_SET
    {
        public String BlockType;
        public String Name;
        public String Parent_Block;
        public bool have_child;

        public bool Set_Save2DSignal;
        public Position_SET Position;
        public String VariableName;
        public int dst_port_num;
    }

    // 2019-09-10 전체 결함주입 모듈 자동 주입을 위한 tol-level block만 추가하는 구조체
   public struct full_fault_block
    {
        public Block_SET Block_Data;
        public string fault_block_name;
        public string full_path_fault_block_name;
        public bool set_injected;
        public int num_dstport;
    }

    struct Occurence_DB
    {
        public String SFI_Occur_Rate;
        public String Occurence;
    }

    struct Rule_DB
    {
        public string rule_f_time;
        public string rule_range_val;
        public string rule_specify_val;
        public string rule_severity;
        public string rule_sim_time;
        public string rule_type;

        public int time_H;
        public int time_L;

        public int range_H;
        public int range_L;



/*
        public double Max_Failure_Value;       // 최대 고장값 크기
        public int Max_Failure_Duration;    // 최대 고장값 유지시간
        public int Failure_NUM;             // 고장 발생 빈도
        public int Severity;                       // 심각도
        */
    }

    struct Fault_Mode_SET
    {
        public string Block_lib;
        public string Occur_type;
        public string F_enable;
        public string F_disable;
        public string F_duration;
        public string F_value;
    }

     struct SET_Block
    {
        public int fault_block_index;
        public int monitoring_block_index;
    }

    struct STATIS_Fault_SET
    {
        public double[] sti_fault_result_SET;
        public int fault_result_SIZE;
    }

    struct SET_Data_Logging
    {
        public string SignalName;
        public string fullblockpath;
        public int ID;
    }


    struct RULE_RESUET
    {
        public double [] rule_result_set_H;
        public double [] rule_result_set_L;
        public int rule_result_size;

    }

    // SFI 시험을 수행하기 위한 결함주입 시나리오 구조체.
    // SFI_SIMluation form에서 결함주입 시험 조합을 설정
    // Form에서 시험 횟수가 결정되면, 랜덤으로 시험 리스트 생성        
    public struct STATISTICAL_FI_SECNARIO_SET
    {
        public string Block_lib;
        public string Occur_type;
        public string F_enable;
        public string F_disable;
        public string F_duration;
        public string F_value;
        public string Fault_location;

    }


    public partial class Form1 : Form
    {

        // [2월 21일] SFI 완료한 후에, Excel 파일이 생성될 수 있도록 설정하는 플래그 변수
        bool C_complete_SFI = false;

        // 사용자 정의 변수 

        String MDL_File_Path;       // 분석 대상 모델의 파일경로 설정
        StreamReader MDL_File;      // 분석 대상 모델을 읽기 위한 파일 스트림
        StreamReader r_FI_MDL_File; // 결함주입블록과 라인을 추가하기 위한 파일 읽기 스트림
        StreamWriter w_FI_MDL_File; // 결함주입블록과 라인을 추가하기 위한 파일 쓰기 스트림
        StreamReader r_FI_Line_File; // 결함주입블록과 라인을 연결하기 위한 파일 읽기 스트림
        StreamWriter w_FI_Line_File;  // 결함주입블록과 라인을 연결하기 위한 파일 쓰기 스트림

        StreamWriter w_FMEA_File;           // 시험결과 저장 파일

        String Golden_Model_File_Path;              // golden run 시뮬레이션 모델이 위치한 경로
        String Golden_Model_Name;                   // golden run 시뮬레이션 모델 이름
        String Fault_Model_Name;

        // 툴에서 사용할 파일의 경로를 저장하고 있는 문자열
        String golden_file_path;
        String fault_file_path;


        // 파싱 결과를 저장할 Block_SET 배열    
        Block_SET[] Block_DB = new Block_SET[100000];
        int Block_DB_count = 0;

        SET_Block SET_Block_DB = new SET_Block(); //    여러개의 블록을 결함주입 블록 또는 모니터링 블록으로 설정할 경우 배열로 정의 

        // 결함주입 위치를 저장하기 위한 SET 구조체
        Selected_SET Fault_SET;

        // 고장분석 위치를 저장하기 위한 SET 구조체
        Selected_SET Monitoring_SET;

        // 임시 위치를 저장하기 위한 SET 구제초
        Selected_SET Tmep_SET;

        // 결함모델 설정 다이알로그의 설정값을 받아 오기 위한 구조체 선언
        Fault_Mode_SET Fautl_Model_Info;
        bool Fault_Model_Check = false;

        // 상위 수준의 전체 결함주입 시험을 수행하기 위한 배열
        full_fault_block[] full_fault_list = new full_fault_block[100];
        int full_fault_list_count = 0;


        // 파일을 변경하기 위한 문자열 변경 자료 구성


        String fault_module;        // 추가되는 결함블럭
        String ADD_Line;            // 추가되는 라인(결함블록 
        //String s_Modify_Line;         // 변경되는 라인 (dest 쪽이 결함 모듈쪽으로 변경되어야 함)
        //String d_Modify_Line;         // 변경되는 라인 (dest 쪽이 결함 모듈쪽으로 변경되어야 함)

        // Simulink 시뮬레이터 연결 모듈
        MLApp.MLApp MATLAB;
        //MLApp.MLApp SFI_MATLAB;

        // golden run 시뮬레이션 결과를 저장하기 위한 배열
        public double[] golden_result_SET;
        public int golden_result_SIZE;

        // golden run 시뮬레이션 결과를 저장하기 위한 배열
        public double[] fault_result_SET;
        public int fault_result_SIZE;

        int fault_injection_time;

        // 통계적 결함주입 시험결과를 저장하기 위한 배열 (3월 18일 삭제)
        //double[][] sti_fault_resut_SET;
        //int sti_number;
        //int sti_result_size;

        // (3월 18일) 규칙 고장 판정을 위한 (범위) 배열 선언 

        RULE_RESUET[] rule_result_table;



        STATIS_Fault_SET[] statistical_FI_DB;
        //int statistical_num = 0;

        // 결함주입 시뮬레이션 결과 분석을 위한 정상상태, 결함주입 시험 비교 자료 저장 구조체

        Failure_Sim_Result_INFO Failure_INFO;

        Failure_Sim_Result_INFO[] SFI_Failure_INFO;

        // 고장분석을 위한 규칙 데이터 세트

        Rule_DB[] rule_set = new Rule_DB[10];
        int rule_set_num = 0;
        int[] time_table;
        double specific_time_data = 0;          // 고장 분석 2번 유형의 해당 시간 구간 에서의 golden run 산출 결과


        // 발생도를 설정하기 위한 구조체 의 선언

        Occurence_DB[] Occurence_SET = new Occurence_DB[10];
        int Occurence_SET_NUM = 0;

        // 고장 판정에 사용할 검출도, 심각도, 발생도를 판정할 수 있는 자료가 설정되어 있는지를 
        // 확인하는 변수

//        bool SEV_SET = false;
//        bool DEC_SET = false;
//        bool OCC_SET = false;

        // 결함주입 시험결과의 고장모드와 고장영향 문자열을 저장하기 위한 String

        String Failure_MODE;
        String Failure_Port_NUM;            // (3월 14일)
        String Failure_EFFECT;

        // 고장영향 분석을 위한 데이터 수집 기능 변수 및 구조체 (3월 7일)
        int Data_Logging_Size = 0;
        SET_Data_Logging[] S_Data_Logging;
        string SignalLoggingName;

        int Sel_Data_Loggin_ID = 0;


        // (3월 13일) 결함주입 시험결과의 데이터를 총괄하기 위한 변수
        double SUM_failure_data_FMEA = 0;
        double AVG_failure_data_FMEA = 0;
        double MAX_failure_data_FMEA = 0;

        // (2019 10 10) SFI 결함주입 시험을 위한 시나리오 구조체 

        STATISTICAL_FI_SECNARIO_SET[] SFI_secnario_set;
        int SFI_secnario_num = 0;

        int SFI_running_num = 0;        // 현재까지 수행된 시험 횟수


        public Form1()
        {
            InitializeComponent();
            init_Tmep_SET();
            init_Fault_SET();
            init_Monitoring_SET();
            init_Block_SET();


            // 결함주입 속성 값 확인 리스트 박스 초기화
            listView3.Columns.Add("속성", 100);
            listView3.Columns.Add("설정 값", 200);
            listView3.View = View.Details;
            listView3.FullRowSelect = true;
            listView3.GridLines = true;
            String[] arr = new string[2];
            arr[0] = "Fault Block Lib";
            arr[1] = "None";
            ListViewItem lvt = new ListViewItem(arr);
            listView3.Items.Add(lvt);

            arr[0] = "Fault Ocurrence Type";
            arr[1] = "None";
            lvt = new ListViewItem(arr);
            listView3.Items.Add(lvt);

            arr[0] = "Fault Enable Time";
            arr[1] = "None";
            lvt = new ListViewItem(arr);
            listView3.Items.Add(lvt);

            arr[0] = "Fault Disable Time";
            arr[1] = "None";
            lvt = new ListViewItem(arr);
            listView3.Items.Add(lvt);

            arr[0] = "Fault Duration Time";
            arr[1] = "None";
            lvt = new ListViewItem(arr);
            listView3.Items.Add(lvt);

            arr[0] = "Fault Value";
            arr[1] = "None";
            lvt = new ListViewItem(arr);
            listView3.Items.Add(lvt);

            // Matlab interface 생성자 및 초기화
            MATLAB = new MLApp.MLApp();
            MATLAB.Visible = 1;             // command 터미널 감춘다

            // golden run 시뮬레이션 결과를 저장하기 위한 배열 크기 초기화

            golden_result_SET = new double[50000];
            golden_result_SIZE = 0;

            fault_result_SET = new double[50000];
            fault_result_SIZE = 0;


            // number of simulation textbox 초기값 0
            textBox1.Text = "0";
            listView8.FullRowSelect = true;

            ///////////////////////////////////////////////////

            Fautl_Model_Info.Block_lib = "none";

        }

        public void init_Block_SET()
        {
            for (int i = 0; i < 1000; i++)
            {
                Block_DB[i].Set_Save2DSignal = false;

                Block_DB[i].Position.X_pos = 0;
                Block_DB[i].Position.X_width = 0;
                Block_DB[i].Position.Y_pos = 0;
                Block_DB[i].Position.Y_hight = 0;

                Block_DB[i].dst_port_num = 1;
            }


        }

        //SET 구조체를 초기화 하기 위한 함수
        public void init_Tmep_SET()
        {
            Tmep_SET.Parent_Block = new string[100];
            Tmep_SET.depth = 0;

        }

        public void init_Fault_SET()
        {
            Fault_SET.Parent_Block = new string[100];
            Fault_SET.depth = 0;
        }

        public void init_Monitoring_SET()
        {
            Monitoring_SET.Parent_Block = new string[100];
            Monitoring_SET.depth = 0;
        }

        // 파일 열기 다이알로그 창 열기 기능
        public string ShowFileOpenDialog()
        {
            //파일오픈창 생성 및 설정
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "MDL File Open";
            ofd.FileName = "*.mdl";
            ofd.Filter = "MDL 파일 (*.mdl) | *.mdl; | 모든 파일 (*.*) | *.*";

            //파일 오픈창 로드
            DialogResult dr = ofd.ShowDialog();

            //OK버튼 클릭시
            if (dr == DialogResult.OK)
            {
                //File명과 확장자를 가지고 온다.
                string fileName = ofd.SafeFileName;
                Golden_Model_Name = ofd.SafeFileName;
                Golden_Model_Name = Golden_Model_Name.Replace(".mdl", "");
                //File경로와 File명을 모두 가지고 온다.
                string fileFullName = ofd.FileName;
                //File경로만 가지고 온다.
                Golden_Model_File_Path = fileFullName.Replace(fileName, "");

                //File경로 + 파일명 리턴
                return fileFullName;
            }
            //취소버튼 클릭시 또는 ESC키로 파일창을 종료 했을경우
            else if (dr == DialogResult.Cancel)
            {
                return "";
            }

            return "";
        }

        private int find_block(string[] words, int index, int root_depth, string level)
        {
            int block_depth = root_depth;
            bool enabel_Port = false;
            bool enable_Array = false;
            bool enable_Objcet = false;

            int Port_out_depth = 0;
            int Array_out_depth = 0;
            int Object_out_depth = 0;


            for (; index < words.Count(); index++)
            {



                if (words[index] == "{")
                {
                    block_depth++;
                }
                else if (words[index] == "}")
                {
                    block_depth--;

                    if (enabel_Port == true && Port_out_depth == block_depth)
                    { enabel_Port = false; }
                    if (enable_Array == true && Array_out_depth == block_depth)
                    { enable_Array = false; }
                    if (enable_Objcet == true && Object_out_depth == block_depth)
                    { enable_Objcet = false; }

                    //Block_DB_count++;
                    if (block_depth == root_depth)
                    {
                        return index;
                    }

                    //break;
                }
                else if (words[index] == "BlockType")
                {
                    Block_DB[Block_DB_count].BlockType = words[++index];
                    Block_DB[Block_DB_count].Parent_Block = level;

                }
                else if (words[index] == "Name" && enabel_Port == false && enable_Array == false && enable_Objcet == false)
                {

                    index++;

                    // Block의 이름에 공백 문자가 포함 또는 이름이 없는 경우 '"'문자를 사용하여 처리(시작)
                    string name_make = "";
                    for (int n = 0; ; n++)
                    {
                        if (words[index + n][words[index + n].Length - 1] != '\"')        //
                        {

                            name_make += words[index + n];
                            name_make += " ";
                        }
                        else
                        {
                            name_make += words[index + n];
                            index += n;
                            break;
                        }
                    }
                    // Block의 이름에 공백 문자가 포함 또는 이름이 없는 경우 '"'문자를 사용하여 처리(끝)

                    Block_DB[Block_DB_count].Name = name_make;

                    if (name_make.Contains("Dashboard") == true)
                    {
                        name_make = "Dashboard";
                    }


                    //richTextBox1.AppendText(level+ " : " + Block_DB[Block_DB_count].Name + "\n");
                    Block_DB_count++;

                }
                else if (words[index] == "Save2DSignal")
                {
                    Block_DB[Block_DB_count - 1].Set_Save2DSignal = true;
                }
                else if (words[index] == "VariableName")
                {

                    index++;

                    // Block의 이름에 공백 문자가 포함 또는 이름이 없는 경우 '"'문자를 사용하여 처리(시작)
                    string name_make = "";
                    for (int n = 0; ; n++)
                    {
                        if (words[index + n][words[index + n].Length - 1] != '\"')        //
                        {

                            name_make += words[index + n];
                            name_make += " ";
                        }
                        else
                        {
                            name_make += words[index + n];
                            index += n;
                            break;
                        }
                    }
                    // Block의 이름에 공백 문자가 포함 또는 이름이 없는 경우 '"'문자를 사용하여 처리(끝)
                    name_make = name_make.Replace("\"", "");
                    Block_DB[Block_DB_count - 1].Name = name_make;

                }
                else if (words[index] == "System")
                {
                    string temp_level = Block_DB[Block_DB_count - 1].Parent_Block;
                    temp_level += "@";
                    temp_level += Block_DB[Block_DB_count - 1].Name;        // 문자열을 구분하기 위한 히든문자열 "@@"

                    Block_DB[Block_DB_count - 1].have_child = true;             // 해당 블록은 subsystem 임

                    // 서브 시스템이 있는 경우 재귀 호출로 처리함(시작)
                    for (int index_block = index; index_block < words.Count(); index_block++)
                    {
                        // Version 3 임시 루틴
                        if (index_block + 1 == words.Count())
                        {
                            index = index_block;
                        }



                        if (words[index_block] == "{" || words[index_block].Contains("{") == true)      // 3월 4일
                        {
                            block_depth++;
                        }
                        else if (words[index_block] == "Block")         // MDL 파일의 Block 구문 시작 ( 일반블록과 subblock으로 구분됨 
                        {

                            index_block = find_block(words, index_block, block_depth, temp_level);
                            // 재귀함수의 종료 조건
                            if (Block_DB_count == 221)
                            {

                            }

                            //richTextBox1.AppendText(words[index_block]+ "\n");
                        }
                        else if (words[index_block] == "}" || words[index_block].Contains("}") == true)      // 3월 4일
                        {
                            block_depth--;
                            //Block_DB_count++;
                            if (block_depth == root_depth)
                            {
                                return index_block;
                            }
                        }
                        else
                        {

                        }

                        // Version 3 임시 루틴
                        //if (index_block+1 == words.Count())
                        //{
                        //    index = index_block;
                        //}
                    }

                    // 서브 시스템이 있는 경우 재귀 호출로 처리함(끝)


                }
                else if (words[index] == "Port")
                {
                    if (enabel_Port == false)
                    {
                        enabel_Port = true;
                        Port_out_depth = block_depth;
                    }
                }
                else if (words[index] == "Object")   // object 안에 또다른 object 가 있을 수 있으며,
                {                                   // 이때 object의 이름이 마지막에 들어갈 수도 있음.
                    if (enable_Objcet == false)
                    {
                        enable_Objcet = true;
                        Object_out_depth = block_depth;
                    }

                }
                else if (words[index] == "Array")
                {
                    if (enable_Array == false)
                    {
                        enable_Array = true;
                        Array_out_depth = block_depth;
                    }
                }
                else if(words[index] == "Ports")
                {
                    char[] delimiter_tmp = { '[', ']', ',' };
                    string str_port;

                    index++;
                    str_port = words[index];

                    string[] port_temp = str_port.ToString().Split(delimiter_tmp);
                    if(port_temp[1] != "")
                        Block_DB[Block_DB_count - 1].dst_port_num = Convert.ToInt32(port_temp[1]);
                }
                else if (words[index] == "Position")
                {
                    // 2019-09-10 full node inject


                    // 문자열 구분자 생성
                    char[] delimiter_tmp = { '[', ']', ',' };
                    string str_position;
                    int position_words_index = 0;
                    string[] position_words = new string[4];

                    for (int i = 0; i < 4; i++)
                    {
                        index++;
                        str_position = words[index];

                        string[] position_temp = str_position.ToString().Split(delimiter_tmp); // 선택된 파일의 모든 워드를 구분한 문자열 배열을 생성
                        //position_words = new string[position_temp.Count()];

                        for (int k = 0; k < position_temp.Count(); k++)
                        {
                            if (position_temp[k] != "")
                            {
                                position_words[position_words_index++] = position_temp[k];
                            }
                        }

                    }

                    Block_DB[Block_DB_count - 1].Position.X_pos = Convert.ToInt32(position_words[0]);
                    Block_DB[Block_DB_count - 1].Position.Y_pos = Convert.ToInt32(position_words[1]);
                    Block_DB[Block_DB_count - 1].Position.X_width = Convert.ToInt32(position_words[2]);
                    Block_DB[Block_DB_count - 1].Position.Y_hight = Convert.ToInt32(position_words[3]);

                }
                else
                {

                }

            }


            return index;
        }

        
        private void button2_Click(object sender, EventArgs e)
        {


            MDL_File_Path = ShowFileOpenDialog();               // 파일 오픈 다이알로그 생성: 선택된 파일의 경로와 이름 
            golden_file_path = MDL_File_Path;
            MDL_File = new StreamReader(MDL_File_Path);         // 선택된 파일을 읽기용 스트림으로 생성

            // 문자열 구분자 생성
            char[] delimiterChars = { ' ', '\t', '\n' };
            string str = MDL_File.ReadToEnd();

            string[] words_temp = str.ToString().Split(delimiterChars); // 선택된 파일의 모든 워드를 구분한 문자열 배열을 생성

            // 구문 정리한 파일의  불필요한 문자를 제거하기 위한 구문 ("") (시작)
            string[] words = new string[words_temp.Count()];
            int word_size = 0;

            //int word_size_t = 0; 
            for (int i = 0; i < words.Count(); i++)
            {
                if (words_temp[i] != "")
                {
                    words[word_size++] = words_temp[i];

                }
            }
            Array.Resize(ref words, word_size - 1);

            // 구문 정리한 파일의  불필요한 문자를 제거하기 위한 구문 (끝)


            int block_depth = 0;        // { 문자의 단계 depth가 0이 되면 파싱 종료

            int t_Data_Logging_cur = 0;     // Data Logging 개수를 확인하기 위한 임시변수

            // 모델의 구조를 파싱하기 위해, 파일의 모든 구문을 탐색한다.
            for (int index_system = 0; index_system < word_size - 1; index_system++)
            {
                //taget 모델의 고장분석 대상을 확인하기 위한 방법
                // 1. NumTestPointedSignals을 확인한다,( 0 이면, 분석대상 설정이 필요합니다)

                // 2. NumTestPointedSignals숫자만큼 데이터를 저장할 수 있는 고장분석 자료 구조 선언

                // 3. TestPointedSignal 을 탐색한 후, SignalName을 저장하여 UI에 출력한다.

                // 4. Simulink.DataIOCC 을 탐색 한후, SignalLoggingName의 값을 확인 한다.

                // 5. golden run 시뮬레이션을 수행한 후,  SignalLoggingName을 실행하여, 테이블 값을 확인하여
                //      각각의 TestPointedSignal의 아이디 값으 확인한다.

                // 6. TestPointedSignal{1}.Values.Data 를 출력하여, 시뮬레이션 결과를 확인 할 수있음. 
                // 각 TestPointedSignal의 ID 값을 {x} 로 확인 할 수 있음

                // 1.
                if (words[index_system] == "NumTestPointedSignals")
                {
                    index_system++;

                    int t_pointed_size = Int32.Parse(words[index_system]);

                    if (t_pointed_size > 0)
                    {
                        Data_Logging_Size = t_pointed_size;

                        // 2.
                        S_Data_Logging = new SET_Data_Logging[Data_Logging_Size];
                    }
                    else
                    {
                        MessageBox.Show("고장영향 분석을 위한 Data Loggin 지정이 필요합니다.");
                        return;
                    }

                }

                // 3.
                if (words[index_system] == "TestPointedSignal")
                {
                    while (true)
                    {
                        index_system++;

                        if (words[index_system] == "}")
                        {
                            t_Data_Logging_cur++;
                            break;
                        }
                        else
                        {
                            if (words[index_system] == "SignalName")
                            {
                                // SignalName 이 두 단어 이상인 경우의 처리 " " 문자를 식별한다
                                string dump_signalname = words[++index_system];
                                int num_comma =0;

                                for (int k = 0;  dump_signalname.Length > k; k++)
                                {
                                    if (dump_signalname[k] == '"')
                                        num_comma++;
                                }
                                
                                if(num_comma == 1)
                                {
                                    while(true)
                                    {
                                        if (words[index_system].Contains('"') == true)
                                        {
                                            dump_signalname += " ";
                                            dump_signalname += words[++index_system];
                                            break;
                                        }
                                        else
                                        {
                                            dump_signalname += " ";
                                            dump_signalname += words[++index_system];
                                        }
                                    }
                                }
                                

                                                                                                                          
                                S_Data_Logging[t_Data_Logging_cur].SignalName = dump_signalname;
                                S_Data_Logging[t_Data_Logging_cur].SignalName = S_Data_Logging[t_Data_Logging_cur].SignalName.Replace("\"", "");

                            }
                            else if (words[index_system] == "FullBlockPath")
                            {
                                // SignalName 이 두 단어 이상인 경우의 처리 " " 문자를 식별한다
                                string dump_signalname = words[++index_system];
                                int num_comma = 0;

                                for (int k = 0; dump_signalname.Length > k; k++)
                                {
                                    if (dump_signalname[k] == '"')
                                        num_comma++;
                                }

                                if (num_comma == 1)
                                {
                                    while (true)
                                    {
                                        if (words[index_system].Contains('"') == true)
                                        {
                                            dump_signalname += " ";
                                            dump_signalname += words[++index_system];
                                            break;
                                        }
                                        else
                                        {
                                            dump_signalname += " ";
                                            dump_signalname += words[++index_system];
                                        }
                                    }
                                }

                                S_Data_Logging[t_Data_Logging_cur].fullblockpath = dump_signalname;
                                S_Data_Logging[t_Data_Logging_cur].fullblockpath = S_Data_Logging[t_Data_Logging_cur].fullblockpath.Replace("\"", "");
                            }
                            else
                            {


                            }
                        }


                    }
                }

                // 4.

                if (words[index_system] == "Simulink.DataIOCC")
                {
                    while (true)
                    {
                        index_system++;

                        if (words[index_system] == "}")
                        {
                            break;
                        }
                        else
                        {
                            if (words[index_system] == "SignalLoggingName")
                            {
                                SignalLoggingName = words[++index_system];
                            }
                            else
                            {

                            }
                        }


                    }
                }

                //5 6은 golden simulation을 수행하는 버튼 동작에서 처리함 !!!!


                /////////////////////////////////////////////////////////////////////////////////////////////////////

                if (words[index_system] == "System" && words[index_system + 1] == "{")             // MDL 파일의 Top System 구문이 시작되는 index =  index_system
                {
                    for (int index_block = index_system; index_block < words.Count(); index_block++)
                    {
                        if (words[index_block] == "{" || words[index_block].Contains("{") == true)      // 3월 4일
                        {
                            block_depth++;
                        }
                        else if (words[index_block] == "Block")         // MDL 파일의 Block 구문 시작 ( 일반블록과 subblock으로 구분됨 
                        {

                            index_block = find_block(words, index_block, block_depth, "TOP LEVEL");
                            //richTextBox1.AppendText(words[index_block]+ "\n"); 

                        }
                        else if (words[index_block] == "}" || words[index_block].Contains("}") == true)      // 3월 4일
                        {

                        }
                        else
                        {

                        }
                    }

                    break;
                }
            }

            // top level의 block들을 추가함
            for (int i = 0; i < Block_DB_count; i++)
            {
                if (Block_DB[i].Parent_Block == "TOP LEVEL")
                {
                    treeView1.Nodes.Add(Block_DB[i].Parent_Block + "@" + Block_DB[i].Name, Block_DB[i].Name);

                }
                else
                {
                    //treeView1.Nodes.Find(Block_DB[i].Parent_Block, true)[1].No
                    /*
                    if (treeView1.Nodes.ContainsKey(Block_DB[i].Parent_Block) == false)
                    {
                        //treeView1.Nodes.Add(Block_DB[i].Parent_Block + Block_DB[i-1].Name, Block_DB[i].Name);
                        
                        //treeView1.Nodes.Add(Block_DB[i].Parent_Block, Block_DB[i].Name);
                        treeView1.Nodes.Find(Block_DB[i-1].Parent_Block, true)[0].Nodes.Add(Block_DB[i].Parent_Block, Block_DB[i].Name);
                    }
                    else
                    */

                    if (Block_DB[i].have_child == true)
                    {
                        treeView1.Nodes.Find(Block_DB[i].Parent_Block, true)[0].Nodes.Add(Block_DB[i].Parent_Block + "@" + Block_DB[i].Name, Block_DB[i].Name);
                    }
                    else
                    {

                        treeView1.Nodes.Find(Block_DB[i].Parent_Block, true)[0].Nodes.Add(Block_DB[i].Parent_Block, Block_DB[i].Name);
                    }
                }
            }
        }

        private void Generation_Fault_Block()
        {
            fault_module = "	Block {\n";
            fault_module += "		BlockType		      Reference\n";
            fault_module += "		Name		      \"F_line_block\"\n";                  // 결함모델이 여려개 있다면 수정됨
            fault_module += "		SID		      \"30\"\n";                                // 블록의 개수에 따라 SID 가 설정됨
            fault_module += "		Ports		      [1, 1]\n";
            fault_module += "		Position		      [195, 100, 30, 30]\n";          // 위치 조정이 필요함
            fault_module += "		ZOrder		      33\n";                                // 수정 될 수 있음
            fault_module += "		LibraryVersion	      \"1.1\"\n";
            fault_module += "		SourceBlock	      \"F_Line_block/Subsystem\"\n";
            fault_module += "		SourceType	      \"SubSystem\"\n";
            fault_module += "		ContentPreviewEnabled   off\n";
            fault_module += "    }\n";

        }

        private void Generation_Fault_Block(string name, string SID, string a, string b, string c, string d)
        {
            fault_module = "	Block {\n";
            fault_module += "		BlockType		      Reference\n";
            fault_module += "		Name		      \"" + name +  "\"\n";                  // 결함모델이 여려개 있다면 수정됨
            fault_module += "		SID		      \" "+ SID + "\"\n";                                // 블록의 개수에 따라 SID 가 설정됨
            fault_module += "		Ports		      [1, 1]\n";
            fault_module += "		Position		      [" + a +", " + b + ", "+ c +", "+ d + "]\n";          // 위치 조정이 필요함
            fault_module += "		ZOrder		      33\n";                                // 수정 될 수 있음
            fault_module += "		LibraryVersion	      \"1.1\"\n";
            fault_module += "		SourceBlock	      \"F_Line_block/Subsystem\"\n";
            fault_module += "		SourceType	      \"SubSystem\"\n";
            fault_module += "		ContentPreviewEnabled   off\n";
            fault_module += "    }\n";

        }

        private void Generation_Fault_Line()
        {
            ADD_Line = "    Line {\n";
            ADD_Line += "      ZOrder		      999\n";                        // 라인의 개수에 따라 변경될 수 있음
            ADD_Line += "      SrcBlock		      \"F_line_block\"\n";
            ADD_Line += "      SrcPort		      1\n";
            ADD_Line += "      Points		      [115, 0; 0, 15]\n";
            ADD_Line += "      DstBlock		      " + Fault_SET.Name + "\n";                  // 리스트에 선택된 Line의 dstBlock 위치의 값이 들어간다. 
            ADD_Line += "      DstPort		      " + Failure_Port_NUM + "\n";
            ADD_Line += "    }\n";
        }

        private void Generation_Fault_Line(string srcBlock, string desBlock, string desport)
        {
            ADD_Line = "    Line {\n";
            ADD_Line += "      ZOrder		      999\n";                        // 라인의 개수에 따라 변경될 수 있음
            if (desport == "1")
            { ADD_Line += "      SrcBlock		      \"" + srcBlock + "\"\n"; }
            else
            { ADD_Line += "      SrcBlock		      \"" + srcBlock + "_" + desport + "\"\n"; }
            ADD_Line += "      SrcPort		      1\n";
            ADD_Line += "      DstBlock		      \"" + desBlock + "\"\n";                  // 리스트에 선택된 Line의 dstBlock 위치의 값이 들어간다. 
            ADD_Line += "      DstPort		   " + desport + "\n";
            ADD_Line += "    }\n";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //string command = "";
            string str;

            // (2월 20일) 서브시스템의 이름을 추적하기 위한 변수
            string[] str_subsystem = new string[30];
            int count_subsystem = 0;


            // 결함 모델을 반영한 Simulink 모델 파일을 생성한다.
            Generation_Fault_Block();
            Generation_Fault_Line();

            // 결함 블록과 라인의 추가 위치를 탐색하기 위한 system_depth 변수
            //int system_depth = 1;

            // 파일을 저장하기 위한 다이알로그를 생성
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "MDL 파일 (*.mdl) | *.mdl; | 모든 파일 (*.*) | *.*";
            saveFileDialog1.Title = "MDL File Save";
            saveFileDialog1.ShowDialog();



            //  다이알로그에서 저장할 파일명이 입력 되었음 (MDL 파일 SAVE 파일명이 null이 아니면...)
            if (saveFileDialog1.FileName != "")
            {
                //Fault_Model_Name;


                // 생성할 결함주입용 simulink 모델의 파일경로를 저장함
                fault_file_path = saveFileDialog1.FileName;
                string t_fault_file_path = fault_file_path;

                // 결함주입용 Simulink 모델의 파일이름을 저장함.
                char[] delimiterChars = { '\\' };
                string[] t_words = fault_file_path.Split(delimiterChars);
                Fault_Model_Name = t_words[t_words.Length - 1];
                fault_file_path = fault_file_path.Replace(Fault_Model_Name, "");
                Fault_Model_Name = Fault_Model_Name.Replace(".mdl", "");

                // 생성된 파일의 원본 자료를 copy하여, 모델파일을 생성한다
                System.IO.File.Copy(golden_file_path, t_fault_file_path);

                // 복사된 파일에 수정할 위치를 찾기 위해, 일단 읽기용으로 파일을 연다
                r_FI_MDL_File = new StreamReader(t_fault_file_path);

                // 파일에서 수정 위치를 찾고, 문자열을 추가하기 위한 StringBuilder 객체 사용
                var sb = new StringBuilder();

                // 파일을 라인별로 읽어서 고장주입 블록이 추가될 subsystem 을 찾는다.
                // 고장주입 블록추가될 위치가 확인되면,, loop 문을 빠져 나온다.
                int depth_find = Fault_SET.depth;
                bool find_loc = false;

                // 수정 코드
                int _depth = 0;
                int subsystem_depth = 0;
                string[] subsystem_name = new string[100];
                bool system_check = false;
                bool start_check = false;
                int[] depth_count = new int[20];
                int depth_count_num = 1;
                bool line_find = false;
                int line_depth = 1;
                // string high_level_block ="";
                //bool p_block_check = false;
                //int p_block_check_depth = 0;

                while ((str = r_FI_MDL_File.ReadLine()) != null)
                {
                    if (start_check == false)
                    {
                        if (str.Contains("System") == true && str.Contains("{"))
                        {
                            start_check = true;
                            system_check = true;
                        }
                    }
                    else if (line_find == false && start_check == true)
                    {
                        if (str.Contains("System") == true && str.Contains("{"))
                        {
                            system_check = true;
                            _depth++;

                        }
                        else if (str.Contains("Name") == true && system_check == true)
                        {
                            system_check = false;
                            depth_count[depth_count_num++] = _depth;
                            subsystem_depth++;

                            // [2월 20일] S//
                            str_subsystem[count_subsystem++] = str;
                            // [2월 20일] E//


                            // [2월 20일] S//
                            // [2월 20일] E//


                        }
                        else if (str.Contains("{") == true)
                        {
                            _depth++;
                        }
                        else if (str.Contains("}") == true)
                        {
                            if (depth_count_num == 0)
                            {

                            }
                            else if (depth_count[depth_count_num - 1] == _depth)
                            {
                                depth_count_num--;
                                subsystem_depth--;

                                // [2월 20일] S//
                                count_subsystem--;
                                // [2월 20일] E//

                            }
                            _depth--;

                        }
                        else
                        {

                        }

                        richTextBox1.AppendText(str);
                        richTextBox1.AppendText("\n");


                        if (str.Contains("Gain") == true)
                        {
                            //MessageBox.Show("SUM");
                        }

                        // 결함주입 대상 블록이 Level 1(즉 최상의 블록 중 하나)인 경우
                        if (str.Contains("Name") == true && str.Contains(Fault_SET.Name)
                            && Fault_SET.depth == 1 && (subsystem_depth) == Fault_SET.depth)
                        {
                            richTextBox1.AppendText(str);
                            richTextBox1.AppendText("\n");
                            find_loc = true;
                            line_find = true;
                        }

                        // 결함주입 대상 블록이 Level 2이하(subsystem) 내의 블록인 경우
                        if (str.Contains("Name") == true && str.Contains(Fault_SET.Name)
                            && (subsystem_depth) == Fault_SET.depth                                             ////// 3월 4일 확인 필수 !!!!
                            && str_subsystem[count_subsystem - 1].Contains(Fault_SET.Parent_Block[0]) == true)
                        {
                            // 결함을 주입할 블록의 위치는 찾았다
                            richTextBox1.AppendText(str);
                            richTextBox1.AppendText("\n");
                            find_loc = true;
                            line_find = true;
                            //break;
                        }

                    }
                    else if (line_find == true && start_check == true)
                    {
                        int kkk;
                        /*
                        if(str.Contains("{{") == true)
                        {
                            string[] StringArray = str.Split(new string[] { "{" }, StringSplitOptions.None);
                            kkk = StringArray.Length - 1;
                        }
                        */


                        if (str.Contains("Line") == true && line_depth == 0)
                        {
                            break;
                        }

                        if (str.Contains("{") == true)
                        {
                            string[] StringArray = str.Split(new string[] { "{" }, StringSplitOptions.None);
                            kkk = StringArray.Length - 1;
                            if (kkk != 0)
                                line_depth += kkk;
                            else
                                line_depth++;
                        }

                        if (str.Contains("}") == true)
                        {
                            string[] StringArray = str.Split(new string[] { "}" }, StringSplitOptions.None);
                            kkk = StringArray.Length - 1;
                            if (kkk != 0)
                                line_depth -= kkk;
                            else
                                line_depth--;
                        }

                        //else
                        //{
                        //   richTextBox1.AppendText(str);
                        //   richTextBox1.AppendText("\n");
                        //}


                    }

                    sb.Append(str); // 앞부분을 모두 저장
                    sb.Append("\n");

                }


                // 고장주입블록이 추가될 위치에다가 고장블록 추가
                sb.Append(fault_module);

                // 고장주입블록 밑에 고장블록 라인을 추가
                sb.Append(ADD_Line);

                // 파일에 남겨진 다른 문자열을 모두 추가한다.
                sb.Append("    Line {\n");            // 추가하는 문자열
                sb.Append(r_FI_MDL_File.ReadToEnd()); // 뒤부분 모두 저장

                // 완료된 문자열을 저장한다.(고장블록과 고장블록 연결 라인이 모두 추가되었다)
                sb.Append(r_FI_MDL_File.ReadToEnd()); // 뒤부분 모두 저장
                r_FI_MDL_File.Close();
                w_FI_MDL_File = new StreamWriter(t_fault_file_path);
                w_FI_MDL_File.Write(sb.ToString());     // 문자열을 추가한다.

                // 결함주입 모듈과 line이 추가된 모델을 저장함
                w_FI_MDL_File.Close();

                // 2. Simulink 모델의 라인연결 설정을 위한 쓰기용 파일을 읽는다.

                r_FI_Line_File = new StreamReader(t_fault_file_path);
                var mody_sb = new StringBuilder();
                bool F_line_set = false;        // 추가된 결함주입 라인의 detblock을 수정하지 않기 위한 신호

                //               if (Failure_Port_NUM == "1")
                //               {

                while ((str = r_FI_Line_File.ReadLine()) != null)
                {
                    if (str.Contains("F_line_block") == true)
                    {
                        mody_sb.Append(str); // 앞부분을 모두 저장
                        mody_sb.Append("\n");
                        break;
                    }

                    mody_sb.Append(str); // 앞부분을 모두 저장
                    mody_sb.Append("\n");
                }


                string f_dstblock = "      DstBlock		      \"F_line_block\"\n"; ;
                string temp_dstblock = "";
                bool dstblock_set = false;
                while ((str = r_FI_Line_File.ReadLine()) != null)
                {
                    if (str.Contains("SrcBlock") == true)
                    {
                        if (str.Contains("F_line_block") == true)
                            F_line_set = true;
                        else F_line_set = false;
                    }

                    // 190118 수정 : DstPort가 여러개인 경우 일단 1번으로 통일하고,
                    // 차후에 옵션을 주어서 사용자가 1,2, ..n 선택하면, 선택된 DstPort를 수정할 수있도록 한다.
                    if (dstblock_set == true && str.Contains("DstPort") == true && str.Contains(Failure_Port_NUM) == true)
                    {
                        //현재는 무조건 해당 코드 수행 (dstport 1일때)
                        mody_sb.Append(f_dstblock); // 앞부분을 모두 저장
                        mody_sb.Append("\n");

                        if (Failure_Port_NUM != "1") mody_sb.Append("      DstPort		    1"); // 앞부분을 모두 저장
                        else mody_sb.Append(str); // 앞부분을 모두 저장

                        mody_sb.Append("\n");
                        break;

                    }

                    if (dstblock_set == true && str.Contains("DstPort") == true && str.Contains(Failure_Port_NUM) != true)
                    {
                        //현재는 무조건 해당 코드 수행 (dstport 1이 이닐때)
                        mody_sb.Append(temp_dstblock); // 앞부분을 모두 저장
                        mody_sb.Append("\n");
                        mody_sb.Append(str); // 앞부분을 모두 저장
                        mody_sb.Append("\n");
                        F_line_set = false;
                        dstblock_set = false;


                    }
                    else if (str.Contains("DstBlock") == true && str.Contains(Fault_SET.Name) == true && F_line_set == false)
                    {
                        temp_dstblock = str;
                        dstblock_set = true;
                        //break;


                    }
                    else
                    {
                        mody_sb.Append(str); // 앞부분을 모두 저장
                        mody_sb.Append("\n");

                    }

                }


                mody_sb.Append(r_FI_Line_File.ReadToEnd());
                r_FI_Line_File.Close();

                // 저장 끝
                w_FI_Line_File = new StreamWriter(t_fault_file_path);

                w_FI_Line_File.Write(mody_sb.ToString());     // 문자열을 추가한다.


                // 결함주입 모듈과 line이 추가된 모델을 저장함
                w_FI_Line_File.Close();

                /////////////////////////////////////////////////////////////////////////////////////////////

                //
                //MessageBox.Show(saveFileDialog1.FileName);
            }


        }


        private void ADD_Failure_Mode_Function(string str1, string str2)
        {
            Failure_MODE = str1;
            Failure_Port_NUM = str2;
        }


        private void ADD_Failure_Effect_Function(string str)
        {
            Failure_EFFECT = str;
        }

        string selected_tree_node;          // TreeView에서 오른쪽 클릭으로 선택된 block이름
        string selected_tree_prenode;       // TreeView에서 오른쪽 클릭으로 선택된 block의 parent block 이름

        void MenuClick(object obj, EventArgs ea)
        {
            MenuItem mI = (MenuItem)obj;
            String str = mI.Text;

            string cmd;


            // 선택된 TreeView node block의 이름으로 Block DB 배열에서 찾는다.
            // block과 parent block 모두가 동일한 block을 찾아야 함(현재까지는 2레벨 까지의 정확성
            int index_node = 0;

            for (; index_node <= Block_DB_count; index_node++)
            {
                if ((Block_DB[index_node].Name == selected_tree_node) && (Block_DB[index_node].Parent_Block == selected_tree_prenode))
                {
                    break;
                }
            }

            if (str == "결함주입 위치")
            {

                Create_failure_mode dlg = new Create_failure_mode();

                dlg.FormFailureModeADDEvent += new Create_failure_mode.FormSendDataHandler(ADD_Failure_Mode_Function);

                dlg.ShowDialog();

                Fault_SET = Tmep_SET;
                //string[] strs = new string[] { Block_DB[index_node].BlockType, Block_DB[index_node].Name };
                string[] strs = new string[] { Failure_MODE, Fault_SET.Name };
                ListViewItem lvi = new ListViewItem(strs);
                listView1.Items.Add(lvi);



                SET_Block_DB.fault_block_index = index_node;
                //MessageBox.Show("결함주입 위치를 선정합니다.");
            }
            if (str == "고장분석 위치")
            {
                Create_failure_effect dlg = new Create_failure_effect();

                dlg.FormFailureEeffectADDEvent += new Create_failure_effect.FormSendDataHandler(ADD_Failure_Effect_Function);

                dlg.ShowDialog();

                Monitoring_SET = Tmep_SET;
                Monitoring_SET.ID_Num = Sel_Data_Loggin_ID;

                //string[] strs = new string[] { Block_DB[index_node].BlockType, Block_DB[index_node].Name };
                string[] strs = new string[] { Failure_EFFECT, Monitoring_SET.Name };
                ListViewItem lvi = new ListViewItem(strs);
                listView2.Items.Add(lvi);

                SET_Block_DB.monitoring_block_index = index_node;

                // (3월 13일) 고장분석 위치의 golden data를 저장한다.
                if (Sel_Data_Loggin_ID == 0)
                {
                    MessageBox.Show("데이터 확인 노드가 선택되지 않았습니다.");
                    return;
                }

                cmd = SignalLoggingName + "{" + Sel_Data_Loggin_ID.ToString() + "}.Values.Data";       // sldemo_autotrans_output{0}.Values.Data
                string sim_result = MATLAB.Execute(cmd);

                // 문자열 구분자 생성
                char[] delimiterChars = { ' ', '\t', '\n' };

                string[] refind_sim_result = sim_result.ToString().Split(delimiterChars); // 선택된 파일의 모든 워드를 구분한 문자열 배열을 생성

                // 구문 정리한 파일의  불필요한 문자를 제거하기 위한 구문 ("") (시작)
                string[] c_refind_sim_result = new string[refind_sim_result.Count()];
                int word_size = 0;
                for (int i = 0; i < refind_sim_result.Count(); i++)
                {
                    if (refind_sim_result[i] != "")
                    {
                        c_refind_sim_result[word_size++] = refind_sim_result[i];
                    }
                }

                // 데이터를 문자열에서 실수형으로 변환  // 3월 08일


                int start_sim = 0;
                int final_sim_size = 0;
                bool set_product = false;
                double setvalue = 0.0;

                if (c_refind_sim_result[3] == "*")
                {
                    setvalue = Double.Parse(c_refind_sim_result[2]);
                    start_sim = 4;
                    set_product = true;
                }
                else if (c_refind_sim_result[3] == "single")
                {
                    start_sim = 6;
                }
                else
                {
                    start_sim = 2;
                }

                golden_result_SIZE = word_size;

                for (int i = start_sim; i < golden_result_SIZE; i++)
                {
                    if (set_product == true)
                    {
                        golden_result_SET[final_sim_size++] = Double.Parse(c_refind_sim_result[i]) * setvalue;
                    }
                    else
                    {
                        golden_result_SET[final_sim_size++] = Double.Parse(c_refind_sim_result[i]);
                    }
                }




                //MessageBox.Show("고장분석 위치를 선정합니다");
            }
            if (str == "상세정보")
            {
                //MessageBox.Show("Block의 상세정보를 보여줌-Silink call");
            }
            if (str == "출력결과 확인")       // 3월08일
            {
                if (Sel_Data_Loggin_ID == 0)
                {
                    MessageBox.Show("데이터 확인 노드가 선택되지 않았습니다.");
                    return;
                }

                cmd = SignalLoggingName + "{" + Sel_Data_Loggin_ID.ToString() + "}.Values.Data";       // sldemo_autotrans_output{0}.Values.Data
                string sim_result = MATLAB.Execute(cmd);

                // 문자열 구분자 생성
                char[] delimiterChars = { ' ', '\t', '\n' };

                string[] refind_sim_result = sim_result.ToString().Split(delimiterChars); // 선택된 파일의 모든 워드를 구분한 문자열 배열을 생성

                // 구문 정리한 파일의  불필요한 문자를 제거하기 위한 구문 ("") (시작)
                string[] c_refind_sim_result = new string[refind_sim_result.Count()];
                int word_size = 0;
                for (int i = 0; i < refind_sim_result.Count(); i++)
                {
                    if (refind_sim_result[i] != "")
                    {
                        c_refind_sim_result[word_size++] = refind_sim_result[i];
                    }
                }

                // 데이터를 문자열에서 실수형으로 변환  // 3월 08일

                double[] final_sim_result = new double[word_size];
                int start_sim = 0;
                int final_sim_size = 0;
                bool set_product = false;
                double setvalue = 0.0;

                // 시뮬레이션 출력결과에 따른 파싱 방법을 정규화 해야 함(미해결) (2019-11-06)
                if (c_refind_sim_result[3] == "*")
                {
                    setvalue = Double.Parse(c_refind_sim_result[2]);
                    start_sim = 4;
                    set_product = true;
                }
                else if (c_refind_sim_result[3] == "single")
                {
                    start_sim = 6;
                }
                else
                {
                    start_sim = 2;
                }


                for (int i = start_sim; i < word_size; i++)
                {
                    if (set_product == true)
                    {
                        final_sim_result[final_sim_size++] = Double.Parse(c_refind_sim_result[i]) * setvalue;
                    }
                    else
                    {
                        final_sim_result[final_sim_size++] = Double.Parse(c_refind_sim_result[i]);
                    }
                }

                // 실수형 데이터를 그래프 객체에 출력한다.
                // chart 에 시뮬레이션 결과를 그려준다.
                chart1.Series[0].Points.Clear();

                chart1.Series[0].ChartType = SeriesChartType.Spline;

                for (int i = 0; i < final_sim_size; i++)
                {
                    //chart1.Series[0].Points.AddXY(i, golden_result_SET[i] * golden_result_SET[0]);
                    chart1.Series[0].Points.AddXY((double)i, final_sim_result[i]);
                }

            }
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {

                treeView1.SelectedNode = e.Node;
                selected_tree_node = treeView1.SelectedNode.Text;

                Tmep_SET.Name = treeView1.SelectedNode.Text;


                if (treeView1.SelectedNode.Parent == null)
                {
                    selected_tree_prenode = "Top Level";
                }
                else
                {
                    selected_tree_prenode = treeView1.SelectedNode.Parent.Text;
                }




                // 트리노드에서 선택된 위치의 블록 계층 구조를 탐색하여 저장한다.

                // 선택된 tree node가 top 노드인 경우에는
                bool tree_node_1_level = false;
                if (treeView1.SelectedNode.Parent == null)
                {
                    Tmep_SET.depth = 0;
                    Tmep_SET.Parent_Block[Tmep_SET.depth] = treeView1.SelectedNode.Text;
                    Tmep_SET.depth++;
                    tree_node_1_level = true;
                }
                else
                {
                    // 선택된 tree node 가 Top 노드가 아닌 경우에는 .. 다음과 같다
                    Tmep_SET.depth = 1;
                    for (int i = treeView1.SelectedNode.Parent.Level, j = 0; i >= 0; i--, j++)        // 트리뷰에서 선택된 노드의 상위 구조를 탐색하기 위한 루프
                    {
                        Tmep_SET.Parent_Block[j] = treeView1.SelectedNode.Parent.Text; // 부모노드의 내용을 확인 한 수
                        Tmep_SET.depth++;

                        //richTextBox1.AppendText(treeView1.SelectedNode.Parent.Text + "\n");
                        treeView1.SelectedNode = treeView1.SelectedNode.Parent;                     // 현재 노드를 부모노드(상위) 로 이동함
                    }                                                                               // 상위노드로 더 올라갈수 없다면, 루프 종료    
                }
                // Block DB의 ID를 찾는다.
                ////////////////////////////////////////////////////////////////////////////////////////////////////////
                for (int k = 0; k < Block_DB_count; k++)
                {
                    char[] delimiterChars = { '@' };

                    //파싱된 문자열을 temp  문자열과 비교하여 위치를 탐색한다.
                    string[] words = Block_DB[k].Parent_Block.Split(delimiterChars);

                    bool check_inside_p = false;

                    // Block_DB에 저장되어 있는 노드 정보와 선택된 트리 노드가 일치하는 노드를 찾는다.
                    for (int i = 1, j = Tmep_SET.depth - 1; j >= 0; i++, j--)
                    {
                        //richTextBox1.AppendText(words[i].ToString() + "\n");
                        if (words.Length == 1 && tree_node_1_level == false)      // top level 만 있는 경우 break.
                            break;
                        else if (tree_node_1_level == true)
                        {
                            if (Tmep_SET.Name == Block_DB[k].Name) check_inside_p = true;
                            break;
                        }

                        if (words.Length - 1 != Tmep_SET.depth) break;    // Block노드의 트리 노드의 depth가 일치하지 않는 경우, 비교할 필요 없음


                        if (Tmep_SET.Parent_Block[j] == words[i].ToString())    // 각 depth의 신호를 검사
                        {

                            if (j == 0)
                            {                               // 모든 depth의 노드는 일치하며, 마지막으로 선택된 block의 일치여부만 확인한다.
                                if (Tmep_SET.Name == Block_DB[k].Name) check_inside_p = true;
                            }
                        }
                        else
                        {                                                      // depth의 노드중 하나라도 일치하지 않는다면, break 
                            check_inside_p = false;
                            break;
                        }

                    }

                    if (check_inside_p == true)
                    {
                        // 현재 선택된 노드의 Block_DB_count를 찾았습니다.
                        richTextBox1.AppendText(Block_DB[k].Parent_Block + Block_DB[k].Name + " : " + k.ToString() + "\n");

                        // 트리에서 선택된 block의 위치를 Block DB에서 찾는데 성공함.
                        Tmep_SET.have_child = Block_DB[k].have_child;
                        Tmep_SET.BlockType = Block_DB[k].BlockType;

                        break;
                    }


                }

                ////////////////////////////////////////////////////////////////////////////////////////////////////////




                // 확인된 Block DB ID를 사용하여, Temp SET의 다른 정보를 최종적으로 기입한다.

                EventHandler eh = new EventHandler(MenuClick);
                MenuItem[] ami = {
                    new MenuItem("결함주입 위치",eh),
                    new MenuItem("-",eh),
                    new MenuItem("상세정보",eh),
                };
                ContextMenu = new System.Windows.Forms.ContextMenu(ami);
            }
            //   MessageBox.Show("KK");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //fault_configuration dlg = new fault_configuration();

            fault_configuration dlg;

            if (Fautl_Model_Info.Block_lib == "none")
            {
                dlg = new fault_configuration();
            }
            else
            {
                dlg = new fault_configuration(Fautl_Model_Info.Block_lib, Fautl_Model_Info.Occur_type,
                    Fautl_Model_Info.F_enable, Fautl_Model_Info.F_disable, Fautl_Model_Info.F_duration,
                    Fautl_Model_Info.F_value);
            }

            dlg.FormSaveEvent += new fault_configuration.FormSendDataHandler(Save_Fault_Configure_Set);

            dlg.ShowDialog();
        }

        // rule ADD event method
        private void Save_Fault_Configure_Set(string[] sender)
        {
            // 리스트 뷰에 설정정보를 추가함
            // 전송 데이터 순서 Block_lib, Occur_type, F_enable, F_disable, F_value
            //listView3
            for (int i = 0; i < listView3.Items.Count; i++)
            {
                listView3.Items[i].SubItems[1].Text = sender[i];
            }

            // 결함모델 설정 구조체를 설정함
            Fautl_Model_Info.Block_lib = sender[0];
            Fautl_Model_Info.Occur_type = sender[1];
            Fautl_Model_Info.F_enable = sender[2];
            Fautl_Model_Info.F_disable = sender[3];
            Fautl_Model_Info.F_duration = sender[4];
            Fautl_Model_Info.F_value = sender[5];

            Fault_Model_Check = true;


        }

        private void GoldenRUN_Click(object sender, EventArgs e)
        {
            // golden run 시뮬레이션을 수행하고, 모니터링 위치의 자료를 수집한다.
            string cmd = "cd " + Golden_Model_File_Path;        // 사용자가 golden run 시뮬레이션 으로 선택한 모델의 위치
            string str = MATLAB.Execute(cmd);
            richTextBox1.AppendText(str);


            // 시뮬레이션을 구동하기 위한 입력 데이터 mat 파일 
            // 별도의 UI를 추가해야 함 일단 하드 코딩 ( only ADC 모델!!!) --> 별도의 입력파일이 필요할 경우 선택할 수 있는 옵션이 필요함!!!!
            
            cmd = "load data_set.mat";           // 선택된 모델을 simulink에서 초기화 한다.
            str = MATLAB.Execute(cmd);
            richTextBox1.AppendText(str);
            

            // (3월 7일)  
            // 1. Golden run simulation을 수행한다.
            // 2. SignalLoggingName 의 명령어를 수행하여, TestPointedSignal 목록 표를 확인한다.
            // 3. TestPointedSignal 목록 표 결과를 파싱하고, listview8번 에 결과 출력(ID 확인)
            // 4. listview8번의 아이템을 선택하여 고장영향 분석 대상 을 선택
            // 5. 결과 조회 (시뮬레이션 결과)
            // 6. 시뮬레이션 결과를 그래프에 출력


            // 1. 
            cmd = "sim('" + Golden_Model_Name + "')";           // 선택된 모델을 simulink에서 초기화 한다.
            //cmd = "sim('ADC_IM_SUM')";           // 선택된 모델을 simulink에서 초기화 한다.
            str = MATLAB.Execute(cmd);            // 모델인식
            richTextBox1.AppendText(str);


            // 2. 
            SignalLoggingName = SignalLoggingName.Replace("\"", "");
            cmd = SignalLoggingName;
            str = MATLAB.Execute(cmd);
            richTextBox1.AppendText(str);

            // 3. 
            char[] delimiterChars = { ' ', '\t', '\n' };
            string[] Output_of_DataLogging = str.ToString().Split(delimiterChars);
            int t_ID_identify = 1;


            string[] refined_DataLogging = new string[1000];
            int refined_size_DL = 0;

            for (int i = 0; i < Output_of_DataLogging.Count(); i++)
            {
                if (Output_of_DataLogging[i] != "")
                {
                    refined_DataLogging[refined_size_DL++] = Output_of_DataLogging[i];
                }
            }


            for (int i = 0; i < refined_size_DL; i++)
            {

                string tmp_SignalName = "";
                string temp_fullpath = "";

                if (refined_DataLogging[i] == "Signal]")
                {

                    i++;        // 다음문장이 이름
                    tmp_SignalName = refined_DataLogging[i++];
                    //while (refined_DataLogging[i].Contains(Golden_Model_Name) != true)
                    //{
                    //    tmp_SignalName += " " + refined_DataLogging[i++];
                    //}
/*
                    if(refined_DataLogging[i].Contains("\n") == true)
                    { 
                        temp_fullpath = refined_DataLogging[i++];
                    }
                    else
                    {
                        temp_fullpath = refined_DataLogging[i++];
                        while (refined_DataLogging[i+1].Contains("[") != true)
                        {
                            temp_fullpath += " " + refined_DataLogging[i++];
                            if (i >= refined_size_DL - 3)
                                break;
                        }
                    }
*/

                    for (int j = 0; j < Data_Logging_Size; j++)
                    {
                        if (tmp_SignalName == S_Data_Logging[j].SignalName)
                        {
                            S_Data_Logging[j].ID = t_ID_identify;
                            t_ID_identify++;
                            break;
                        }
                        /*
                                                // 신호 이름 확인
                                                if (tmp_SignalName == S_Data_Logging[j].SignalName && temp_fullpath == S_Data_Logging[j].fullblockpath)
                                                {
                                                    S_Data_Logging[j].ID = t_ID_identify;
                                                    t_ID_identify++;
                                                    break;
                                                }
                                                else if (S_Data_Logging[j].SignalName == "" && temp_fullpath == S_Data_Logging[j].fullblockpath)
                                                {
                                                    S_Data_Logging[j].ID = t_ID_identify;
                                                    t_ID_identify++;
                                                    break;
                                                }
                        */
                    }

                }
            }

            ///////////////////////!!!!!!!!!!!

            for (int j = 0; j < Data_Logging_Size; j++)
            {

                string[] listUpdata = new string[] { S_Data_Logging[j].ID.ToString(), S_Data_Logging[j].SignalName,
                    S_Data_Logging[j].fullblockpath};

                ListViewItem lvi = new ListViewItem(listUpdata);
                listView8.Items.Add(lvi);
            }

            //4,5,6은 ListView8의 더블클릭 이벤트에서 처리한다. 


            // 모니터링을 수행할 출력 파일의 이름을 설정한다
            // 별도의 UI를 추가해야 함 일단 하드코딩
            /*
            cmd = "CalibratedAirspeed";       // 분석대상 모듈의 포트 핸들을 획득
            str = MATLAB.Execute(cmd);     // 사용자가 분석할 신호를 선택할 수 있다
            */


            /*

            // 수행 결과가 문자열로 출력된다. 문자열의 결과 값을 실수 값으로 변환해야 한다.
            // 

            // 문자열 구분자 생성
            char[] delimiterChars = { ' ', '\t', '\n' };
           
            string[] words_temp = str.ToString().Split(delimiterChars); // 선택된 파일의 모든 워드를 구분한 문자열 배열을 생성

            // 구문 정리한 파일의  불필요한 문자를 제거하기 위한 구문 ("") (시작)
            string[] words = new string[words_temp.Count()];
            int word_size = 0;
            for (int i = 0; i < words.Count(); i++)
            {
                if (words_temp[i] != "")
                {
                    words[word_size++] = words_temp[i];
                }
            }

            richTextBox1.AppendText("\n----------------------------\n");

            double check_number;

            for (int i = 0; i < word_size; i++)
            {
                //   richTextBox1.AppendText(words[i]);
                //   richTextBox1.AppendText("\n");
                bool canConvert = double.TryParse(words[i], out check_number);
                if (canConvert == true)
                {
                    golden_result_SET[golden_result_SIZE++] = Convert.ToDouble(words[i]);
                    //richTextBox1.AppendText(golden_result_SET[golden_result_SIZE - 1].ToString());
                    //richTextBox1.AppendText("\n");
                }
            }
            richTextBox1.AppendText("\n----------------------------\n");
            // 구문 정리한 파일의  불필요한 문자를 제거하기 위한 구문 (끝)


            // chart 에 시뮬레이션 결과를 그려준다.
            chart1.Series[0].Points.Clear();

            chart1.Series[0].ChartType = SeriesChartType.Spline;

            for(int i =1; i < golden_result_SIZE; i++)
            {
                //chart1.Series[0].Points.AddXY(i, golden_result_SET[i] * golden_result_SET[0]);
                chart1.Series[0].Points.AddXY(i, golden_result_SET[i]);
            }

            */


            /* 모니터링 하고자 하는 block이 output port 가 있는 Block 인 경우
            //cmd = "ph = get_param('" + Golden_Model_Name + "/" + "PressureAltitude" + "', 'PortHandles')";       // 분석대상 모듈의 포트 핸들을 획득
            cmd = "ph = get_param('" + "ADC/Pressure Altitude(ft)" + "', 'PortHandles')";       // 분석대상 모듈의 포트 핸들을 획득
            str = MATLAB.Execute(cmd);     // 사용자가 분석할 신호를 선택할 수 있다
            richTextBox1.AppendText(str);

            cmd = "set_param(ph.State, 'DataLogging', 'on')";                      // 분석대상 모듈의 출력 포트에 대한 datalogging 설정
            str = MATLAB.Execute(cmd);     // 사용자가 분석할 신호를 선택할 수 있다
            richTextBox1.AppendText(str);

            cmd = "sim('" + Golden_Model_Name + "')";           // 선택된 모델을 simulink에서 초기화 한다.
            MATLAB.Execute(cmd);            // 모델인식
            */
        }



        // Tree View을 필터링 하는 재귀함수

        bool filter_M_block = false;
        bool filter_F_block = false;

        private void TreeRecursive_M(TreeNode TreeNode)
        {

            string temp = "";
            TreeNode tmp = TreeNode;

            while (tmp.Parent != null)
            {
                temp = "@" + tmp.Parent.Text + temp;
                tmp = tmp.Parent;

            }

            temp = "TOP LEVEL" + temp;


            for (int i = 0; i < Block_DB_count; i++)
            {

                if (Block_DB[i].Name == TreeNode.Text && Block_DB[i].Parent_Block == temp)
                {


                    if (Block_DB[i].Set_Save2DSignal == true && filter_M_block == true)
                    {
                        TreeNode.BackColor = Color.Green;

                    }
                    else if (Block_DB[i].Set_Save2DSignal == true && filter_M_block == false)
                    {
                        TreeNode.BackColor = Color.White;

                    }
                }
            }

            foreach (TreeNode tn in TreeNode.Nodes)
            {
                TreeRecursive_M(tn);
            }
        }

        private void TreeRecursive_F(TreeNode TreeNode)
        {

            string temp = "";
            TreeNode tmp = TreeNode;

            while (tmp.Parent != null)
            {
                temp = "@" + tmp.Parent.Text + temp;
                tmp = tmp.Parent;

            }

            temp = "TOP LEVEL" + temp;


            for (int i = 0; i < Block_DB_count; i++)
            {

                if (Block_DB[i].Name == TreeNode.Text && Block_DB[i].Parent_Block == temp)
                {


                    if (((Block_DB[i].BlockType == "Product") || (Block_DB[i].BlockType == "Sum") || (Block_DB[i].BlockType == "Math") || (Block_DB[i].BlockType == "Gain")) && filter_F_block == true)
                    {
                        TreeNode.BackColor = Color.Red;

                    }
                    else if (((Block_DB[i].BlockType == "Product") || (Block_DB[i].BlockType == "Sum") || (Block_DB[i].BlockType == "Math") || (Block_DB[i].BlockType == "Gain")) && filter_F_block == false)
                    {
                        TreeNode.BackColor = Color.White;

                    }
                }
            }

            foreach (TreeNode tn in TreeNode.Nodes)
            {
                TreeRecursive_F(tn);
            }
        }


        // 결함주입 시뮬레이션을 수행하는 함수 임, 1회의 결함주입 시험을 수행하고, 시뮬레이션 결과를
        // 문자열로 반환 받는다.
        private string Run_fault_Simultion()
        {
            MATLAB.Execute("clear");


            string cmd = "load data_set.mat";           // 선택된 모델을 simulink에서 초기화 한다.
            string str;
            /*
            string str = MATLAB.Execute(cmd);
            richTextBox1.AppendText(str);
            */

            //Fault_Model_Name
            //fault_file_path

            // 1. 결함주입 Simulink 모델이 있는 위치로 작업 공간을 이동한다.
            cmd = "cd " + fault_file_path;        // 사용자가 golden run 시뮬레이션 으로 선택한 모델의 위치
            str = MATLAB.Execute(cmd);

            // 2. 결함주입 Simulink 모델을 초기화 하기 위해, 준비 시뮬레이션을 1회 수행한다.
            cmd = "sim('" + Fault_Model_Name + "')";           // 선택된 모델을 simulink에서 초기화 한다.
            MATLAB.Execute(cmd);            // 모델인식

            // 3. 결함주입 시나리오를 설정한다.
            // 3-1. 결함이 주입되는 시간 구간 설정.
            cmd = "f_time = randi([" + Fautl_Model_Info.F_enable + "," + Fautl_Model_Info.F_disable + "]);";
            str = MATLAB.Execute(cmd);

            cmd = "f_time";
            str = MATLAB.Execute(cmd);
            str = str.Replace("\n", "");
            str = str.Replace("f_time =", "");
            fault_injection_time = Int32.Parse(str);

            // 3-2. 결함값이 유지되는 시간을 random하게 설정 --> 결함이 유지되는 시간 구간은 사용자가 설정할 수 있도록??
            Random r = new Random();
            //Fautl_Model_Info.F_duration;// = (r.Next(1, 30)).ToString();

            cmd = "f_time = int32(f_time);";
            str = MATLAB.Execute(cmd);

            // 3-3. 결함이 활성화 되는 시간에서의 break point(in simulink)
            cmd = "fault_S_set = sprintf('tbreak %d', f_time);";
            str = MATLAB.Execute(cmd);

            // 3-3. 결함이 비활성화 되는 시간에서의 break point(in simulink)
            cmd = "fault_E_set = sprintf('tbreak %d', f_time+" + Fautl_Model_Info.F_duration + ");";
            str = MATLAB.Execute(cmd);

            // 3-4. 결함이 활성화 되도록 결함모듈 제어신호 명령 (제어신호 3)
            string str_f_selector = Fault_Model_Name;   // 
            string str_f_value = Fault_Model_Name;

            for (int x = 0; x <= Fault_SET.depth - 2; x++)
            {
                str_f_selector += "/" + Fault_SET.Parent_Block[x];
                str_f_value += "/" + Fault_SET.Parent_Block[x];
            }

            // (3월 13일) fault model에 다른 결함주입 블록의 제어문 작성 코드
            //  { "Loss of Function", "More Than Requested","Less Than Requested", "Wrong Direction", "Unintended Activation", "Locked Function" ,"Early Timing", "Late Timing"};

            if (Fautl_Model_Info.Block_lib == "Less Than Requested" || Fautl_Model_Info.Block_lib == "More Than Requested")
            {

                //str_f_model += "/" + Fault_SET.Name;
                str_f_selector += "/" + "F_line_block" + "/" + "Constant";                     // (3월 11일): 자동화 수정 요구
                str_f_selector = str_f_selector.Replace("\"", "");

                str_f_value += "/" + "F_line_block" + "/" + "Gain";
                str_f_value = str_f_value.Replace("\"", "");

                cmd = "slector_f_set = 'set_param(''" + str_f_selector + "'', ''value'', ''2'')';";       // (3월 11일): 자동화 수정 요구
                str = MATLAB.Execute(cmd);

                // 3-4. 결함이 비활성화 되도록 결함모듈 제어신호 명령 (제어신호 1)
                cmd = "slector_init = 'set_param(''" + str_f_selector + "'', ''value'', ''1'')';";
                str = MATLAB.Execute(cmd);

                cmd = "fault_V_set = 'set_param(''" + str_f_value + "'', ''Gain'', ''" + Fautl_Model_Info.F_value + "'')';";
                str = MATLAB.Execute(cmd);

                // 3-5. 결함주입 시나리오를 구동하는 명령어 리스트 설정(준비완료)

                cmd = "cmds = {fault_S_set, slector_init, fault_V_set, 'c', slector_f_set, fault_E_set, 'c',  slector_init, 'c', 'c'};";
                str = MATLAB.Execute(cmd);

            }
            else if (Fautl_Model_Info.Block_lib == "Unintended Activation")
            {
                //str_f_model += "/" + Fault_SET.Name;
                str_f_selector += "/" + "F_line_block" + "/" + "Constant";                     // (3월 11일): 자동화 수정 요구
                str_f_selector = str_f_selector.Replace("\"", "");

                str_f_value += "/" + "F_line_block" + "/" + "Constant2";
                str_f_value = str_f_value.Replace("\"", "");

                cmd = "slector_f_set = 'set_param(''" + str_f_selector + "'', ''value'', ''4'')';";       // (3월 11일): 자동화 수정 요구
                str = MATLAB.Execute(cmd);

                // 3-4. 결함이 비활성화 되도록 결함모듈 제어신호 명령 (제어신호 1)
                cmd = "slector_init = 'set_param(''" + str_f_selector + "'', ''value'', ''1'')';";
                str = MATLAB.Execute(cmd);

                // (3월 15일) 결함 값이 범위 내의 랜덤의 경우 !!! 처리루프 필요함

                if (Fautl_Model_Info.F_value.Contains("-") == true)
                {
                    // 결함주입 값이 random은 경우 "1-20"  입력한다.
                    //string random_fualt 
                    char[] delimiterChars = { ' ', '\t', '\n', '-' };
                    string[] refind_F_value = Fautl_Model_Info.F_value.ToString().Split(delimiterChars);
                    string random_fualt = r.Next(Int32.Parse(refind_F_value[0]), Int32.Parse(refind_F_value[1])).ToString();


                    cmd = "fault_V_set = 'set_param(''" + str_f_value + "'', ''value'', ''" + random_fualt + "'')';";
                    str = MATLAB.Execute(cmd);

                }
                else
                {

                    cmd = "fault_V_set = 'set_param(''" + str_f_value + "'', ''value'', ''" + Fautl_Model_Info.F_value + "'')';";
                    str = MATLAB.Execute(cmd);
                }

                // 3-5. 결함주입 시나리오를 구동하는 명령어 리스트 설정(준비완료)
                cmd = "cmds = {fault_S_set, slector_init, fault_V_set, 'c', slector_f_set, fault_E_set, 'c',  slector_init, 'c', 'c'};";
                str = MATLAB.Execute(cmd);
            }
            else if (Fautl_Model_Info.Block_lib == "Locked Function")
            {
                //str_f_model += "/" + Fault_SET.Name;
                str_f_selector += "/" + "F_line_block" + "/" + "Constant";                     // (3월 11일): 자동화 수정 요구
                str_f_selector = str_f_selector.Replace("\"", "");

                cmd = "slector_f_set = 'set_param(''" + str_f_selector + "'', ''value'', ''5'')';";       // (3월 11일): 자동화 수정 요구
                str = MATLAB.Execute(cmd);

                // 3-4. 결함이 비활성화 되도록 결함모듈 제어신호 명령 (제어신호 1)
                cmd = "slector_init = 'set_param(''" + str_f_selector + "'', ''value'', ''1'')';";
                str = MATLAB.Execute(cmd);

                // 3-5. 결함주입 시나리오를 구동하는 명령어 리스트 설정(준비완료)
                cmd = "cmds = {fault_S_set, slector_init, 'c', slector_f_set, fault_E_set, 'c',  slector_init, 'c', 'c'};";
                str = MATLAB.Execute(cmd);
            }
            else
            {
                //str_f_model += "/" + Fault_SET.Name;
                str_f_selector += "/" + "F_line_block" + "/" + "Constant";                     // (3월 11일): 자동화 수정 요구
                str_f_selector = str_f_selector.Replace("\"", "");

                cmd = "slector_f_set = 'set_param(''" + str_f_selector + "'', ''value'', ''3'')';";       // (3월 11일): 자동화 수정 요구
                str = MATLAB.Execute(cmd);

                // 3-4. 결함이 비활성화 되도록 결함모듈 제어신호 명령 (제어신호 1)
                cmd = "slector_init = 'set_param(''" + str_f_selector + "'', ''value'', ''1'')';";
                str = MATLAB.Execute(cmd);

                // 3-5. 결함주입 시나리오를 구동하는 명령어 리스트 설정(준비완료)
                cmd = "cmds = {fault_S_set, slector_init, 'c', slector_f_set, fault_E_set, 'c',  slector_init, 'c', 'c'};";
                str = MATLAB.Execute(cmd);
            }



            // 결함주입 모듈 설정 모듈 초기화
            //cmd = "set_param(''My_XXX_run/F_line_block/Constant'', ''value'', ''1'');";
            //str = MATLAB.Execute(cmd);

            // 3-6 결함주입 시뮬레이션 구동 시작

            cmd = "sim('" + Fault_Model_Name + "', 'debug', cmds)";
            str = MATLAB.Execute(cmd);

            // 4. 결함주입 시뮬레이션 수행 결과를 반환한다.
            // 모니터링을 수행할 출력 파일의 이름을 설정한다
            // Monitoring_SET.Name

            cmd = "ans." + SignalLoggingName + "{" + Monitoring_SET.ID_Num.ToString() + "}.Values.Data";       // sldemo_autotrans_output{0}.Values.Data
            str = MATLAB.Execute(cmd);     // 사용자가 분석할 신호를 선택할 수 있다

            return str;


        }

        private string Run_fault_Simultion(string FI_Module)
        {

            //SFI_MATLAB = new MLApp.MLApp();
            //SFI_MATLAB.Visible = 1;             // command 터미널 감춘다

            //MATLAB.Execute("clear");


            string cmd;// = "load data_set.mat";           // 선택된 모델을 simulink에서 초기화 한다.
            string str;

            // 1. 결함주입 Simulink 모델이 있는 위치로 작업 공간을 이동한다.
            cmd = "cd " + fault_file_path;        // 사용자가 golden run 시뮬레이션 으로 선택한 모델의 위치
            str = MATLAB.Execute(cmd);

            // 2. 결함주입 Simulink 모델을 초기화 하기 위해, 준비 시뮬레이션을 1회 수행한다.
            cmd = "sim('" + Fault_Model_Name + "')";           // 선택된 모델을 simulink에서 초기화 한다.
            MATLAB.Execute(cmd);            // 모델인식

            // 3. 결함주입 시나리오를 설정한다.
            // 3-1. 결함이 주입되는 시간 구간 설정.
            cmd = "f_time = randi([" + Fautl_Model_Info.F_enable + "," + Fautl_Model_Info.F_disable + "]);";
            str = MATLAB.Execute(cmd);

            cmd = "f_time";
            str = MATLAB.Execute(cmd);
            str = str.Replace("\n", "");
            str = str.Replace("f_time =", "");
            fault_injection_time = Int32.Parse(str);

            // 3-2. 결함값이 유지되는 시간을 random하게 설정 --> 결함이 유지되는 시간 구간은 사용자가 설정할 수 있도록??
            Random r = new Random();

            cmd = "f_time = int32(f_time);";
            str = MATLAB.Execute(cmd);

            // 3-3. 결함이 활성화 되는 시간에서의 break point(in simulink)
            cmd = "fault_S_set = sprintf('tbreak %d', f_time);";
            str = MATLAB.Execute(cmd);

            // 3-3. 결함이 비활성화 되는 시간에서의 break point(in simulink)
            cmd = "fault_E_set = sprintf('tbreak %d', f_time+" + Fautl_Model_Info.F_duration + ");";
            str = MATLAB.Execute(cmd);

            // 3-4. 결함이 활성화 되도록 결함모듈 제어신호 명령 (제어신호 3)
            string str_f_selector = Fault_Model_Name;   // 
            string str_f_value = Fault_Model_Name;
/*
            for (int x = 0; x <= Fault_SET.depth - 2; x++)
            {
                str_f_selector += "/" + Fault_SET.Parent_Block[x];
                str_f_value += "/" + Fault_SET.Parent_Block[x];
            }
*/
            // (3월 13일) fault model에 다른 결함주입 블록의 제어문 작성 코드
            //  { "Loss of Function", "More Than Requested","Less Than Requested", "Wrong Direction", "Unintended Activation", "Locked Function" ,"Early Timing", "Late Timing"};

            if (Fautl_Model_Info.Block_lib == "Less Than Requested" || Fautl_Model_Info.Block_lib == "More Than Requested")
            {

                //str_f_model += "/" + Fault_SET.Name;
                str_f_selector += "/" + FI_Module + "/" + "Constant";                     // (3월 11일): 자동화 수정 요구
                str_f_selector = str_f_selector.Replace("\"", "");

                str_f_value += "/" + FI_Module + "/" + "Gain";
                str_f_value = str_f_value.Replace("\"", "");

                cmd = "slector_f_set = 'set_param(''" + str_f_selector + "'', ''value'', ''2'')';";       // (3월 11일): 자동화 수정 요구
                str = MATLAB.Execute(cmd);

                // 3-4. 결함이 비활성화 되도록 결함모듈 제어신호 명령 (제어신호 1)
                cmd = "slector_init = 'set_param(''" + str_f_selector + "'', ''value'', ''1'')';";
                str = MATLAB.Execute(cmd);

                cmd = "fault_V_set = 'set_param(''" + str_f_value + "'', ''Gain'', ''" + Fautl_Model_Info.F_value + "'')';";
                str = MATLAB.Execute(cmd);

                // 3-5. 결함주입 시나리오를 구동하는 명령어 리스트 설정(준비완료)

                cmd = "cmds = {fault_S_set, slector_init, fault_V_set, 'c', slector_f_set, fault_E_set, 'c',  slector_init, 'c', 'c'};";
                str = MATLAB.Execute(cmd);

            }
            else if (Fautl_Model_Info.Block_lib == "Unintended Activation")
            {
                //str_f_model += "/" + Fault_SET.Name;
                str_f_selector += "/" + FI_Module + "/" + "Constant";                     // (3월 11일): 자동화 수정 요구
                str_f_selector = str_f_selector.Replace("\"", "");

                str_f_value += "/" + FI_Module + "/" + "Constant2";
                str_f_value = str_f_value.Replace("\"", "");

                cmd = "slector_f_set = 'set_param(''" + str_f_selector + "'', ''value'', ''4'')';";       // (3월 11일): 자동화 수정 요구
                str = MATLAB.Execute(cmd);

                // 3-4. 결함이 비활성화 되도록 결함모듈 제어신호 명령 (제어신호 1)
                cmd = "slector_init = 'set_param(''" + str_f_selector + "'', ''value'', ''1'')';";
                str = MATLAB.Execute(cmd);

                // (3월 15일) 결함 값이 범위 내의 랜덤의 경우 !!! 처리루프 필요함

                if (Fautl_Model_Info.F_value.Contains("-") == true)
                {
                    // 결함주입 값이 random은 경우 "1-20"  입력한다.
                    //string random_fualt 
                    char[] delimiterChars = { ' ', '\t', '\n', '-' };
                    string[] refind_F_value = Fautl_Model_Info.F_value.ToString().Split(delimiterChars);
                    string random_fualt = r.Next(Int32.Parse(refind_F_value[0]), Int32.Parse(refind_F_value[1])).ToString();


                    cmd = "fault_V_set = 'set_param(''" + str_f_value + "'', ''value'', ''" + random_fualt + "'')';";
                    str = MATLAB.Execute(cmd);

                }
                else
                {

                    cmd = "fault_V_set = 'set_param(''" + str_f_value + "'', ''value'', ''" + Fautl_Model_Info.F_value + "'')';";
                    str = MATLAB.Execute(cmd);
                }

                // 3-5. 결함주입 시나리오를 구동하는 명령어 리스트 설정(준비완료)
                cmd = "cmds = {fault_S_set, slector_init, fault_V_set, 'c', slector_f_set, fault_E_set, 'c',  slector_init, 'c', 'c'};";
                str = MATLAB.Execute(cmd);
            }
            else if (Fautl_Model_Info.Block_lib == "Locked Function")
            {
                //str_f_model += "/" + Fault_SET.Name;
                str_f_selector += "/" + FI_Module + "/" + "Constant";                     // (3월 11일): 자동화 수정 요구
                str_f_selector = str_f_selector.Replace("\"", "");

                cmd = "slector_f_set = 'set_param(''" + str_f_selector + "'', ''value'', ''5'')';";       // (3월 11일): 자동화 수정 요구
                str = MATLAB.Execute(cmd);

                // 3-4. 결함이 비활성화 되도록 결함모듈 제어신호 명령 (제어신호 1)
                cmd = "slector_init = 'set_param(''" + str_f_selector + "'', ''value'', ''1'')';";
                str = MATLAB.Execute(cmd);

                // 3-5. 결함주입 시나리오를 구동하는 명령어 리스트 설정(준비완료)
                cmd = "cmds = {fault_S_set, slector_init, 'c', slector_f_set, fault_E_set, 'c',  slector_init, 'c', 'c'};";
                str = MATLAB.Execute(cmd);
            }
            else
            {
                //str_f_model += "/" + Fault_SET.Name;
                str_f_selector += "/" + FI_Module + "/" + "Constant";                     // (3월 11일): 자동화 수정 요구
                str_f_selector = str_f_selector.Replace("\"", "");

                cmd = "slector_f_set = 'set_param(''" + str_f_selector + "'', ''value'', ''3'')';";       // (3월 11일): 자동화 수정 요구
                str = MATLAB.Execute(cmd);

                // 3-4. 결함이 비활성화 되도록 결함모듈 제어신호 명령 (제어신호 1)
                cmd = "slector_init = 'set_param(''" + str_f_selector + "'', ''value'', ''1'')';";
                str = MATLAB.Execute(cmd);

                // 3-5. 결함주입 시나리오를 구동하는 명령어 리스트 설정(준비완료)
                cmd = "cmds = {fault_S_set, slector_init, 'c', slector_f_set, fault_E_set, 'c',  slector_init, 'c', 'c'};";
                str = MATLAB.Execute(cmd);
            }



            // 결함주입 모듈 설정 모듈 초기화
            //cmd = "set_param(''My_XXX_run/F_line_block/Constant'', ''value'', ''1'');";
            //str = MATLAB.Execute(cmd);

            // 3-6 결함주입 시뮬레이션 구동 시작

            cmd = "sim('" + Fault_Model_Name + "', 'debug', cmds)";
            str = MATLAB.Execute(cmd);

            // 4. 결함주입 시뮬레이션 수행 결과를 반환한다.
            // 모니터링을 수행할 출력 파일의 이름을 설정한다
            // Monitoring_SET.Name

            cmd = "ans." + SignalLoggingName + "{" + Monitoring_SET.ID_Num.ToString() + "}.Values.Data";       // sldemo_autotrans_output{0}.Values.Data
            str = MATLAB.Execute(cmd);     // 사용자가 분석할 신호를 선택할 수 있다

            //SFI_MATLAB.Execute("quit force");
            //SFI_MATLAB.Quit();

            return str;


        }



        // 문자열로 반환된 시뮬레이션 결과를 double set르로 변환하는 함수

        private double[] string_to_double_set(string str, ref int result_num)
        {

            char[] delimiterChars = { ' ', '\t', '\n' };

            string[] refind_sim_result = str.ToString().Split(delimiterChars); // 선택된 파일의 모든 워드를 구분한 문자열 배열을 생성

            // 구문 정리한 파일의  불필요한 문자를 제거하기 위한 구문 ("") (시작)
            string[] c_refind_sim_result = new string[refind_sim_result.Count()];
            int word_size = 0;
            for (int i = 0; i < refind_sim_result.Count(); i++)
            {
                if (refind_sim_result[i] != "")
                {
                    c_refind_sim_result[word_size++] = refind_sim_result[i];
                }
            }

            // 데이터를 문자열에서 실수형으로 변환  // 3월 08일

            double[] final_sim_result = new double[word_size];
            int start_sim = 0;
            int final_sim_size = 0;
            bool set_product = false;
            double setvalue = 0.0;

            if (c_refind_sim_result[3] == "*")
            {
                setvalue = Double.Parse(c_refind_sim_result[2]);
                start_sim = 4;
                set_product = true;
            }
            else if (c_refind_sim_result[3] == "single")
            {
                start_sim = 6;
            }
            else
            {
                start_sim = 2;
            }


            for (int i = start_sim; i < word_size; i++)
            {
                if (set_product == true)
                {
                    final_sim_result[final_sim_size++] = Double.Parse(c_refind_sim_result[i]) * setvalue;
                }
                else
                {
                    final_sim_result[final_sim_size++] = Double.Parse(c_refind_sim_result[i]);
                }
            }

            result_num = word_size;

            return final_sim_result;

            // 문자열 구분자 생성
            /*
            char[] delimiterChars = { ' ', '\t', '\n' };

            string[] refind_sim_result = str.ToString().Split(delimiterChars); // 선택된 파일의 모든 워드를 구분한 문자열 배열을 생성

            // 구문 정리한 파일의  불필요한 문자를 제거하기 위한 구문 ("") (시작)
            string[] c_refind_sim_result = new string[refind_sim_result.Count()];
            int word_size = 0;
            for (int i = 0; i < refind_sim_result.Count(); i++)
            {
                if (refind_sim_result[i] != "")
                {
                    c_refind_sim_result[word_size++] = refind_sim_result[i];
                }
            }

            // 데이터를 문자열에서 실수형으로 변환  // 3월 08일

            double[] final_sim_result = new double[word_size];
            int start_sim = 0;
            int final_sim_size = 0;
            bool set_product = false;
            double setvalue = 0.0;

            if (c_refind_sim_result[3] == "*")
            {
                setvalue = Double.Parse(c_refind_sim_result[2]);
                start_sim = 4;
                set_product = true;
            }
            else
            {
                start_sim = 2;
            }


            for (int i = start_sim; i < word_size; i++)
            {
                if (set_product == true)
                {
                    final_sim_result[final_sim_size++] = Double.Parse(c_refind_sim_result[i]) * setvalue;
                }
                else
                {
                    final_sim_result[final_sim_size++] = Double.Parse(c_refind_sim_result[i]);
                }
            }

            fault_result_SIZE = final_sim_size;

            return final_sim_result;
            */
        }

        // 시뮬레이션 결과 double set을  그래프 chart에 그려주는 함수    
        private void drow_chart(ref double[] data, ref int size, int type)
        {
            // type 1: golden type 2 : fault

            if (type == 1)
            {

            }
            else if (type == 2)
            {

                chart1.Series[1].Points.Clear();

                chart1.Series[1].ChartType = SeriesChartType.Spline;

                for (int i = 1; i < size; i++)
                {
                    //chart1.Series[1].Points.AddXY(i, fault_result_SET[i] * fault_result_SET[0]);
                    //if (golden_result_SET[i] != (data[i]*1000))
                    //{
                    chart1.Series[1].Points.AddXY(i, data[i]);
                    //}
                    //else
                    //{
                    //
                    //}
                }
            }
            else
            {

            }
        }



        private void FaultRUN_Click(object sender, EventArgs e)
        {
            // 결함주입 시뮬레이션을 수행함; 시뮬레이션 결과가 문자열로 저장됨
            string str = Run_fault_Simultion();

            //문자열로 저장된 시뮬레이션 결과를 double 형 배열로 변환
            fault_result_SET = string_to_double_set(str, ref fault_result_SIZE);

            // 시뮬레이션 결과를 그래프로 표식
            drow_chart(ref fault_result_SET, ref fault_result_SIZE, 2);
            // chart 에 시뮬레이션 결과를 그려준다.
            ////////////////////////////////////////////////////////////
        }


        private void check_Filter_monitor_Block_CheckedChanged(object sender, EventArgs e)
        {
            // 모니터링 block 만 필터링 해서 보고자 할때 
            if (check_Filter_monitor_Block.Checked == true)
            {

                // 결함주입 시뮬레이션

                // Tree 분류
                TreeNodeCollection nodes = treeView1.Nodes;

                //treeView1.Node

                foreach (TreeNode n in nodes)
                {
                    filter_M_block = true;
                    TreeRecursive_M(n);
                }

            }
            else
            {
                // 결함주입 시뮬레이션

                // Tree 분류
                TreeNodeCollection nodes = treeView1.Nodes;

                //treeView1.Node

                foreach (TreeNode n in nodes)
                {
                    filter_M_block = false;
                    TreeRecursive_M(n);
                }
            }

        }

        private void check_Filter_fault_Block_CheckedChanged(object sender, EventArgs e)
        {
            // 결함주입 가능한 블록함 필터링 해서 보고자 할대
            if (check_Filter_fault_Block.Checked == true)
            {

                // Tree 분류
                TreeNodeCollection nodes = treeView1.Nodes;

                //treeView1.Node

                foreach (TreeNode n in nodes)
                {
                    filter_F_block = true;
                    TreeRecursive_F(n);
                }
            }
            else
            {
                // 결함주입 시뮬레이션

                // Tree 분류
                TreeNodeCollection nodes = treeView1.Nodes;

                //treeView1.Node

                foreach (TreeNode n in nodes)
                {
                    filter_F_block = false;
                    TreeRecursive_F(n);
                }
            }


        }


        // 95% 신뢰구간에서의 시험 횟수
        int num_of_sim_i = 0;
        double num_of_sim_d = 0;

        private void CI95_CheckedChanged(object sender, EventArgs e)
        {
            if (Fault_Model_Check == true)
            {
                double N = Convert.ToDouble(Fautl_Model_Info.F_disable) - Convert.ToDouble(Fautl_Model_Info.F_enable);
                double error_magine = 0.01;
                double t_value = 1.96;
                double init_N = 0.5;

                num_of_sim_d = N / (1 + (error_magine * error_magine) * ((N - 1) / ((t_value * t_value) * init_N * (1 - init_N))));
                num_of_sim_i = Convert.ToInt32(num_of_sim_d);

                textBox1.Text = Convert.ToString(num_of_sim_i);

            }
            else
            {

            }
        }

        // 99% 신뢰구간에서의 시험 횟수
        private void CI99_CheckedChanged(object sender, EventArgs e)
        {
            if (Fault_Model_Check == true)
            {
                double N = Convert.ToDouble(Fautl_Model_Info.F_disable) - Convert.ToDouble(Fautl_Model_Info.F_enable);
                double error_magine = 0.01;
                double t_value = 2.5758;
                double init_N = 0.5;

                num_of_sim_d = N / (1 + (error_magine * error_magine) * ((N - 1) / ((t_value * t_value) * init_N * (1 - init_N))));
                num_of_sim_i = Convert.ToInt32(num_of_sim_d);

                textBox1.Text = Convert.ToString(num_of_sim_i);

            }
            else
            {

            }
        }

        // 99.% 신뢰구간에서의 시험 횟수
        private void CI999_CheckedChanged(object sender, EventArgs e)
        {
            if (Fault_Model_Check == true)
            {
                double N = Convert.ToDouble(Fautl_Model_Info.F_disable) - Convert.ToDouble(Fautl_Model_Info.F_enable);
                double error_magine = 0.01;
                double t_value = 3.0902;
                double init_N = 0.5;

                num_of_sim_d = N / (1 + (error_magine * error_magine) * ((N - 1) / ((t_value * t_value) * init_N * (1 - init_N))));
                num_of_sim_i = Convert.ToInt32(num_of_sim_d);

                textBox1.Text = Convert.ToString(num_of_sim_i);

            }
            else
            {

            }
        }

        private void excel_export_Click(object sender, EventArgs e)
        {

            if (C_complete_SFI == true)
            {
                SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                saveFileDialog1.Filter = "TXT 파일 (*.txt) | *.txt; | 모든 파일 (*.*) | *.*";
                saveFileDialog1.Title = "TXT File Save";
                saveFileDialog1.ShowDialog();
                if (saveFileDialog1.FileName != "")
                {

                    w_FMEA_File = new StreamWriter(saveFileDialog1.FileName);
                    w_FMEA_File.Write("순번\t");     // 문자열을 추가한다.
                    w_FMEA_File.Write("고장모드\t");
                    w_FMEA_File.Write("고장값\t");
                    w_FMEA_File.Write("고장유지시간\t");
                    w_FMEA_File.Write("고장영향\t");
                    w_FMEA_File.Write("심각도\t");
                    w_FMEA_File.Write("발생빈도\t");
                    w_FMEA_File.Write("검출도\t");
                    w_FMEA_File.Write("RPN\t");
                    w_FMEA_File.Write("평균 고장값\t");
                    w_FMEA_File.Write("최대 고장값\t");
                    w_FMEA_File.Write("고장주입 시간\t\n");

                    // for (int i = 0; i < SFI_secnario_num; i++) SFI_running_num
                    for (int i = 0; i < SFI_running_num; i++)           // 지금까지 수행된 시험횟수라도 저장한다./
                    {
                        ListViewItem item = listView4.Items[i];

                        w_FMEA_File.Write(item.SubItems[0].Text + "\t");
                        w_FMEA_File.Write(item.SubItems[1].Text + "\t");
                        w_FMEA_File.Write(item.SubItems[2].Text + "\t");
                        w_FMEA_File.Write(item.SubItems[3].Text + "\t");
                        w_FMEA_File.Write(item.SubItems[4].Text + "\t");
                        w_FMEA_File.Write(item.SubItems[5].Text + "\t");
                        w_FMEA_File.Write(item.SubItems[6].Text + "\t");
                        w_FMEA_File.Write(item.SubItems[7].Text + "\t");
                        w_FMEA_File.Write(item.SubItems[8].Text + "\t");
                        w_FMEA_File.Write(item.SubItems[9].Text + "\t");
                        w_FMEA_File.Write(item.SubItems[10].Text + "\t");
                        w_FMEA_File.Write(item.SubItems[11].Text + "\t" + "\n");
                    }

                    // 결함주입 모듈과 line이 추가된 모델을 저장함
                    w_FMEA_File.Close();



                }
            }
            else
            {
                MessageBox.Show("통계적 시험을 완료한 후에, FMEA 파일을 출력할 수 있습니다");
            }



            /*     
                        if (C_complete_SFI == true)
                        {


                            // FMEA excel 파일을 저장하기 위한 다이알로그를 생성
                            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                            saveFileDialog1.Filter = "EXCEL 파일 (*.xlsx) | *.xlsx; | 모든 파일 (*.*) | *.*";
                            saveFileDialog1.Title = "EXCEL File Save";
                            saveFileDialog1.ShowDialog();

                            //  다이알로그에서 저장할 파일명이 입력 되었음 (MDL 파일 SAVE 파일명이 null이 아니면...)
                            if (saveFileDialog1.FileName != "")
                            {
                                // test용 일단 생성 차후, 엑셀 생성 버튼 으로 사용
                                // 배열 파일을 엑셀로 출력한다.!!!
                                // [2월 21일]

                                // 신규파일 생성
                                Excel.Application ExcelApp = null;        // Excel 프로그램을 의미함
                                Excel.Workbook ExcelBook = null;            // 통합 문서를 의미함
                                Excel.Worksheet ExcelSheet = null;          // 워크시트를 의미함    

                                ExcelApp = new Excel.Application();
                                ExcelApp.Visible = false;           // Excel 작업 내용이 보이지 않음
                                ExcelApp.DisplayAlerts = false;     // Excel 경고 발생을 방지
                                ExcelApp.Interactive = false;       // 유저의 조작에 방해 받지 않도록 한다



                                if (ExcelApp == null)
                                {
                                    MessageBox.Show("Excel이 정상적으로 설치되어 있지 않습니다.");
                                    return;
                                }

                                ExcelBook = ExcelApp.Workbooks.Add(Type.Missing);   // 통합 문서를 하나 추가한다
                                ExcelSheet = ExcelBook.ActiveSheet;
                                ExcelSheet.Name = "FMEA";
                                ExcelSheet.Cells[1, 1] = "순번";
                                ExcelSheet.Cells[1, 2] = "고장모드";
                                ExcelSheet.Cells[1, 3] = "고장영향";
                                ExcelSheet.Cells[1, 4] = "심각도";
                                ExcelSheet.Cells[1, 5] = "발생빈도";
                                ExcelSheet.Cells[1, 6] = "검출도";
                                ExcelSheet.Cells[1, 7] = "RPN";
                                ExcelSheet.Cells[1, 8] = "평균 고장값";
                                ExcelSheet.Cells[1, 9] = "최대 고장값";
                                ExcelSheet.Cells[1, 10] = "고장주입 시간";
                                /////////////////////////// FMEA 리스트에 있는 데이터를 불러온다


                                for (int i = 0; i < Convert.ToInt32(textBox1.Text); i++)
                                {
                                    ListViewItem item = listView4.Items[i];

                                    ExcelSheet.Cells[2 + i, 1] = item.SubItems[0].Text;
                                    ExcelSheet.Cells[2 + i, 2] = item.SubItems[1].Text;
                                    ExcelSheet.Cells[2 + i, 3] = item.SubItems[2].Text;
                                    ExcelSheet.Cells[2 + i, 4] = item.SubItems[3].Text;
                                    ExcelSheet.Cells[2 + i, 5] = item.SubItems[4].Text;
                                    ExcelSheet.Cells[2 + i, 6] = item.SubItems[5].Text;
                                    ExcelSheet.Cells[2 + i, 7] = item.SubItems[6].Text;
                                    ExcelSheet.Cells[2 + i, 8] = item.SubItems[7].Text;
                                    ExcelSheet.Cells[2 + i, 9] = item.SubItems[8].Text;
                                    ExcelSheet.Cells[2 + i, 10] = item.SubItems[8].Text;
                                }



                                ////////////////////////////////////////////////////////////////
                                ExcelBook.SaveAs(saveFileDialog1.FileName);


                                ExcelBook.Close();
                                ExcelApp.Quit();

                                ReleaseExceObject(ExcelSheet);
                                ReleaseExceObject(ExcelBook);
                                ReleaseExceObject(ExcelApp);
                            }

                        }
                        else
                        {
                            MessageBox.Show("통계적 시험을 완료한 후에, FMEA 파일을 출력할 수 있습니다");
                        }
            */

        }

        public static void ReleaseExceObject(object obj)
        {
            try
            {
                if (obj != null)
                {
                    Marshal.ReleaseComObject(obj);
                    obj = null;
                }
            }
            catch (Exception ex)
            {
                obj = null;
                throw ex;
            }
            finally
            {
                GC.Collect();
            }


        }

        private void button4_Click(object sender, EventArgs e)
        {

            // [2월 21일] SFI 시험이 완료되었다는 플래그 변수를 설정함
            // FMEA Excel 파일을 생성하기 위한 플래그 변수 임.
            C_complete_SFI = true;

            int[] fault_t_DATA;

            // Statistical fault injection 시작 버튼
            if (Convert.ToDecimal(textBox1.Text) == 0)
            {

            }
            else
            {
                // 정해진 시험횟수 결과를 저장할 수 있는 구조체 공간을 확보한다.
                statistical_FI_DB = new STATIS_Fault_SET[Convert.ToInt32(textBox1.Text)];
                //statistical_FI_DB = new STATIS_Fault_SET[100];

                // 시험결과를 저정할 데이터 구조체의 공간을 확보합니다.
                SFI_Failure_INFO = new Failure_Sim_Result_INFO[Convert.ToInt32(textBox1.Text)];


                // SFI 시뮬레이션 결과를 수집하기 위한 데이터 공간 할당
                for (int i = 0; i < Convert.ToInt32(textBox1.Text); i++)
                {
                    statistical_FI_DB[i].sti_fault_result_SET = new double[10000];  // 수집할 신호 또는 블록의 데이터 샘플 수
                    statistical_FI_DB[i].fault_result_SIZE = 0;



                }

                fault_t_DATA = new int[Convert.ToInt32(textBox1.Text)];

                // SFI 시뮬레이션 수행 > SFI로 계산된 시험횟수 만큼 시뮬레이션 수행하고,
                // statistcal_FI_DB 에 수집데이터를 저장한다.
                for (int i = 0; i < Convert.ToInt32(textBox1.Text); i++)
                {
                    // 결함주입 시뮬레이션을 수행함; 시뮬레이션 결과가 문자열로 저장됨
                    string str = Run_fault_Simultion();

                    //문자열로 저장된 시뮬레이션 결과를 double 형 배열로 변환
                    statistical_FI_DB[i].sti_fault_result_SET = string_to_double_set(str, ref statistical_FI_DB[i].fault_result_SIZE);

                    // 시뮬레이션 결과를 그래프로 표식
                    //drow_chart(ref fault_result_SET, ref fault_result_SIZE, 2);
                    // chart 에 시뮬레이션 결과를 그려준다.

                    if (rule_set[0].rule_type == "1")
                    {

                        Continuous_error_range_check(ref statistical_FI_DB[i].sti_fault_result_SET, ref statistical_FI_DB[i].fault_result_SIZE, ref SFI_Failure_INFO[i]);
                    }
                    else if (rule_set[0].rule_type == "2")
                    {
                        // (3월 19일)   
                        Check_specific_time_data(ref statistical_FI_DB[i].sti_fault_result_SET, ref statistical_FI_DB[i].fault_result_SIZE, ref SFI_Failure_INFO[i]);
                    }
                    else
                    {

                    }



                    // 분석 완료된 결과물을 리스트 박스에 출력한다.
                    // 검출도는 미구현 10, 발생빈도(미구현)는 비율에 따라 계산된다.(현재는 10)


                    String[] FMEA_set = new String[10];

                    FMEA_set[0] = (i + 1).ToString();  // ""순번
                    FMEA_set[1] = Failure_MODE;  // ""고장모드
                    FMEA_set[2] = Failure_EFFECT;  // ""고장영향
                    FMEA_set[3] = SFI_Failure_INFO[i].Severity.ToString(); //"심각도";  // ""심각도
                    FMEA_set[4] = "계산중";  // ""발생빈도
                    FMEA_set[5] = "계산중";  // ""검출도
                    FMEA_set[6] = "계산중";  // ""RPN
                    FMEA_set[7] = SFI_Failure_INFO[i].AVG_Failure_Value.ToString();
                    FMEA_set[8] = SFI_Failure_INFO[i].Max_Failure_Value.ToString();
                    FMEA_set[9] = fault_injection_time.ToString();                              // 고장 시간

                    fault_t_DATA[i] = fault_injection_time;

                    ListViewItem lvi = new ListViewItem(FMEA_set);

                    listView4.Items.Add(lvi);


                    SFI_Failure_INFO[i].Occurrence = 10;
                    SFI_Failure_INFO[i].Detection = 10;

                }

                AVG_failure_data_FMEA = SUM_failure_data_FMEA / Convert.ToInt32(textBox1.Text);

                listView4.Items.Clear();


                for (int i = 0; i < Convert.ToInt32(textBox1.Text); i++)
                {
                    // 리스트 박스에 해당 정보 업데이트

                    SFI_Failure_INFO[i].RPN = SFI_Failure_INFO[i].Severity * SFI_Failure_INFO[i].Occurrence * SFI_Failure_INFO[i].Detection;

                    String[] FMEA_set = new String[10];

                    FMEA_set[0] = (i + 1).ToString();  // ""순번
                    FMEA_set[1] = Failure_MODE;  // ""고장모드
                    FMEA_set[2] = Failure_EFFECT;  // ""고장영향
                    FMEA_set[3] = SFI_Failure_INFO[i].Severity.ToString(); //"심각도";  // ""심각도
                    FMEA_set[4] = SFI_Failure_INFO[i].Occurrence.ToString();  // ""발생빈도
                    FMEA_set[5] = SFI_Failure_INFO[i].Detection.ToString();  // ""검출도
                    FMEA_set[6] = SFI_Failure_INFO[i].RPN.ToString();  // ""RPN
                    FMEA_set[7] = SFI_Failure_INFO[i].AVG_Failure_Value.ToString();
                    FMEA_set[8] = SFI_Failure_INFO[i].Max_Failure_Value.ToString();
                    FMEA_set[9] = fault_t_DATA[i].ToString();

                    ListViewItem lvi = new ListViewItem(FMEA_set);

                    listView4.Items.Add(lvi);

                }

                MessageBox.Show("시험결과 리포트 \n 평균 고장 값 " + AVG_failure_data_FMEA.ToString() +
                    "\n 최대 고장 값 " + MAX_failure_data_FMEA.ToString());


            }


        }

        private void listView4_SelectedIndexChanged(object sender, EventArgs e)
        {
            //ListView lv = sender as ListView;
            //lv.FullRowSelect = true;
            int SelectRow = listView4.FocusedItem.Index;

            drow_chart(ref statistical_FI_DB[SelectRow].sti_fault_result_SET, ref statistical_FI_DB[SelectRow].fault_result_SIZE, 2);


        }

        private void Check_specific_time_data(ref double[] fault_SET, ref int fault_SIZE, ref Failure_Sim_Result_INFO Result)
        {

            int temp_severity = 0;
            // 결함주입 시험결과 데이터에서 분석 구간의 데이터 를 수집하고 평균을 구한다

            double data_S = 0;
            int data_i = 0;
            for (int i = time_table[rule_set[0].time_L]; i < time_table[rule_set[0].time_H]; i++)
            {
                data_S += fault_SET[i];
                data_i++;
            }

            double F_specific_data = data_S / data_i;


            // 산출된 평균 값은 각 규칙의 심각도 수준에 의해 가장 높은 심각도 값으로 결정한다

            for (int i = 0; i < rule_set_num; i++)
            {
                if (rule_set[i].range_L <= F_specific_data && rule_set[i].range_H > F_specific_data)
                {
                    if (temp_severity <= Convert.ToInt32(rule_set[i].rule_severity))
                    {
                        temp_severity = Convert.ToInt32(rule_set[i].rule_severity);
                    }

                }

            }

            Result.Severity = temp_severity;

            // golden run에서의 평균값과 산출된 평균 값의 편차를 기록한다.

            Result.AVG_Failure_Value = Math.Abs(F_specific_data - specific_time_data);


            // 각 시험 결과의 평균과 최대 값을 기록한다



            // (3월 13일)
            if (MAX_failure_data_FMEA < Result.AVG_Failure_Value)
            {
                MAX_failure_data_FMEA = Result.AVG_Failure_Value;
            }

            SUM_failure_data_FMEA += Result.AVG_Failure_Value;




        }

        private void Continuous_error_range_check(ref double[] fault_SET, ref int fault_SIZE, ref Failure_Sim_Result_INFO Result)
        {
            // golden run의 시험정보와 fault injection 시험정보 결과를 비교해서
            // 고장의 크기, 고장빈도, 고장지속시간 정보를 산출한다
            // 1차적으로는 위 3가지 정보를 도출하고 차후에 추가한다.

            // 1. 데이터 초기화

            Failure_INFO.Max_Failure_Duration = 0;
            Failure_INFO.Max_Failure_Value = 0;
            Failure_INFO.Failure_NUM = 0;
            Failure_INFO.Severity = 0;
            int temp_severity = 0;

            double temp_failure_value = 0;
            double temp_Max_failure_value = 0;
            double temp_Sum_failure_value = 0;
            int count_failure = 0;

            bool set_state_F = false;

            for (int i = 1; (i < golden_result_SIZE && i < fault_SIZE); i++)
            {
                if (golden_result_SET[i] != fault_SET[i])
                {
                    set_state_F = true;
                }
                else
                {
                    set_state_F = false;
                }



                if (set_state_F == true)
                {
                    temp_failure_value = Math.Abs(golden_result_SET[i] - fault_SET[i]);
                    temp_Sum_failure_value += temp_failure_value; count_failure++;

                    if (temp_Max_failure_value < temp_failure_value)
                    {
                        temp_Max_failure_value = temp_failure_value;

                    }

                    // 고장범위 테이블을 조회하여, 시스템 운용중 발생하는 오차 범위에 다른 심각도 판정을 결정한다.
                    for (int k = 0; k < rule_set_num; k++)
                    {
                        if (rule_set[k].range_H != 100)
                        {
                            if (rule_result_table[k].rule_result_set_L[i] <= Math.Abs(fault_SET[i] - golden_result_SET[i]) &&
                                rule_result_table[k].rule_result_set_H[i] > Math.Abs(fault_SET[i] - golden_result_SET[i]))
                            {
                                if (temp_severity <= Convert.ToInt32(rule_set[k].rule_severity))
                                {
                                    temp_severity = Convert.ToInt32(rule_set[k].rule_severity);
                                }
                            }
                        }
                        else
                        {
                            if (rule_result_table[k].rule_result_set_L[i] <= Math.Abs(fault_SET[i] - golden_result_SET[i]))
                            {
                                if (temp_severity <= Convert.ToInt32(rule_set[k].rule_severity))
                                {
                                    temp_severity = Convert.ToInt32(rule_set[k].rule_severity);
                                }
                            }
                        }
                    }
                }
            }



            //본 시험의 고장 최대값을 기록한다.
            Failure_INFO.Max_Failure_Value = temp_Max_failure_value;
            //본 시험의 평균 고장 값을 기록한다.
            Failure_INFO.AVG_Failure_Value = temp_Sum_failure_value / count_failure;

            // (3월 13일)
            if (MAX_failure_data_FMEA < Failure_INFO.Max_Failure_Value)
            {
                MAX_failure_data_FMEA = Failure_INFO.Max_Failure_Value;
            }

            SUM_failure_data_FMEA += Failure_INFO.Max_Failure_Value;


            // 최종 심각도가 확정       (statistical fault injection을 수행하기 위해서 Failure_INFO를 
            // 배열 형태로 설정해야 함(today)

            Failure_INFO.Severity = temp_severity;

            // 분석 결과를 결과 구조체 변수 레퍼런스에 저장
            Result = Failure_INFO;


        }

        private void button5_Click(object sender, EventArgs e)
        {
            // (3월 18일) 고장 판정을 위한 분석 테이블 및 기타 분석 자료를 생성한다.

            //rule_set[rule_set_num].rule_type = sender[0].ToString();

            if (rule_set[0].rule_type == "1")
            {
                rule_result_table = new RULE_RESUET[rule_set_num];

                for (int x = 0; x < rule_set_num; x++)
                {
                    rule_result_table[x].rule_result_size = golden_result_SIZE;

                    rule_result_table[x].rule_result_set_H = new double[rule_result_table[x].rule_result_size];
                    rule_result_table[x].rule_result_set_L = new double[rule_result_table[x].rule_result_size];
                }

                for (int i = 0; i < rule_set_num; i++)
                {
                    for (int j = 0; j < rule_result_table[i].rule_result_size; j++)
                    {

                        rule_result_table[i].rule_result_set_L[j] = (golden_result_SET[j] * rule_set[i].range_L * 0.01);
                        rule_result_table[i].rule_result_set_H[j] = (golden_result_SET[j] * rule_set[i].range_H * 0.01);

                    }
                }
            }
            else if (rule_set[0].rule_type == "2")
            {
                // 선택된 시간 구간의 포인트와 golden run에서의 평균 값을 산출한다.
                int t_interval = (int)(golden_result_SIZE / Convert.ToInt32(rule_set[0].rule_sim_time));
                time_table = new int[t_interval];

                time_table[0] = 1;

                for (int i = 1; i < t_interval; i++)
                {
                    time_table[i] = 1 + (t_interval * i);
                }

                // golden run에서의 평균 값은 ... (여기서는 시간 은 기준선이므로 0, 3번 분석은 for 문으로 0 ... n까지 돌려야 한다(rule_set_num)
                double data_S = 0;
                int data_i = 0;
                for (int i = time_table[rule_set[0].time_L]; i < time_table[rule_set[0].time_H]; i++)
                {
                    data_S += golden_result_SET[i];
                    data_i++;
                }

                specific_time_data = data_S / data_i;

                MessageBox.Show(specific_time_data.ToString());



            }
            else
            {

            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Severity_Rule_Editor dlg = new Severity_Rule_Editor();

            dlg.FormRuleADDEvent += new Severity_Rule_Editor.FormSendDataHandler(ADD_Rule_Function);

            dlg.ShowDialog();

            //SEV_SET = true;
        }

        private void ADD_Rule_Function(string[] sender)
        {
            //new string[] {rule_type, rule_f_time, rule_range_val, rule_specify_val, rule_severity, rule_sim_time };

            rule_set[rule_set_num].rule_type = sender[0].ToString();
            rule_set[rule_set_num].rule_f_time = sender[1].ToString();
            rule_set[rule_set_num].rule_range_val = sender[2].ToString();
            rule_set[rule_set_num].rule_specify_val = sender[3].ToString();
            rule_set[rule_set_num].rule_severity = sender[4].ToString();
            rule_set[rule_set_num].rule_sim_time = sender[5].ToString();


            // "지속적 오차범위 검사" 의 경우 범위 검사를 위해 "range_H"와 "range_L"를 설정한다.
            if (rule_set[rule_set_num].rule_type == "1")        // (전 임무 수행기간 중 얼마만큼 안정적인 임무를 수행하는지 판단하는 용도로 사용가능)
            {
                if (rule_set[rule_set_num].rule_range_val.Contains("+") == true)
                {
                    // range L만 있고 H는 없다.
                    char[] delimiterChars = { '+' };
                    string[] t_words = rule_set[rule_set_num].rule_range_val.Split(delimiterChars);
                    rule_set[rule_set_num].range_L = Convert.ToInt32(t_words[0]);

                    rule_set[rule_set_num].range_H = 100;
                }
                else if (rule_set[rule_set_num].rule_range_val.Contains("-") == true)
                {
                    char[] delimiterChars = { '-' };
                    string[] t_words = rule_set[rule_set_num].rule_range_val.Split(delimiterChars);
                    rule_set[rule_set_num].range_L = Convert.ToInt32(t_words[0]);
                    rule_set[rule_set_num].range_H = Convert.ToInt32(t_words[1]);
                }
                else
                {

                }
            }
            else if (rule_set[rule_set_num].rule_type == "2")
            {
                // 특정 시간의 값 범위 확인에 의한 고장 판정 ..(특정 시간까지 임무(감속)를 완수 할 수 있는지를 판단하는 용도로 사용 가능)
                char[] delimiterChars = { '-' };
                string[] t_words = rule_set[rule_set_num].rule_f_time.Split(delimiterChars);
                rule_set[rule_set_num].time_L = Convert.ToInt32(t_words[0]);
                rule_set[rule_set_num].time_H = Convert.ToInt32(t_words[1]);

                string[] t_words2 = rule_set[rule_set_num].rule_specify_val.Split(delimiterChars);
                rule_set[rule_set_num].range_L = Convert.ToInt32(t_words2[0]);
                rule_set[rule_set_num].range_H = Convert.ToInt32(t_words2[1]);



            }
            else if (rule_set[rule_set_num].rule_type == "3")
            {
                // 특정 임무(값) 이 완수되는 시간 구간에 의한 고장 판정..(해당 임무를 수행하는데 소요되는 시간을 판단하는 용도로 사용 가능)



            }
            else
            {

            }


            rule_set_num++;

            ListViewItem lvi = new ListViewItem(sender);
            listView6.Items.Add(lvi);

        }

        private void ADD_Occur_Function(string[] sender)
        {
            // Occur 구조체 정의

            // Occur 구조체 선언
            //Occurence_SET, Occurence_SET_NUM

            // Occur 구조체 자료 할당
            Occurence_SET[Occurence_SET_NUM].SFI_Occur_Rate = sender[0].ToString() + "%";
            Occurence_SET[Occurence_SET_NUM].Occurence = sender[1].ToString();

            Occurence_SET_NUM++;

            // Occur 자료 리스트 추가 (사용자 확인)

            ListViewItem lvi = new ListViewItem(sender);
            listView7.Items.Add(lvi);

        }

        private void button7_Click(object sender, EventArgs e)
        {
            // 발생도를 편집할 수 있는 다이알로그를 호출합니다.
            Occurrence_Editor dlg = new Occurrence_Editor();

            // 데이터를 받아오기 위한 이벤트 추가
            dlg.FormOccurenceADDEvent += new Occurrence_Editor.FormSendDataHandler(ADD_Occur_Function);
            //dlg.FormRuleADDEvent += new Severity_Rule_Editor.FormSendDataHandler(ADD_Rule_Function);

            dlg.ShowDialog();

            //OCC_SET = true;


        }

        private void listView8_Click(object sender, EventArgs e)
        {

            int indexnum;
            indexnum = listView8.FocusedItem.Index;
            string test = listView8.Items[indexnum].SubItems[0].Text;

            Sel_Data_Loggin_ID = Int32.Parse(test);


            // 고장영향을 분석한 위치를 선택하는 아이템 클릭 이벤트
            EventHandler eh = new EventHandler(MenuClick);
            MenuItem[] ami = {
                    new MenuItem("출력결과 확인",eh),
                    new MenuItem("고장분석 위치",eh),
                    new MenuItem("-",eh),
                    new MenuItem("(미)상세정보",eh),
                };
            ContextMenu = new System.Windows.Forms.ContextMenu(ami);

        }

        private void button8_Click(object sender, EventArgs e)
        {
            // 이미 만들어진 결함주입 모델을 불러오는 기능을 수행함
            MDL_File_Path = ShowFileOpenDialog();               // 파일 오픈 다이알로그 생성: 선택된 파일의 경로와 이름 
            fault_file_path = MDL_File_Path;

            // 결함주입용 Simulink 모델의 파일이름을 저장함.
            char[] delimiterChars = { '\\' };
            string[] t_words = fault_file_path.Split(delimiterChars);
            Fault_Model_Name = t_words[t_words.Length - 1];
            fault_file_path = fault_file_path.Replace(Fault_Model_Name, "");
            Fault_Model_Name = Fault_Model_Name.Replace(".mdl", "");


        }

        private void button9_Click(object sender, EventArgs e)
        {

            SUM_failure_data_FMEA = 0;
            AVG_failure_data_FMEA = 0;
            MAX_failure_data_FMEA = 0;


            for (int i = listView4.Items.Count; i > 0; i--)
            {
                listView4.Items[i - 1].Remove();
            }
            listView4.Refresh();
        }


        private void Full_SET_Click(object sender, EventArgs e)
        {

            //1. Block 중 top level에 있는 모든 블록에 결함주입 모듈을 모두 추가한다
            // 수정 작업
            for (int i = 0; i < Block_DB_count; i++)
            {
                //if (Block_DB[i].Parent_Block == "TOP LEVEL")
               // {
                    if (Block_DB[i].BlockType == "Constant" || Block_DB[i].BlockType == "BusCreator"
                         || Block_DB[i].BlockType == "SubSystem" || Block_DB[i].BlockType == "MultiPortSwitch" || Block_DB[i].BlockType == "Inport"
                         || Block_DB[i].BlockType == "FromWorkspace" || Block_DB[i].BlockType == "InportShadow" || Block_DB[i].BlockType == "Switch"
                         || Block_DB[i].BlockType == "BusSelector")
                    {

                    }
                    else
                    {
                        
                        char[] delimiterChars = { '"' };
                        string[] t_words = Block_DB[i].Name.Split(delimiterChars);
                        string block_name ="";

                        for(int a=0; a < t_words.Count(); a++)
                        {
                            if(t_words[a] != "")
                            {
                                block_name = t_words[a];
                            }
                        }


                        full_fault_list[full_fault_list_count].Block_Data = Block_DB[i];

                    full_fault_list[full_fault_list_count].fault_block_name = "FAULT_" + block_name;//Block_DB[i].Name;

                    char[] delimiterpath = { '@', '"' };
                    string[] path_words = Block_DB[i].Parent_Block.Split(delimiterpath);
                    string[] path_check_words = new string[10];
                    int path_check_words_count = 0;

                    for(int loop = 0; loop < path_words.Count(); loop++)
                    {
                        if(path_words[loop] != "TOP LEVEL" && path_words[loop] !="")
                        {
                            path_check_words[path_check_words_count++] = path_words[loop];
                        }
                    }
                    string full_path = "";

                    for (int loop = 0; loop < path_check_words_count; loop++)
                    {
                        full_path += path_check_words[loop]+ "/";
                    }

                     full_fault_list[full_fault_list_count].full_path_fault_block_name
                         = full_path + "FAULT_" + block_name;//Block_DB[i].Name;    

                        // dstport 개수 만큼 결함주입 모듈 설정
                        full_fault_list[full_fault_list_count].num_dstport = Block_DB[i].dst_port_num;

                        full_fault_list[full_fault_list_count++].set_injected = false;
                    }

                //}
            }

            // 2. 파일을 하나 만든다.
            // 파일을 저장하기 위한 다이알로그를 생성
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "MDL 파일 (*.mdl) | *.mdl; | 모든 파일 (*.*) | *.*";
            saveFileDialog1.Title = "MDL File Save";
            saveFileDialog1.ShowDialog();


            //  다이알로그에서 저장할 파일명이 입력 되었음 (MDL 파일 SAVE 파일명이 null이 아니면...)
            if (saveFileDialog1.FileName != "")
            {
                //Fault_Model_Name;


                // 생성할 결함주입용 simulink 모델의 파일경로를 저장함
                fault_file_path = saveFileDialog1.FileName;
                string t_fault_file_path = fault_file_path;

                // 결함주입용 Simulink 모델의 파일이름을 저장함.
                char[] delimiterChars = { '\\' };
                string[] t_words = fault_file_path.Split(delimiterChars);
                Fault_Model_Name = t_words[t_words.Length - 1];
                fault_file_path = fault_file_path.Replace(Fault_Model_Name, "");
                Fault_Model_Name = Fault_Model_Name.Replace(".mdl", "");

                // 생성된 파일의 원본 자료를 copy하여, 모델파일을 생성한다
                System.IO.File.Copy(golden_file_path, t_fault_file_path);

                // 복사된 파일에 수정할 위치를 찾기 위해, 일단 읽기용으로 파일을 연다
                r_FI_MDL_File = new StreamReader(t_fault_file_path);

                // 파일에서 수정 위치를 찾고, 문자열을 추가하기 위한 StringBuilder 객체 사용
                var sb = new StringBuilder();

                // 파일을 라인별로 읽어서 고장주입 블록이 추가될 subsystem 을 찾는다.
                // 고장주입 블록추가될 위치가 확인되면,, loop 문을 빠져 나온다.
                int depth_find = Fault_SET.depth;

                // 수정 코드(파일 탐색에 필요한 변수 선언)
                string str;
                int fault_list_index = 0;
                bool set_BlockType = false;
                bool start_block_set = false;
                bool set_BlockName = false;
                bool set_Blockfault = false;

                int current_depth = 0;
                int F_SID = 999;        // fault block이 추가될 때마다 1씩 감소


                // 결함 주입 블록을 추가하기 위한 파일 탐색
                while ((str = r_FI_MDL_File.ReadLine()) != null)
                {
                    // 3. 시스템 전까지 파일을 모두 카피한다.

                    if (start_block_set == false)           //MDL 파일의 분석 시작 위치 .. 모델 블록 시작
                    {
                        if (str.Contains("System") == true && str.Contains("{"))
                        {
                            start_block_set = true;
                            current_depth = 1;
                        }
                    }
                    else
                    {
                        if (str.Contains("{"))
                        {
                            current_depth++;    // 결함이 주입될 계층을 지시하는 변수
                        }
                        else if (str.Contains("}"))
                        {
                            current_depth--;
                        }
                        else
                        {

                        }
                    }

                    if (start_block_set == true)
                    {
                        // 리스트의 블록 타입과 일치하
                        if (str.Contains("BlockType") == true && str.Contains(full_fault_list[fault_list_index].Block_Data.BlockType))
                        {
                            set_BlockType = true;
                        }

                        if (str.Contains("Name") == true && str.Contains(full_fault_list[fault_list_index].Block_Data.Name))
                        {
                            set_BlockName = true;
                        }

                        if (set_BlockName == true && set_BlockType == true)
                        {
                            set_Blockfault = true;

                            // Generation_Fault_Block(string SID, string a, string b, string c, string d);
                            int a = full_fault_list[fault_list_index].Block_Data.Position.X_pos - 20;
                            int b = full_fault_list[fault_list_index].Block_Data.Position.Y_pos;
                            int c = full_fault_list[fault_list_index].Block_Data.Position.X_pos - 10;
                            int d = full_fault_list[fault_list_index].Block_Data.Position.Y_hight;

                            Generation_Fault_Block(full_fault_list[fault_list_index].fault_block_name,
                                F_SID.ToString(), a.ToString(), b.ToString(), c.ToString(), d.ToString());

                            F_SID--;
                        }
                    }

                    if (set_Blockfault == true)
                    {
                        if (str.Contains("}") && (current_depth == 1 || current_depth == 3 || current_depth == 5 ||
                            current_depth == 7 || current_depth == 9 || current_depth == 11))        // top level 1인경우, 
                        {
                            // 결함모델 주입
                            set_Blockfault = false;
                            set_BlockName = false;
                            set_BlockType = false;

                            sb.Append(str); // 앞부분을 모두 저장
                            sb.Append("\n");

                            // 결함 추가
                            sb.Append(fault_module); // 앞부분을 모두 저장

                            // 같은 블록에 2개 이상의 결함주입 볼록이 추가되는 경우 
                            for(int index = 2; index <= full_fault_list[fault_list_index].num_dstport; index++)
                            {
                                // Generation_Fault_Block(string SID, string a, string b, string c, string d);
                                int a = full_fault_list[fault_list_index].Block_Data.Position.X_pos - 20;
                                int b = full_fault_list[fault_list_index].Block_Data.Position.Y_pos + 30;
                                int c = full_fault_list[fault_list_index].Block_Data.Position.X_pos - 10;
                                int d = full_fault_list[fault_list_index].Block_Data.Position.Y_hight + 30;

                                Generation_Fault_Block(full_fault_list[fault_list_index].fault_block_name + "_" + index.ToString(),
                                    F_SID.ToString(), a.ToString(), b.ToString(), c.ToString(), d.ToString());

                                F_SID--;

                                sb.Append(fault_module); // 앞부분을 모두 저장
                            }

                            fault_list_index++;



                            if (full_fault_list_count == fault_list_index)
                            {
                                break;
                            }
                        }
                        else
                        {
                            // 추가적인 텍스트 없이, 기존 문장을 저장한다.
                            sb.Append(str); // 앞부분을 모두 저장
                            sb.Append("\n");
                        }
                    }
                    else
                    {
                        // 추가적인 텍스트 없이, 기존 문장을 저장한다.
                        sb.Append(str); // 앞부분을 모두 저장
                        sb.Append("\n");
                    }

                }


                // 결함주입 블록을 모두 추가하고, 나머지 텍스트를 복사해 오는 기능

                while ((str = r_FI_MDL_File.ReadLine()) != null)
                {
                    sb.Append(str); // 앞부분을 모두 저장
                    sb.Append("\n");
                }

                r_FI_MDL_File.Close();                                                        // 읽기용 파일 스트림 닫고, 
                w_FI_MDL_File = new StreamWriter(t_fault_file_path);    // 쓰기용 파일 스트림 열고
                w_FI_MDL_File.Write(sb.ToString());                                 // 결함 블록이 추가된 문자열 자료들을 파일에 저장
                                                                                    // 결함주입 모듈과 line이 추가된 모델을 저장함
                w_FI_MDL_File.Close();                                                          // 쓰기용 파일 스트림 닫고 종료

                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                // 8. 라인은 ??
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                ///
                r_FI_MDL_File = new StreamReader(t_fault_file_path);

                current_depth = 0;
                start_block_set = false;
                bool set_Line = false;
                string t_DstBlock = "a";
                string t_f_DstBlock = "a";
                string t_SrcBlock = "a";
                bool set_DstBlock = false;
                bool set_SrcBlock = false;
                bool set_DstPort = false;
                bool set_update_DstBlock = false;
                bool readty_New_LINE = false;
                string buffer_port = "a";
                string buffer_DstBlock = "a";
                bool set_branch = false;
                int num_branch = 0;

                bool add_new_line = false;

                string [] str_t_f_branch = new string[10];  // 1버전은 10개의 분기 모듈 처리 가능함
                string [] str_t_branch = new string[10];  // 1버전은 10개의 분기 모듈 처리 가능함
                string[] str_t_branch_port = new string[10];

                var sb_line = new StringBuilder();

                while ((str = r_FI_MDL_File.ReadLine()) != null)
                {

                    if (start_block_set == false)           //MDL 파일의 분석 시작 위치 .. 모델 블록 시작
                    {
                        if (str.Contains("System") == true && str.Contains("{"))
                        {
                            start_block_set = true;
                            current_depth = 1;
                        }
                    }
                    else
                    {
                        if (str.Contains("{"))
                        {
                            current_depth++;    // 결함이 주입될 계층을 지시하는 변수
                        }
                        else if (str.Contains("}"))
                        {
                            current_depth--;
                        }
                        else
                        {

                        }
                    }

                    /////// top level의 Line 문 도달 했는지 확인한다. !!!!
                    if (start_block_set == true)
                    {
                        if (str.Contains("Line") == true && (current_depth == 2 || current_depth == 4 || current_depth == 6 || current_depth == 8
                            || current_depth == 10 || current_depth == 12) && str.Contains("{") == true)
                        {
                            set_Line = true;        // Line 구문에 들어온 상태
                        }

                        if(set_Line == true && (current_depth == 1 || current_depth == 3 || current_depth == 5 || current_depth == 7
                            || current_depth == 9 || current_depth == 11 || current_depth == 13) && str.Contains("}") == true)
                        {
                            set_Line = false;                   // Line 구문 종료
                            if(add_new_line == true)
                            {
                                readty_New_LINE = true;
                                add_new_line = false;
                            //  set_update_DstBlock = false;
                            }
                        }
                         
                        // branch 가 있는 경우 조건

                        if(set_Line == true && str.Contains("DstBlock") == true)
                        {
                            set_DstBlock = true;

                            if(set_branch == true)
                            {
                                num_branch++;
                            }
                        }

                        if(set_Line == true && str.Contains("SrcBlock") == true)
                        {
                            set_SrcBlock = true;
                        }

                        if (set_Line == true && str.Contains("DstPort") == true)
                        {
                            set_DstPort = true;
                        }

                        if (set_Line == true && str.Contains("Branch") == true)
                        {
                            set_branch = true;
                        }

                       // if(set_DstBlock == true && str.Contains("DstBlock") != true)
                       // {
                       //    set_DstBlock = false;
                       // }
                    }


                    //////////////////////////////////위치를 찾으면 문자열 업데이트....
                    ///
                    if (set_DstBlock == true)
                    {
                        // 수정할 Line의 DstBlock 구문에 도착함, 신호 이름 앞에 접두사 "FAULT_"를 추가한다.
                        char[] delimiterDstBlock = { '"' };
                        string[] t_delimiter_DstBlock = str.Split(delimiterDstBlock);

                        t_DstBlock = t_delimiter_DstBlock[1];
                        t_f_DstBlock = "FAULT_" + t_delimiter_DstBlock[1];

                        for(int t_num = 0; t_num < full_fault_list_count; t_num++)
                        {
                            // 결함 블록을 생성한 라인만 수정을 한다(2019 년 11일 수정필요함)
                            if(t_f_DstBlock == full_fault_list[t_num].fault_block_name) // && full_fault_list[t_num].Block_Data.Name == t_SrcBlock)
                            //if ( full_fault_list[t_num].fault_block_name.Contains(t_f_DstBlock) == true) // && full_fault_list[t_num].Block_Data.Name == t_SrcBlock)
                            {
                                //sb_line.Append("      DstBlock		      \"" + t_f_DstBlock + "\"");
                                //sb_line.Append("\n");
                                buffer_DstBlock = "      DstBlock		      \"" + t_f_DstBlock;

                                set_DstBlock = false;
                                set_update_DstBlock = true;         // DstBlock에 접두사 "FAULT_"를 추가하였음
                                add_new_line = true;
                                // branch 작업

                                if (set_branch == true)
                                { 
                                    str_t_f_branch[num_branch - 1] = t_f_DstBlock;
                                    str_t_branch[num_branch - 1] = t_DstBlock;
                                }
                            }
                        }
               
                        if(set_update_DstBlock != true)
                        {
                            set_DstBlock = false;
                            sb_line.Append(str);
                            sb_line.Append("\n");
                        }

                        //if (set_update_DstBlock == true)
                        //{
                        //    set_DstBlock = false;
                        //    buffer_DstBlock = str;
                        //}


                    }
                    else if(set_SrcBlock == true)
                    {
                        //t_SrcBlock
                        char[] delimiterSrcBlock = { '"' };
                        string[] t_delimiter_SrcBlock = str.Split(delimiterSrcBlock);

                        t_SrcBlock = t_delimiter_SrcBlock[1];

                        set_SrcBlock = false;

                        sb_line.Append(str); // 앞부분을 모두 저장
                        sb_line.Append("\n");

                    }
                    else if(set_DstPort == true)
                    {       // 포트의 개수를 설정하는 구문  현재는 5개까지 지원할 수 있도록
                        if(set_update_DstBlock == true)
                        {
                            if(str.Contains("1") == true)       
                            {
                                buffer_port = "1";
                                sb_line.Append(buffer_DstBlock + "\"\n");

                                sb_line.Append(str); // 앞부분을 모두 저장
                                sb_line.Append("\n");

                                if (set_branch == true)
                                    str_t_branch_port[num_branch - 1] = "1";
                            }
                            else if (str.Contains("2") == true)
                            {
                                buffer_port = "2";
                                buffer_DstBlock += "_2\"";
                                sb_line.Append(buffer_DstBlock + "\n");
                                sb_line.Append("DstPort		      1\n");

                                if (set_branch == true)
                                    str_t_branch_port[num_branch - 1] = "2";
                            }
                            else if (str.Contains("3") == true)
                            {
                                buffer_port = "3";
                                buffer_DstBlock += "_3\"";
                                sb_line.Append(buffer_DstBlock + "\n");
                                sb_line.Append("DstPort		      1\n");

                                if (set_branch == true)
                                    str_t_branch_port[num_branch - 1] = "3";
                            }
                            else if (str.Contains("4") == true)
                            {
                                buffer_port = "4";
                                buffer_DstBlock += "_4\"";
                                sb_line.Append(buffer_DstBlock + "\n");
                                sb_line.Append("DstPort		      1\n");

                                if (set_branch == true)
                                    str_t_branch_port[num_branch - 1] = "4";
                            }
                            else if (str.Contains("5") == true)
                            {
                                buffer_port = "5";
                                buffer_DstBlock += "_5\"";
                                sb_line.Append(buffer_DstBlock + "\n");
                                sb_line.Append("DstPort		      1\n");

                                if (set_branch == true)
                                    str_t_branch_port[num_branch - 1] = "5";
                            }
                            else
                            {

                            }

                            set_update_DstBlock = false;    // Dst 업데이트 완료 

                        }
                        else
                        {
                            sb_line.Append(str); // 앞부분을 모두 저장
                            sb_line.Append("\n");
                        }

                        set_DstPort = false;
                    }
                    else if(readty_New_LINE == true)
                    {
                        // 새로운 line을 추가한다, 앞서 수정된 dest 정보를 활용하여 새로운 라인 추가
                        sb_line.Append("}\n"); // 앞부분을 모두 저장

                        if (set_branch == false)
                        { 
                            Generation_Fault_Line(t_f_DstBlock, t_DstBlock, buffer_port);
                            sb_line.Append(ADD_Line); // 앞부분을 모두 저장
                        }
                        else
                        {
                            for(int b_path= 0; b_path < num_branch; b_path++)
                            {
                                //Generation_Fault_Line(str_t_f_branch[b_path], str_t_branch[b_path], "1");
                                if(str_t_f_branch[b_path] != null && str_t_branch[b_path] != null && str_t_branch_port[b_path] != null)
                                { 
                                    Generation_Fault_Line(str_t_f_branch[b_path], str_t_branch[b_path], str_t_branch_port[b_path]);
                                    sb_line.Append(ADD_Line); // 앞부분을 모두 저장    
                                }
                            }

                            set_branch = false;
                            num_branch = 0;

                        }

                        readty_New_LINE = false;
                    }
                    else
                    {
                        sb_line.Append(str); // 앞부분을 모두 저장
                        sb_line.Append("\n");
                    }

                }

                r_FI_MDL_File.Close();                                                        // 읽기용 파일 스트림 닫고, 
                w_FI_MDL_File = new StreamWriter(t_fault_file_path);    // 쓰기용 파일 스트림 열고
                w_FI_MDL_File.Write(sb_line.ToString());                                 // 결함 블록이 추가된 문자열 자료들을 파일에 저장
                                                                                    // 결함주입 모듈과 line이 추가된 모델을 저장함
                w_FI_MDL_File.Close();                                                          // 쓰기용 파일 스트림 닫고 종료
            }
        }


        // 통계적 결함주입 시험 시나리오 리스트가 저장되어 있음
        private void Save_Scenario_Set(STATISTICAL_FI_SECNARIO_SET[] obj, int count)
        {
            SFI_secnario_num = count;

            SFI_secnario_set = new STATISTICAL_FI_SECNARIO_SET[SFI_secnario_num];

            SFI_secnario_set = obj;


        }

        private void SFI_SIM_Click(object sender, EventArgs e)
        {
            SFI_Simulation dlg;

            dlg = new SFI_Simulation(full_fault_list, full_fault_list_count);
            
            dlg.FormSaveEvent += new SFI_Simulation.FormSendDataHandler(Save_Scenario_Set);

            dlg.ShowDialog();
        }
        /*
         *     struct Fault_Mode_SET
    {
        public string Block_lib;
        public string Occur_type;
        public string F_enable;
        public string F_disable;
        public string F_duration;
        public string F_value;
    }
         * Fautl_Model_Info
         */

        private void thread_run()
        {
            int[] fault_t_DATA;

            // Statistical fault injection 시작 버튼
            if (SFI_secnario_num == 0)
            {

            }
            else
            {
                // 정해진 시험횟수 결과를 저장할 수 있는 구조체 공간을 확보한다.
                statistical_FI_DB = new STATIS_Fault_SET[SFI_secnario_num];


                // 시험결과를 저정할 데이터 구조체의 공간을 확보합니다.
                SFI_Failure_INFO = new Failure_Sim_Result_INFO[SFI_secnario_num];


                // SFI 시뮬레이션 결과를 수집하기 위한 데이터 공간 할당
                for (int i = 0; i < SFI_secnario_num; i++)
                {
                    statistical_FI_DB[i].sti_fault_result_SET = new double[10000];  // 수집할 신호 또는 블록의 데이터 샘플 수
                    statistical_FI_DB[i].fault_result_SIZE = 0;



                }

                fault_t_DATA = new int[SFI_secnario_num];

                // SFI 시뮬레이션 수행 > SFI로 계산된 시험횟수 만큼 시뮬레이션 수행하고,
                // statistcal_FI_DB 에 수집데이터를 저장한다.
                for (int i = 0; i < SFI_secnario_num; i++)
                {

                    SFI_running_num = i;      // 현재 수행중인 시험 횟수를 저장한다. 시뮬링크 스턱 방지

                    // 결함주입 시뮬레이션을 수행함; 시뮬레이션 결과가 문자열로 저장됨

                    Fautl_Model_Info.Block_lib = SFI_secnario_set[i].Block_lib;
                    Fautl_Model_Info.Occur_type = "Trasient";
                    Fautl_Model_Info.F_enable = SFI_secnario_set[i].F_enable;
                    Fautl_Model_Info.F_disable = SFI_secnario_set[i].F_disable;
                    Fautl_Model_Info.F_duration = SFI_secnario_set[i].F_duration;
                    Fautl_Model_Info.F_value = SFI_secnario_set[i].F_value;


                    string str = Run_fault_Simultion(SFI_secnario_set[i].Fault_location);


                    //문자열로 저장된 시뮬레이션 결과를 double 형 배열로 변환
                    statistical_FI_DB[i].sti_fault_result_SET = string_to_double_set(str, ref statistical_FI_DB[i].fault_result_SIZE);

                    // 시뮬레이션 결과를 그래프로 표식
                    //drow_chart(ref fault_result_SET, ref fault_result_SIZE, 2);
                    // chart 에 시뮬레이션 결과를 그려준다.

                    if (rule_set[0].rule_type == "1")
                    {

                        Continuous_error_range_check(ref statistical_FI_DB[i].sti_fault_result_SET, ref statistical_FI_DB[i].fault_result_SIZE, ref SFI_Failure_INFO[i]);
                    }
                    else if (rule_set[0].rule_type == "2")
                    {
                        // (3월 19일)   
                        Check_specific_time_data(ref statistical_FI_DB[i].sti_fault_result_SET, ref statistical_FI_DB[i].fault_result_SIZE, ref SFI_Failure_INFO[i]);
                    }
                    else
                    {

                    }



                    // 분석 완료된 결과물을 리스트 박스에 출력한다.
                    // 검출도는 미구현 10, 발생빈도(미구현)는 비율에 따라 계산된다.(현재는 10)

                    SFI_Failure_INFO[i].Occurrence = 10;
                    SFI_Failure_INFO[i].Detection = 10;
                    SFI_Failure_INFO[i].RPN = SFI_Failure_INFO[i].Severity * SFI_Failure_INFO[i].Occurrence * SFI_Failure_INFO[i].Detection;

                    String[] FMEA_set = new String[12];


                    FMEA_set[0] = (i + 1).ToString();  // ""순번
                    //FMEA_set[1] = SFI_secnario_set[i].Fault_location;  // ""고장모드

                    FMEA_set[1] = SFI_secnario_set[i].Fault_location; //
                    FMEA_set[2] = SFI_secnario_set[i].F_value;
                    FMEA_set[3] = SFI_secnario_set[i].F_duration;


                    FMEA_set[4] = Failure_EFFECT;  // ""고장영향
                    FMEA_set[5] = SFI_Failure_INFO[i].Severity.ToString(); //"심각도";  // ""심각도
                    FMEA_set[6] = SFI_Failure_INFO[i].Occurrence.ToString();  // ""발생빈도
                    FMEA_set[7] = SFI_Failure_INFO[i].Detection.ToString();  // ""검출도
                    FMEA_set[8] = SFI_Failure_INFO[i].RPN.ToString();  // ""RPN
                    FMEA_set[9] = SFI_Failure_INFO[i].AVG_Failure_Value.ToString();
                    FMEA_set[10] = SFI_Failure_INFO[i].Max_Failure_Value.ToString();
                    FMEA_set[11] = fault_injection_time.ToString();                              // 고장 시간

                    fault_t_DATA[i] = fault_injection_time;

                    WriteListSafe(FMEA_set);

                    
                }
            }
        }

        private delegate void safecalldelegate(string[] data);

        private void WriteListSafe(string [] data)
        {
            if (listView4.InvokeRequired)
            {
                var d = new safecalldelegate(WriteListSafe);
                Invoke(d, new object[] { data });
            }
            else
            {
                ListViewItem lvi = new ListViewItem(data);
                listView4.Items.Add(lvi);
            }
        }

        private void SFI_SIM_RUN_Click(object sender, EventArgs e)
        {

            C_complete_SFI = true;
            // simulink compiling stuck 현상을 해결하기 위한 스레드
       

            Thread sim_t = new Thread(new ThreadStart(thread_run));
            sim_t.Start();

            /*
                            AVG_failure_data_FMEA = SUM_failure_data_FMEA / Convert.ToInt32(textBox1.Text);

                            listView4.Items.Clear();


                            for (int i = 0; i < SFI_secnario_num; i++)
                            {
                                // 리스트 박스에 해당 정보 업데이트

                                SFI_Failure_INFO[i].RPN = SFI_Failure_INFO[i].Severity * SFI_Failure_INFO[i].Occurrence * SFI_Failure_INFO[i].Detection;

                                String[] FMEA_set = new String[12];

                                FMEA_set[0] = (i + 1).ToString();  // ""순번

                                //FMEA_set[1] = SFI_secnario_set[i].Fault_location;  // ""고장모드
                                FMEA_set[1] = SFI_secnario_set[i].Fault_location; // + "    " +  + "    " + ;
                                FMEA_set[2] = SFI_secnario_set[i].F_value;
                                FMEA_set[3] = SFI_secnario_set[i].F_duration;

                                FMEA_set[4] = Failure_EFFECT;  // ""고장영향
                                FMEA_set[5] = SFI_Failure_INFO[i].Severity.ToString(); //"심각도";  // ""심각도
                                FMEA_set[6] = SFI_Failure_INFO[i].Occurrence.ToString();  // ""발생빈도
                                FMEA_set[7] = SFI_Failure_INFO[i].Detection.ToString();  // ""검출도
                                FMEA_set[8] = SFI_Failure_INFO[i].RPN.ToString();  // ""RPN
                                FMEA_set[9] = SFI_Failure_INFO[i].AVG_Failure_Value.ToString();
                                FMEA_set[10] = SFI_Failure_INFO[i].Max_Failure_Value.ToString();
                                FMEA_set[11] = fault_t_DATA[i].ToString();

                                ListViewItem lvi = new ListViewItem(FMEA_set);

                                listView4.Items.Add(lvi);

                            }

                            MessageBox.Show("시험결과 리포트 \n 평균 고장 값 " + AVG_failure_data_FMEA.ToString() +
                                "\n 최대 고장 값 " + MAX_failure_data_FMEA.ToString());


                        }
            */
            // [2월 21일] SFI 시험이 완료되었다는 플래그 변수를 설정함
            // FMEA Excel 파일을 생성하기 위한 플래그 변수 임.


        }
    }
}