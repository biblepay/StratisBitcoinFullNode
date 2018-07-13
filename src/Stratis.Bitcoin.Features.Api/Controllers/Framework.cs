using System;
using System.Data.SqlClient;
using System.Data;
using Microsoft.VisualBasic;
using System.Web;
using System.Net;
using System.IO;

namespace USGDFramework
{
    public static class clsStaticHelper
    {
        public static string GetDirectoryUp(string sStart, int iDepth)
        {
            for (int i = 0; i < iDepth; i++)
            {
                DirectoryInfo diRoot = Directory.GetParent(sStart);
                sStart = diRoot.FullName;
            }
            return sStart;
        }

        public static string GetConfig(string key)
        {
            try
            {
                // BIBLEPAY TODO - When we make an installer, we will need to make GetDirectoryUp more resilient to find the installed ini file in the executing directory subfolder (not 4 parents above the executing directory)
                string sPath = GetDirectoryUp(Directory.GetCurrentDirectory(),4) + "\\wwwroot\\biblepaygui.ini";
                using (System.IO.StreamReader sr = new System.IO.StreamReader(sPath))
                {
                    while (sr.EndOfStream == false)
                    {
                        string sTemp = sr.ReadLine();
                        int iPos = sTemp.IndexOf("=");
                        if (iPos > 0)
                        {
                            string sIniKey = sTemp.Substring(0, iPos);
                            string sIniValue = sTemp.Substring(iPos + 1, sTemp.Length - iPos - 1);
                            sIniValue = sIniValue.Replace("\"", "");
                            if (sIniKey.ToLower().Contains("_e"))
                            {
                                sIniValue = modCryptography.Des3DecryptData2(sIniValue);
                            }
                            if (key.ToLower() == sIniKey.ToLower())
                            {
                                sr.Close();
                                return sIniValue;
                            }
                        }
                    }
                    sr.Close();
                }
            }
            catch (Exception ex)
            {
                //
            }

            return "";
        }

    }
    public class Data
    {

        public string sSQLConn()
        {
            return "Server=" + clsStaticHelper.GetConfig("DatabaseHost")
            + ";" + "Database=" + clsStaticHelper.GetConfig("DatabaseName")
            + ";MultipleActiveResultSets=true;" + "Uid=" + clsStaticHelper.GetConfig("DatabaseUser")
            + ";pwd=" + clsStaticHelper.GetConfig("DatabasePass");
        }

        public Data()
        {
            // Constructor goes here; since we use SQL Server connection pooling, dont create connection here, for best practices create connection at usage point and destroy connection after Using goes out of scope - see GetDataTable
            // This keeps the pool->databaseserver connection count < 10.  
        }


        public void Exec(string sql, bool bLog = true, bool bLogError = true)
        {
            try
            {
                if (bLog) Log(sql);
                using (SqlConnection con = new SqlConnection(sSQLConn()))
                {
                    con.Open();
                    SqlCommand myCommand = new SqlCommand(sql, con);

                    myCommand.ExecuteNonQuery();
                }

            }
            catch (Exception ex)
            {
                if (bLogError) Log(" EXEC: " + sql + "," + ex.Message);
            }
        }
      
        public DataTable GetDataTable(string sql, bool bLog = true)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(sSQLConn()))
                {
                    con.Open();

                    SqlDataAdapter a = new SqlDataAdapter(sql, con);
                    DataTable t = new DataTable();
                    a.Fill(t);
                    return t;
                }
            }
            catch (Exception ex)
            {
                Log("GetDataTable:" + sql + "," + ex.Message);
            }
            DataTable dt = new DataTable();
            return dt;
        }


        public double GetScalarDouble(string sql, object vCol, bool bLog = true)
        {
            DataTable dt1 = GetDataTable(sql, bLog);
            try
            {
                if (dt1.Rows.Count > 0)
                {
                    object oOut = null;
                    if (vCol.GetType().ToString() == "System.String")
                    {
                        oOut = dt1.Rows[0][vCol.ToString()];
                    }
                    else
                    {
                        oOut = dt1.Rows[0][Convert.ToInt32(vCol)];
                    }
                    double dOut = Convert.ToDouble("0" + oOut.ToString());
                    return dOut;
                }
            }
            catch (Exception ex)
            {
            }
            return 0;
        }


        public string GetScalarString(string sql, object vCol, bool bLog = true)
        {
            DataTable dt1 = GetDataTable(sql);
            try
            {
                if (dt1.Rows.Count > 0)
                {
                    object oOut = null;
                    if (vCol.GetType().ToString() == "System.String")
                    {
                        oOut = dt1.Rows[0][vCol.ToString()];
                    }
                    else
                    {
                        oOut = dt1.Rows[0][Convert.ToInt32(vCol)];
                    }
                    return oOut.ToString();
                }
            }
            catch (Exception ex)
            {
            }
            return "";
        }


        public SqlDataReader GetDataReader(string sql)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(sSQLConn()))
                {
                    con.Open();
                    SqlDataReader myReader = default(SqlDataReader);
                    SqlCommand myCommand = new SqlCommand(sql, con);
                    myReader = myCommand.ExecuteReader();
                    return myReader;
                }
            }
            catch (Exception ex)
            {
                Log("GetDataReader:" + ex.Message + "," + sql);
            }
            SqlDataReader dr = default(SqlDataReader);
            return dr;
        }


        public string ReadFirstRow(string sql, object vCol, bool bLog = true)
        {

            try
            {
                using (SqlConnection con = new SqlConnection(sSQLConn()))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        //cmd.CommandTimeout = 6000;

                        SqlDataReader dr = cmd.ExecuteReader();
                        if (!dr.HasRows | dr.FieldCount == 0) return string.Empty;
                        while (dr.Read())
                        {
                            if (vCol is String)
                            {
                                return dr[(string)vCol].ToString();
                            }
                            else
                            {
                                return dr[(int)vCol].ToString();
                            }
                        }
                    }
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Log("Readfirstrow: " + sql + ", " + ex.Message);
            }
            return "";
        }
        

        public static void Log(string sData)
        {
            try
            {
                string sPath = null;
                string sDocRoot = clsStaticHelper.GetConfig("LogPath");
                sPath = sDocRoot + "biblestrat.log";
                System.IO.StreamWriter sw = new System.IO.StreamWriter(sPath, true);
                string Timestamp = DateTime.Now.ToString();
                sw.WriteLine(Timestamp + ", " + sData);
                sw.Close();
            }
            catch (Exception ex)
            {
                string sMsg = ex.Message;
            }
        }


        private string GetMenuRow(string sGuid, string sMenuName, string sLink, bool bActiveHasSub, bool bHasSub, bool bLIEndTag,
            string sButtonID, string sMenuClick, long ExternalLink, string sMethod)
        {

            string sOut = "";
            string sClass = "";
            if (bActiveHasSub)
            {
                sClass = " class='active has-sub' ";
            }
            if (bHasSub)
            {
                sClass = " class='has-sub' ";
            }
            string sHREF = string.Empty;
            //If this is an external Link, call the event to replace the contents otherwise call the Event to raise the event into the program
            string sOnclick = null;
            if (ExternalLink == 1)
            {
                sOnclick = "send('na', '','LINK', '', '', '" + sLink + "', link_complete);";
                //Display the resource
                sOnclick = sLink;
                sOnclick = "validate(this,'link','" + sLink + "');";
                sOnclick = "FrameNav('" + sLink + "','" + sMethod + "','" + sGuid + "');";
                if (sLink.StartsWith("http"))
                {
                    sOnclick = "window.navigate('" + sLink + "');";
                    sOnclick = "location.href='" + sLink + "';";
                }
            }
            else
            {
                sOnclick = "send('na', '','LINKEVENT','','','" + sLink + "',linkevent_complete);";
                //Fire the event into the .NET app
                sOnclick = sLink;
                sOnclick = "validate(this,'link','" + sLink + "');";
                sOnclick = "FrameNav('" + sLink + "','" + sMethod + "','" + sGuid + "');";
            }
            sOut += "<li " + sClass + "><a onclick=\"" + sOnclick + "\">" + "<span>" + sMenuName + "</span></a>" + Constants.vbCrLf;
            if (bLIEndTag)
                sOut = sOut + "</li>";
            return sOut;
        }


        public string GetTopLevelMenu(string sURL1)
        {
            // Depending on context, filter the menu
            bool bAcc = (sURL1.ToUpper().Contains("ACCOUNTABILITY"));
            string sWhere = bAcc ? " where Accountability=1" : " ";
            string sql = "select * from MenuWallet " + sWhere + " order by hierarchy, ordinal";
            string sUser = USGDFramework.clsStaticHelper.GetConfig("DatabaseName");

            DataTable dt = GetDataTable(sql);
            string sOut = "<div id='cssmenu'> <UL> ";
            string[] vHierarchy = null;
            string sArgs = null;
            string sLastRootMenuName = "?";
            string sRootMenuName = "";
            bool bULOpen = false;
            string sButtonID = "btn1";
            int x = 0;
            for (int y = 0; y < dt.Rows.Count; y++)
            {
                
                vHierarchy = dt.Rows[y]["Hierarchy"].ToString().Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);

                for (x = 0; x < vHierarchy.Length; x++)
                {
                    string sMenuName = vHierarchy[x].ToString();
                    if (string.IsNullOrEmpty(sMenuName))
                        sMenuName = vHierarchy[x + 1].ToString();
                    if (x == 0)
                    {
                        sRootMenuName = vHierarchy[x].ToString();
                        if (string.IsNullOrEmpty(sRootMenuName))
                            sRootMenuName = vHierarchy[x + 1].ToString();
                    }
                    string sURL = dt.Rows[y]["Classname"].ToString();
                    string sMethod = (dt.Rows[y]["Method"] ?? "").ToString();
                    string sGuid = (dt.Rows[y]["id"].ToString());
                    long ExternalLink = 1;
                    sArgs = sURL;
                    if (x == 0 & sLastRootMenuName != sRootMenuName)
                    {
                        if (bULOpen)
                        {
                            bULOpen = false;
                            sOut += "</UL>";
                        }
                        sOut += GetMenuRow(sGuid, sMenuName, sURL, true, false, false, sButtonID, sArgs, ExternalLink, sMethod);
                    }
                    else if (x != 0)
                    {
                        if (!bULOpen) { sOut += "<UL>"; bULOpen = true; }
                        sOut += GetMenuRow(sGuid, sMenuName, sURL, false, false, false, sButtonID, sArgs, ExternalLink, sMethod);
                    }
                    if (x == 0)
                        sLastRootMenuName = sRootMenuName;
                }

            }
            if (bULOpen)
                sOut += "</UL>";

            sOut = sOut + "</UL>";
            sOut += "<table class='last'>";
            for (x = 1; x <= 10; x++)
            {
                sOut += "<tr><td>&nbsp;</td></tr>";
            }
            sOut += "</table> </div>";
            return sOut;

        }
    }
}
