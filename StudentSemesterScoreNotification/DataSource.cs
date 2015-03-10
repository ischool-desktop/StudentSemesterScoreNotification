﻿using Campus.Report;
using FISCA.Data;
using FISCA.UDT;
using JHSchool.Behavior.BusinessLogic;
using JHSchool.Data;
using JHSchool.Evaluation.Mapping;
using K12.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace StudentSemesterScoreNotification
{
    class DataSource
    {
        private static AccessHelper _A;
        private static QueryHelper _Q;
        private static DegreeMapper _degreeMapper;

        private static List<string> _文字描述 = new List<string>();
        private static List<StudentRecord> _students;
        private static int _schoolYear, _semester;

        //DataRow catch
        private static Dictionary<string, DataRow> _RowCatchs = new Dictionary<string, DataRow>();

        /// <summary>
        /// 日常生活表現名稱對照使用
        /// </summary>
        private static Dictionary<string, string> _DLBehaviorConfigNameDict = new Dictionary<string, string>();

        /// <summary>
        /// 日常生活表現子項目名稱,呼叫GetDLBehaviorConfigNameDict 一同取得
        /// </summary>
        private static Dictionary<string, List<string>> _DLBehaviorConfigItemNameDict = new Dictionary<string, List<string>>();

        /// <summary>
        /// XML 內解析子項目名稱
        /// </summary>
        /// <param name="elm"></param>
        /// <returns></returns>
        private static List<string> ParseItems(XElement elm)
        {
            List<string> retVal = new List<string>();

            foreach (XElement subElm in elm.Elements("Item"))
            {
                // 因為社團功能，所以要將"社團活動" 字不放入
                string name = subElm.Attribute("Name").Value;
                if (name != "社團活動")
                    retVal.Add(name);
            }
            return retVal;
        }

        /// <summary>
        /// 取得日常生活表現設定名稱
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, string> GetDLBehaviorConfigNameDict()
        {
            Dictionary<string, string> retVal = new Dictionary<string, string>();
            try
            {
                _DLBehaviorConfigItemNameDict.Clear();

                // 包含新竹與高雄
                K12.Data.Configuration.ConfigData cd = K12.Data.School.Configuration["DLBehaviorConfig"];
                if (!string.IsNullOrEmpty(cd["DailyBehavior"]))
                {
                    string key = "日常行為表現";
                    //日常行為表現
                    XElement e1 = XElement.Parse(cd["DailyBehavior"]);
                    string name = e1.Attribute("Name").Value;
                    retVal.Add(key, name);

                    // 日常生活表現子項目
                    List<string> items = ParseItems(e1);
                    if (items.Count > 0)
                        _DLBehaviorConfigItemNameDict.Add(key, items);
                }

                if (!string.IsNullOrEmpty(cd["GroupActivity"]))
                {
                    string key = "團體活動表現";
                    //團體活動表現
                    XElement e4 = XElement.Parse(cd["GroupActivity"]);
                    string name = e4.Attribute("Name").Value;
                    retVal.Add(key, name);

                    // 團體活動表現
                    List<string> items = ParseItems(e4);
                    if (items.Count > 0)
                        _DLBehaviorConfigItemNameDict.Add(key, items);

                }

                if (!string.IsNullOrEmpty(cd["PublicService"]))
                {
                    string key = "公共服務表現";
                    //公共服務表現
                    XElement e5 = XElement.Parse(cd["PublicService"]);
                    string name = e5.Attribute("Name").Value;
                    retVal.Add(key, name);
                    List<string> items = ParseItems(e5);
                    if (items.Count > 0)
                        _DLBehaviorConfigItemNameDict.Add(key, items);

                }

                if (!string.IsNullOrEmpty(cd["SchoolSpecial"]))
                {
                    string key = "校內外特殊表現";
                    //校內外特殊表現,新竹沒有子項目，高雄有子項目
                    XElement e6 = XElement.Parse(cd["SchoolSpecial"]);
                    string name = e6.Attribute("Name").Value;
                    retVal.Add(key, name);
                    List<string> items = ParseItems(e6);
                    if (items.Count > 0)
                        _DLBehaviorConfigItemNameDict.Add(key, items);
                }

                if (!string.IsNullOrEmpty(cd["OtherRecommend"]))
                {
                    //其他表現
                    XElement e2 = XElement.Parse(cd["OtherRecommend"]);
                    string name = e2.Attribute("Name").Value;
                    retVal.Add("其他表現", name);
                }

                if (!string.IsNullOrEmpty(cd["DailyLifeRecommend"]))
                {
                    //日常生活表現具體建議
                    XElement e3 = XElement.Parse(cd["DailyLifeRecommend"]);
                    string name = e3.Attribute("Name").Value;
                    retVal.Add("具體建議", name);  // 高雄
                    retVal.Add("綜合評語", name);  // 新竹
                }
            }
            catch (Exception ex)
            {
                FISCA.Presentation.Controls.MsgBox.Show("日常生活表現設定檔解析失敗!" + ex.Message);
            }

            return retVal;
        }

        /// <summary>
        /// 取得一張初始化的資料表
        /// </summary>
        /// <returns></returns>
        private static DataTable GetEmptyDataTable()
        {
            _RowCatchs.Clear();
            _DLBehaviorConfigNameDict = GetDLBehaviorConfigNameDict();

            List<string> plist = K12.Data.PeriodMapping.SelectAll().Select(x => x.Type).Distinct().ToList();
            List<string> alist = K12.Data.AbsenceMapping.SelectAll().Select(x => x.Name).ToList();

            DataTable dt = new DataTable();
            dt.Columns.Add("列印日期");
            dt.Columns.Add("學校名稱");
            dt.Columns.Add("學年度");
            dt.Columns.Add("學期");
            dt.Columns.Add("姓名");
            dt.Columns.Add("班級");
            dt.Columns.Add("座號");
            dt.Columns.Add("學號");
            dt.Columns.Add("大功");
            dt.Columns.Add("小功");
            dt.Columns.Add("嘉獎");
            dt.Columns.Add("大過");
            dt.Columns.Add("小過");
            dt.Columns.Add("警告");
            dt.Columns.Add("上課天數");
            dt.Columns.Add("學習領域成績");
            dt.Columns.Add("學習領域原始成績");
            dt.Columns.Add("課程學習成績");
            dt.Columns.Add("課程學習原始成績");
            dt.Columns.Add("班導師");
            dt.Columns.Add("教務主任");
            dt.Columns.Add("校長");
            dt.Columns.Add("服務學習時數");
            dt.Columns.Add("文字描述");

            //科目欄位
            for (int i = 1; i <= Global.SupportSubjectCount; i++)
            {
                dt.Columns.Add("S科目" + i);
                dt.Columns.Add("S領域" + i);
                dt.Columns.Add("S節數" + i);
                dt.Columns.Add("S權數" + i);
                dt.Columns.Add("S等第" + i);
                dt.Columns.Add("S成績" + i);
                dt.Columns.Add("S原始成績" + i);
                dt.Columns.Add("S補考成績" + i);
            }

            //領域欄位
            for (int i = 1; i <= Global.SupportDomainCount; i++)
            {
                dt.Columns.Add("D領域" + i);
                dt.Columns.Add("D節數" + i);
                dt.Columns.Add("D權數" + i);
                dt.Columns.Add("D等第" + i);
                dt.Columns.Add("D成績" + i);
                dt.Columns.Add("D原始成績" + i);
                dt.Columns.Add("D補考成績" + i);
            }

            //假別欄位
            for (int i = 1; i <= Global.SupportAbsentCount; i++)
                dt.Columns.Add("列印假別" + i);

            //日常生活表現欄位
            foreach (string key in Global.DLBehaviorRef.Keys)
            {
                dt.Columns.Add(key + "_Name");
                dt.Columns.Add(key + "_Description");
            }

            //日常生活表現子項目欄位
            foreach (string key in _DLBehaviorConfigNameDict.Keys)
            {
                int itemIndex = 0;

                if (_DLBehaviorConfigItemNameDict.ContainsKey(key))
                {
                    foreach (string item in _DLBehaviorConfigItemNameDict[key])
                    {
                        itemIndex++;
                        dt.Columns.Add(key + "_Item_Name" + itemIndex);
                        dt.Columns.Add(key + "_Item_Degree" + itemIndex);
                        dt.Columns.Add(key + "_Item_Description" + itemIndex);
                    }
                }
            }

            //社團欄位
            for (int i = 1; i <= Global.SupportClubCount; i++)
            {
                dt.Columns.Add("社團Name" + i);
                dt.Columns.Add("社團Score" + i);
                dt.Columns.Add("社團Effort" + i);
                dt.Columns.Add("社團Text" + i);
            }

            //看ColumnName用的
            //List<string> dcs = new List<string>();

            //foreach (DataColumn dc in dt.Columns)
            //    dcs.Add(dc.ColumnName);

            return dt;
        }

        private static string SelectTime() //取得Server的時間
        {
            DataTable dtable = _Q.Select("select now()"); //取得時間
            DateTime dt = DateTime.Now;
            DateTime.TryParse("" + dtable.Rows[0][0], out dt); //Parse資料
            string ComputerSendTime = dt.ToString("yyyy/MM/dd"); //最後時間

            return ComputerSendTime;
        }

        /// <summary>
        /// 填寫DataTable的資料
        /// </summary>
        /// <param name="dt"></param>
        private static void FillData(DataTable dt)
        {
            string printDateTime = SelectTime();
            string schoolName = K12.Data.School.ChineseName;
            string 校長 = K12.Data.School.Configuration["學校資訊"].PreviousData.SelectSingleNode("ChancellorChineseName").InnerText;
            string 教務主任 = K12.Data.School.Configuration["學校資訊"].PreviousData.SelectSingleNode("EduDirectorName").InnerText;

            //假別設定
            Dictionary<string, List<string>> allowAbsentDic = new Dictionary<string, List<string>>();
            foreach (AbsentSetting abs in _A.Select<AbsentSetting>())
            {
                string target = abs.Target;
                string source = abs.Source;

                if (!allowAbsentDic.ContainsKey(target))
                    allowAbsentDic.Add(target, new List<string>());

                allowAbsentDic[target].Add(source);
            }

            List<string> classIDs = _students.Select(x => x.RefClassID).Distinct().ToList();
            List<string> studentIDs = _students.Select(x => x.ID).ToList();

            //學生ID字串
            string id_str = string.Join("','", studentIDs);
            id_str = "'" + id_str + "'";

            //班級 catch
            Dictionary<string, ClassRecord> classDic = new Dictionary<string, ClassRecord>();
            foreach (ClassRecord cr in K12.Data.Class.SelectByIDs(classIDs))
            {
                if (!classDic.ContainsKey(cr.ID))
                    classDic.Add(cr.ID, cr);
            }

            //基本資料
            foreach (StudentRecord student in _students)
            {
                DataRow row = dt.NewRow();
                ClassRecord myClass = classDic.ContainsKey(student.RefClassID) ? classDic[student.RefClassID] : new ClassRecord();
                TeacherRecord myTeacher = myClass.Teacher != null ? myClass.Teacher : new TeacherRecord();

                row["列印日期"] = printDateTime;
                row["學校名稱"] = schoolName;
                row["學年度"] = _schoolYear;
                row["學期"] = _semester;
                row["姓名"] = student.Name;
                row["班級"] = myClass.Name + "";
                row["班導師"] = myTeacher.Name + "";
                row["座號"] = student.SeatNo + "";
                row["學號"] = student.StudentNumber;

                row["校長"] = 校長;
                row["教務主任"] = 教務主任;

                //filedName是 "列印假別1~20"
                foreach (string filedName in allowAbsentDic.Keys)
                {
                    row[filedName] = 0;
                }

                dt.Rows.Add(row);

                _RowCatchs.Add(student.ID, row);
            }

            //上課天數
            foreach (SemesterHistoryRecord shr in K12.Data.SemesterHistory.SelectByStudents(_students))
            {
                DataRow row = _RowCatchs[shr.RefStudentID];

                foreach (SemesterHistoryItem shi in shr.SemesterHistoryItems)
                {
                    if (shi.SchoolYear == _schoolYear && shi.Semester == _semester)
                        row["上課天數"] = shi.SchoolDayCount + "";
                }
            }

            //學期科目及領域成績
            foreach (JHSemesterScoreRecord jsr in JHSchool.Data.JHSemesterScore.SelectBySchoolYearAndSemester(studentIDs, _schoolYear, _semester))
            {
                DataRow row = _RowCatchs[jsr.RefStudentID];
                _文字描述.Clear();

                //學習領域成績
                row["學習領域成績"] = jsr.LearnDomainScore.HasValue ? jsr.LearnDomainScore.Value + "" : string.Empty;
                row["學習領域原始成績"] = jsr.LearnDomainScoreOrigin.HasValue ? jsr.LearnDomainScoreOrigin.Value + "" : string.Empty;
                row["課程學習成績"] = jsr.CourseLearnScore.HasValue ? jsr.CourseLearnScore.Value + "" : string.Empty;
                row["課程學習原始成績"] = jsr.CourseLearnScoreOrigin.HasValue ? jsr.CourseLearnScoreOrigin.Value + "" : string.Empty;

                //科目成績
                int count = 0;
                foreach (SubjectScore subj in jsr.Subjects.Values)
                {
                    count++;

                    //超過就讓它爆炸
                    if (count > Global.SupportSubjectCount)
                        throw new Exception("超過支援列印科目數量: " + Global.SupportSubjectCount);

                    row["S科目" + count] = subj.Subject;
                    row["S領域" + count] = string.IsNullOrWhiteSpace(subj.Domain) ? "彈性課程" : subj.Domain;
                    row["S節數" + count] = subj.Period + "";
                    row["S權數" + count] = subj.Credit + "";
                    row["S成績" + count] = subj.Score.HasValue ? subj.Score.Value + "" : string.Empty;
                    row["S等第" + count] = subj.Score.HasValue ? _degreeMapper.GetDegreeByScore(subj.Score.Value) : string.Empty;
                    row["S原始成績" + count] = subj.ScoreOrigin.HasValue ? subj.ScoreOrigin.Value + "" : string.Empty;
                    row["S補考成績" + count] = subj.ScoreMakeup.HasValue ? subj.ScoreMakeup.Value + "" : string.Empty;
                }

                count = 0;
                foreach (DomainScore domain in jsr.Domains.Values)
                {
                    count++;

                    //超過就讓它爆炸
                    if (count > Global.SupportDomainCount)
                        throw new Exception("超過支援列印領域數量: " + Global.SupportDomainCount);

                    row["D領域" + count] = domain.Domain;
                    row["D節數" + count] = domain.Period + "";
                    row["D權數" + count] = domain.Credit + "";
                    row["D成績" + count] = domain.Score.HasValue ? domain.Score.Value + "" : string.Empty;
                    row["D等第" + count] = domain.Score.HasValue ? _degreeMapper.GetDegreeByScore(domain.Score.Value) : string.Empty;
                    row["D原始成績" + count] = domain.ScoreOrigin.HasValue ? domain.ScoreOrigin.Value + "" : string.Empty;
                    row["D補考成績" + count] = domain.ScoreMakeup.HasValue ? domain.ScoreMakeup.Value + "" : string.Empty;

                    if (!string.IsNullOrWhiteSpace(domain.Text))
                        _文字描述.Add(domain.Domain + " : " + domain.Text);
                }

                row["文字描述"] = string.Join(Environment.NewLine, _文字描述);
            }

            //預設學年度學期物件
            JHSchool.Behavior.BusinessLogic.SchoolYearSemester sysm = new JHSchool.Behavior.BusinessLogic.SchoolYearSemester(_schoolYear, _semester);

            //AutoSummary
            foreach (AutoSummaryRecord asr in AutoSummary.Select(_students.Select(x => x.ID), new JHSchool.Behavior.BusinessLogic.SchoolYearSemester[] { sysm }))
            {
                DataRow row = _RowCatchs[asr.RefStudentID];

                //缺曠
                foreach (AbsenceCountRecord acr in asr.AbsenceCounts)
                {
                    string key = Global.GetKey(acr.PeriodType, acr.Name);

                    //filedName是 "列印假別1~20"
                    foreach (string filedName in allowAbsentDic.Keys)
                    {
                        foreach (string item in allowAbsentDic[filedName])
                        {
                            if (key == item)
                            {
                                int count = 0;
                                int.TryParse(row[filedName] + "", out count);

                                count += acr.Count;
                                row[filedName] = count;
                            }
                        }
                    }
                }

                //獎懲
                row["大功"] = asr.MeritA;
                row["小功"] = asr.MeritB;
                row["嘉獎"] = asr.MeritC;
                row["大過"] = asr.DemeritA;
                row["小過"] = asr.DemeritB;
                row["警告"] = asr.DemeritC;

                //日常生活表現
                JHMoralScoreRecord msr = asr.MoralScore;
                XmlElement textScore = (msr != null && msr.TextScore != null) ? msr.TextScore : K12.Data.XmlHelper.LoadXml("<TextScore/>");

                foreach (string key in Global.DLBehaviorRef.Keys)
                    SetDLBehaviorData(key, Global.DLBehaviorRef[key], textScore, row);
            }

            //社團成績
            string condition = string.Format("SchoolYear='{0}' and Semester='{1}' and studentid in ({2})", _schoolYear, _semester, id_str);
            List<AssnCode> list = _A.Select<AssnCode>(condition);

            foreach (string id in studentIDs)
            {
                int count = 0;
                DataRow row = _RowCatchs[id];

                foreach (AssnCode ac in list.FindAll(x => x.StudentID == id))
                {
                    XmlElement scores = K12.Data.XmlHelper.LoadXml(ac.Scores);

                    foreach (XmlElement item in scores.SelectNodes("Item"))
                    {
                        count++;

                        //超過就讓它爆炸
                        if (count > Global.SupportClubCount)
                            throw new Exception("超過支援列印社團數量: " + Global.SupportClubCount);

                        string name = item.GetAttribute("AssociationName");
                        string score = item.GetAttribute("Score");
                        string effort = item.GetAttribute("Effort");
                        string text = item.GetAttribute("Text");

                        row["社團Name" + count] = name;
                        row["社團Score" + count] = score;
                        row["社團Effort" + count] = effort;
                        row["社團Text" + count] = text;
                    }
                }
            }

            //服務學習時數
            string query = string.Format("select ref_student_id,occur_date,reason,hours from $k12.service.learning.record where school_year={0} and semester={1} and ref_student_id in ({2})", _schoolYear, _semester, id_str);
            DataTable table = _Q.Select(query);
            foreach (DataRow dr in table.Rows)
            {
                string sid = dr["ref_student_id"] + "";

                DataRow row = _RowCatchs[sid];

                decimal new_hr = 0;
                decimal.TryParse(dr["hours"] + "", out new_hr);

                decimal old_hr = 0;
                decimal.TryParse(row["服務學習時數"] + "", out old_hr);

                decimal hr = old_hr + new_hr;
                row["服務學習時數"] = hr;
            }
        }

        /// <summary>
        /// 填寫日常生活表現資料
        /// </summary>
        /// <param name="name"></param>
        /// <param name="path"></param>
        /// <param name="textScore"></param>
        /// <param name="row"></param>
        private static void SetDLBehaviorData(string name, string path, XmlElement textScore, DataRow row)
        {
            row[name + "_Name"] = _DLBehaviorConfigNameDict.ContainsKey(name) ? _DLBehaviorConfigNameDict[name] : string.Empty;

            if (_DLBehaviorConfigItemNameDict.ContainsKey(name))
            {
                int index = 0;
                foreach (string itemName in _DLBehaviorConfigItemNameDict[name])
                {
                    foreach (XmlElement item in textScore.SelectNodes(path))
                    {
                        if (itemName == item.GetAttribute("Name"))
                        {
                            index++;
                            row[name + "_Item_Name" + index] = itemName;
                            row[name + "_Item_Degree" + index] = item.GetAttribute("Degree");
                            row[name + "_Item_Description" + index] = item.GetAttribute("Description");
                        }
                    }
                }
            }
            else if (_DLBehaviorConfigNameDict.ContainsKey(name))
            {
                string value = _DLBehaviorConfigNameDict[name];

                foreach (XmlElement item in textScore.SelectNodes(path))
                {
                    if (value == item.GetAttribute("Name"))
                        row[name + "_Description"] = item.GetAttribute("Description");
                }
            }
        }

        /// <summary>
        /// 取得列印資料表
        /// </summary>
        /// <param name="students"></param>
        /// <param name="schoolYear"></param>
        /// <param name="semester"></param>
        /// <returns></returns>
        public static DataTable GetDataTable(List<StudentRecord> students, int schoolYear, int semester)
        {
            Initialize();

            _students = students;
            _schoolYear = schoolYear;
            _semester = semester;

            DataTable dt = GetEmptyDataTable();

            //Stopwatch sw = new Stopwatch();
            //sw.Start();
            //有學生才填資料
            if (_students.Count > 0)
                FillData(dt);

            //sw.Stop();
            //Console.WriteLine(sw.ElapsedMilliseconds);

            return dt;
        }

        /// <summary>
        /// 物件初始化
        /// </summary>
        private static void Initialize()
        {
            if (_A == null)
                _A = new AccessHelper();
            if (_Q == null)
                _Q = new QueryHelper();
            if (_degreeMapper == null)
                _degreeMapper = new DegreeMapper();
        }
    }
}