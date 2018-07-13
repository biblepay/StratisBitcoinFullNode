using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Web;
using Microsoft.AspNetCore.Http;

namespace BiblePayGUI
{

        public static class MyExtensions
        {
            public static int WordCount(this String str)
            {
                return str.Split(new char[] { ' ', '.', '?' },
                                 StringSplitOptions.RemoveEmptyEntries).Length;
            }

            public static Guid ToGuid(this object o)
            {
                return o.ToGuid();
            }

            public static Guid ToGuid(this String str)
            {
                return str.ToGuid();
            }

            public static Guid ToGuid2(this string s1)
            {
                return s1.ToGuid();
            }

            public static DateTime ToDate(this String str)
            {
                if (str == String.Empty) return Convert.ToDateTime("1-1-1900");
                try
                {
                    return Convert.ToDateTime(str);

                }
                catch (Exception ex)
                {
                    return Convert.ToDateTime("1-1-1900");
                }

            }

            public static String ToStr(this object o)
            {
                if (o == null) return String.Empty;
                return o.ToString();
            }

        }
    }

    namespace BiblePayGUI
    {


        public class USGDGui
        {
            public SystemObject Sys = null;
            // This is also the Gui Version
            public long Version = 2036;

            public string ViewGuid
            {
                get
                {
                    return Sys.GetObjectValue(ObjectName, "ViewGuid");
                }
                set
                {
                    Sys.SetObjectValue(ObjectName, "ViewGuid", value);
                }
            }

            public string ParentID
            {
                get
                {
                    return Sys.GetObjectValue(ObjectName, "ParentID");
                }
                set
                {
                    Sys.SetObjectValue(ObjectName, "ParentID", value);
                }
            }


            public string WhereClause
            {
                get
                {
                    return Sys.GetObjectValue(ObjectName, "WhereClause");
                }
                set
                {
                    Sys.SetObjectValue(ObjectName, "WhereClause", value);
                }
            }


            public string ObjectName { get; set; }
            public SystemObject.SectionMode SectionMode { get; set; }

            public USGDGui(SystemObject ptrSystemObject)
            {
                Sys = ptrSystemObject;
            }

        }


        public class WebObj
        {
            public string body;
            public string action;
            public string divname;
            public string eventname;
            public string name;
            public string value;
            public string classname;
            public string methodname;
            public string javascript;
            public string doappend;
            public string breadcrumb;
            public string breadcrumbdiv;
            public string ApplicationMessage;
            public string guid;
            public string orderby;
            public bool ClearScreen;
            public bool SingleTable;
            public string usgdid;
            public string usgdvalue;
            public string caption;
            public string ErrorTextNew;
            public string Checked;
            public List<SystemObject.LookupValue> LookupValues;
        }


        public class USGDTable
        {
            // A superclassed version of the DataTable, that allows specialized handling of GUIDs, provides a facility for fully qualified column names in inner joined result sets,
            // and allows us to extend the datatable schema with customized column properties.
            // First: We copy the DataTable results into a USGDTable and while copying, add the appropriate replacements (IE Fully Qualified column names etc).

            public int Rows { get; set; }
            public int Cols { get; set; }
            public struct TableValue
            {
                public object Value;
                public object Guid;
            }
            public TableValue[,] USGDValue = null;
            public string[] ColumnNames = null;
            private SystemObject Sys = null;

            public USGDTable(DataTable dt, SystemObject s, string sTableName)
            {
                Sys = s;
                USGDValue = new TableValue[dt.Rows.Count, dt.Columns.Count];
                ColumnNames = new string[dt.Columns.Count];
                // Copy the column names
                for (int z = 0; z < dt.Columns.Count; z++)
                {
                    ColumnNames[z] = dt.Columns[z].ColumnName;
                    SystemObject.USGDDictionary d = Sys.GetDictionaryMember(sTableName, ColumnNames[z]);
                    if (d.ParentFieldName != null)
                    {
                        // Ensure we don't have in global memory first:
                        string key = d.ParentGuiField1 + "," + d.ParentTable;
                        if (!Sys.dictDictionaryParent.ContainsKey(key))
                        {
                            Sys.AddDictionaryParent(key, "1");
                            // This field must have data populated
                            if (d.ParentTable.Length > 0)
                            {
                                string sql = "Select ID," + d.ParentGuiField1;
                                if (d.ParentGuiField2.Length > 0)
                                {
                                    sql += "," + d.ParentGuiField2;
                                }
                                sql += " FROM " + d.ParentTable;
                                DataTable dt1 = Sys._data.GetDataTable(sql);
                                for (int y = 0; y < dt1.Rows.Count; y++)
                                {
                                    string Caption = dt1.Rows[y][d.ParentGuiField1].ToString();
                                    if (d.ParentGuiField2.Length > 0)
                                    {
                                        Caption += ", " + dt1.Rows[y][d.ParentGuiField2].ToString();
                                    }
                                    string id = dt1.Rows[y]["ID"].ToString();
                                    // Store
                                    Sys.AddDictionaryParent(id, Caption);

                                }
                            }
                        }
                    }
                }
                // For each column in the dictionary in USE, ensure we have the GUID parent value:

                for (int y = 0; y < dt.Rows.Count; y++)
                {
                    for (int x = 0; x < dt.Columns.Count; x++)
                    {
                        TableValue tv = new TableValue();
                        tv.Value = dt.Rows[y][x];
                        if (tv.Value.GetType() == typeof(System.Guid))
                        {
                            tv.Guid = tv.Value;
                            tv.Value = Sys.GetDictionaryParentCaption(tv.Guid.ToString());
                        }
                        USGDValue[y, x] = tv;
                    }
                }
                Rows = dt.Rows.Count;
                Cols = dt.Columns.Count;
            }

            public object Value(int row, int col)
            {
                return USGDValue[row, col].Value;
            }
            public object GuidValue(int row, int col)
            {
                return USGDValue[row, col].Guid;
            }
            //Access the Value by row string
            public object Value(int row, string col)
            {
                for (int x = 0; x < Cols; x++)
                {
                    if (col.ToUpper() == ColumnNames[x].ToUpper())
                    {
                        return USGDValue[row, x].Value;
                    }
                }
                return null;
            }

            public object GuidValue(int row, string col)
            {
                for (int x = 0; x < Cols; x++)
                {
                    if (col.ToUpper() == ColumnNames[x].ToUpper())
                    {
                        return USGDValue[row, x].Guid;
                    }
                }
                return null;
            }

        }

    

        public class SystemObject
        {
            public string Username;
            public string UserGuid;
            public long lOrphanNews;
            public string Theme;
            public Guid Organization;
            public SectionMode LastSectionMode = SectionMode.View;
            public bool bFailedValidation = false;

            public Dictionary<String, WebObj> dictMembers = new Dictionary<String, WebObj>();
            public Dictionary<String, USGDDictionary> dictMasterDictionary = new Dictionary<String, USGDDictionary>();
            public Dictionary<String, USGDDictionaryParent> dictDictionaryParent = new Dictionary<String, USGDDictionaryParent>();
            public HttpContext CurrentHTTPContext { get; set; }
            public string IP { get; set; }
            public List<Breadcrumb> _breadcrumb = new List<Breadcrumb>();
            public long iCount = 0;
            private string ID = null;
            public USGDFramework.Data _data = null;
            public WebObj LastWebObject { get; set; }

            public struct Page
            {
                public string Name;
                public SectionMode Mode;
                public string ActiveSection;
            }

            public string ActiveSection { get; set; }

            public string GetViewFieldsForSection(string sSectionName, out string sDependentSection, out string sDependentSectionFields)
            {
                string sql = "Select * FROM SECTION WHERE Name='" + sSectionName + "' and deleted = 0";
                DataTable dt2 = this._data.GetDataTable(sql, false);
                string Fields = dt2.Rows[0]["Fields"].ToString();
                sDependentSection = dt2.Rows[0]["DependentSection"].ToString();
                sDependentSectionFields = dt2.Rows[0]["DependentFields"].ToString();
                return Fields;
            }

            public SQLDetective GetSectionSQL(string sSection, string sTable, string sParentGuid)
            {
                SQLDetective s = new SQLDetective();
                string s1 = "";
                string s2 = "";
                string sFields = GetViewFieldsForSection(sSection, out s1, out s2);
                s.FromFields = sFields;
                s.Table = sTable;
                if (sParentGuid == null || sParentGuid == string.Empty) sParentGuid = Guid.NewGuid().ToString(); // prevent SQL select error 
                s.WhereClause = "ParentId='" + sParentGuid + "' And " + sTable + ".deleted=0";
                s.OrderBy = sTable + ".Updated";
                if (LastWebObject != null)
                {

                    if (LastWebObject.orderby.Length > 0) s.OrderBy = LastWebObject.orderby;
                }
                return s;
            }

            public string GetLookupValues(string sTable, string sFieldName, out string sMethodName)
            {
                string sql = "Select * FROM LOOKUP WHERE TableName='"
                    + sTable + "' and Field='" + sFieldName + "'";
                DataTable dt2 = this._data.GetDataTable(sql);
                if (dt2.Rows.Count == 0)
                {
                    sMethodName = string.Empty;
                    return String.Empty;
                }

                string sValues = dt2.Rows[0]["FieldList"].ToString();
                sMethodName = (dt2.Rows[0]["Method"] ?? String.Empty).ToString();
                return sValues;
            }

            public WebReply Redirect(string sPage, object Caller)
            {
                // Add breadcrumb for the calling page
                string myClass = Caller.GetType().ToString();
                StackTrace stackTrace = new StackTrace();
                StackFrame[] stackFrames = stackTrace.GetFrames();  // get method calls (frames)
                StackFrame callingFrame = stackFrames[1];
                MethodInfo method = (MethodInfo)callingFrame.GetMethod();
                string sMyMethod = method.Name;
                AddBreadcrumb(sPage, myClass, sMyMethod, true);
                SetObjectValue("", "ApplicationMessage", sPage);
                string _ID = "";
                try
                {
                    _ID = ((BiblePayGUI.USGDGui)Caller).ViewGuid;
                }
                catch (Exception ex)
                {
                }

                string sql = "Select ID,Sections from PAGE where deleted=0 and organization = '" + Organization.ToString() + "' and name='" + sPage + "'";
                // This is the database driven version of the Page
                DataTable dt = _data.GetDataTable(sql);
                WebReply wrMaster = new WebReply();
                if (dt.Rows.Count > 0)
                {
                    string sSections = dt.Rows[0]["Sections"].ToString();
                    string[] vSections = sSections.Split(new string[] { "," }, StringSplitOptions.None);
                    for (int i = 0; i < vSections.Length; i++)
                    {
                        // Add the sections to the page
                        string sSectionName = vSections[i];
                        sql = "Select * From Section where Name='" + sSectionName + "' and deleted=0";
                        DataTable dtSection = _data.GetDataTable(sql);
                        if (dtSection.Rows.Count > 0)
                        {
                            string sClass = dtSection.Rows[0]["Class"].ToString();
                            string sMethod = dtSection.Rows[0]["Method"].ToString();
                            // invoke and append
                            var type = Type.GetType(sClass);
                            object myObject = Activator.CreateInstance(type, this);
                            ((BiblePayGUI.USGDGui)myObject).ParentID = _ID;
                            MethodInfo methodInfo = type.GetMethod(sMethod);
                            WebReply wrMini = (WebReply)methodInfo.Invoke(myObject, null);
                            wrMaster.AddWebPackages(wrMini.Packages);
                        }
                    }
                }
                if (wrMaster.Packages.Count > 0)
                {
                    WebReplyPackage wrp1 = wrMaster.Packages[0];
                    wrp1.ClearScreen = true;
                    wrMaster.Packages[0] = wrp1;
                }
                return wrMaster;
            }

            private string[] Tokenize(string sRow)
            {
                sRow = sRow.ToUpper();
                sRow = sRow.Replace(" ", "_");
                // inside apostrophes, change the underscore to another character
                bool bInQuote = false;
                string sOut = "";
                for (int i = 0; i < sRow.Length; i++)
                {
                    string sChar = sRow[i].ToString();
                    if (sChar == "\"")
                    {
                        bInQuote = !bInQuote;
                    }
                    if (sChar == "_" && bInQuote)
                    {
                        sChar = " ";
                    }
                    sOut += sChar;
                }

                string[] sTokens = sOut.Split(new string[] { "_" }, StringSplitOptions.None);
                return sTokens;
            }

            private DateTime ToDateTime(object o)
            {
                if (o == null || ("" + o.ToString() == String.Empty)) return Convert.ToDateTime("1-1-1900");
                return Convert.ToDateTime(o);
            }

            private bool ValidateSectionUsingSectionRules(string sSection, string sTableName)
            {
                DataTable dt = _data.GetDataTable("Select  * FROM SECTIONRULES WHERE SECTIONID=(Select ID from Section where name='" + sSection + "')");
                bool bFailed = false;
                for (int iRows = 0; iRows < dt.Rows.Count; iRows++)
                {
                    string sRule = dt.Rows[iRows]["RuleText"].ToString();
                    string[] vRows = sRule.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                    for (int i = 0; i < vRows.Length; i++)
                    {
                        string sRow = vRows[i];
                        string[] vRow = Tokenize(sRow);
                        // Now we need to process the Rule
                        // First the IF rule
                        if (vRow.Length > 4)
                        {
                            if (vRow[0] == "IF")
                            {
                                string sArg1 = vRow[1].Replace("@", "");
                                string sArg2 = vRow[3].Replace("@", "");
                                string sCondition = vRow[2];
                                string sAction = vRow[4];
                                string sMessage = vRow[5];
                                string sArgValue1 = GetObjectValue(sSection, sArg1);
                                string sArgValue2 = GetObjectValue(sSection, sArg2);
                                if (sArg2 == "\"\"") sArgValue2 = "";
                                if (sArg1 == "") sArgValue1 = "";

                                if (sCondition == ">")
                                {
                                    if (ToDateTime(sArgValue1) > ToDateTime(sArgValue2))
                                    {
                                        WebObj dm = GetWebObjectFromDom(sSection, sArg1);
                                        dm.ErrorTextNew = sMessage;
                                        UpdateObject(sSection, ref dm);
                                        bFailed = true;
                                    }

                                }
                                else if (sCondition == "=")
                                {
                                    if (sArgValue1.Trim() == sArgValue2.Trim())
                                    {
                                        WebObj dm = GetWebObjectFromDom(sSection, sArg1);
                                        dm.ErrorTextNew = sMessage;
                                        UpdateObject(sSection, ref dm);
                                        bFailed = true;

                                    }
                                }
                                else if (sCondition == "NOT")
                                {
                                    if (sArg2 == "ALLORNONE")
                                    {
                                        // if any of sArg1 are populated, but not all, fail
                                        string[] vCols = sArg1.Split(new string[] { "," }, StringSplitOptions.None);
                                        int iTotalLen = 0;
                                        int iTotalPopulated = 0;
                                        string sFirstCol = vCols[0];
                                        for (int i1 = 0; i1 < vCols.Length; i1++)
                                        {
                                            string sCol = vCols[i1];
                                            int iLen = GetObjectValue(sSection, sCol).Trim().Length;
                                            iTotalLen += iLen;
                                            if (iLen > 0) iTotalPopulated++;
                                        }
                                        if (iTotalPopulated > 0 && iTotalPopulated != vCols.Length)
                                        {
                                            WebObj dm = GetWebObjectFromDom(sSection, sFirstCol);
                                            dm.ErrorTextNew = sMessage;
                                            UpdateObject(sSection, ref dm);
                                            bFailed = true;
                                        }

                                    }
                                }

                            }

                        }

                    }

                }
                return bFailed;
            }


            public bool SaveObject(string sSectionName, string sTable, object caller)
            {
                bool bIsNew = false;

                string _ID = ((BiblePayGUI.USGDGui)caller).ViewGuid;

                if (_ID == String.Empty)
                {
                    _ID = Guid.NewGuid().ToString();
                    bIsNew = true;
                }
                string sOrgGuid = this.Organization.ToString();
                string sParentID = ((BiblePayGUI.USGDGui)caller).ParentID;

                if (sParentID == String.Empty)
                {
                    throw new Exception("Calling class must set ParentID before saving a business object.");
                }

                bool bFailed = ValidateSectionUsingSectionRules(sSectionName, sTable);
                if (bFailed)
                {
                    ((BiblePayGUI.USGDGui)caller).ViewGuid = _ID;
                    bFailedValidation = true;
                    ((BiblePayGUI.USGDGui)caller).SectionMode = LastSectionMode;

                    return false;
                }


                if (bIsNew)
                {
                    string sql10 = "Insert into " + sTable + " (id,added,Deleted,addedby,organization,ParentID) values ('"
                        + _ID.ToString() + "',getdate(),0,'" + this.UserGuid.ToString() + "','" + sOrgGuid + "','" + sParentID + "')";
                    _data.Exec(sql10);
                    ((BiblePayGUI.USGDGui)caller).ViewGuid = _ID;

                }

                LastWebObject.guid = _ID;
                string sql = "Update " + sTable + " Set Updated=GetDate(),UpdatedBy='" + this.UserGuid.ToString() + "',";
                string sDependentSection = string.Empty;
                string sDependentSectionFields = string.Empty;
                string Fields = GetViewFieldsForSection(sSectionName, out sDependentSection, out sDependentSectionFields);


                string[] vFields = Fields.Split(new string[] { "," }, StringSplitOptions.None);
                for (int i = 0; i < vFields.Length; i++)
                {

                    string sField = vFields[i];
                    string sUpdate = sField + "= [" + sField + "],";
                    sql += sUpdate;
                }
                sql = sql.Substring(0, sql.Length - 1);

                sql += " where id = '" + _ID.ToString() + "'";
                sql = Replacements(sSectionName, sql);
               
                // DEPENDENT SECTION
                if (sDependentSection.Length > 0 && sDependentSectionFields.Length > 0)
                {
                    string sDependentGuid = Guid.NewGuid().ToString();
                    sql = "Insert into " + sDependentSection + " (id,deleted,added) values ('" + sDependentGuid.ToString() + "',0,getdate());";
                    _data.Exec(sql);
                    sql = "Update " + sDependentSection + " Set Updated=GetDate(),ParentId = '" + _ID + "',";
                    vFields = sDependentSectionFields.Split(new string[] { "," }, StringSplitOptions.None);
                    for (int i = 0; i < vFields.Length; i++)
                    {
                        string sField = vFields[i];
                        string sUpdate = sField + "= [" + sField + "],";
                        sql += sUpdate;
                    }
                    sql = sql.Substring(0, sql.Length - 1);
                    sql += " where id = '" + sDependentGuid.ToString() + "'";
                    sql = Replacements(sSectionName, sql);
                   
                }
                return true;
            }

            public string GenerateWhereClauseFromSection(string sSectionName, string sTableName)
            {

                string s1 = string.Empty;
                string s2 = string.Empty;
                string Fields = GetViewFieldsForSection(sSectionName, out s1, out s2);
                string[] vFields = Fields.Split(new string[] { "," }, StringSplitOptions.None);
                string sWhere = "";
                for (int i = 0; i < vFields.Length; i++)
                {
                    string sGuiValue = GetObjectValue(sSectionName, vFields[i]);
                    if (sGuiValue.Length > 0)
                    {
                        sWhere += vFields[i] + " like '%" + sGuiValue + "%' AND ";
                    }
                }
                if (sWhere.Length > 0)
                {
                    sWhere = sWhere.Substring(0, sWhere.Length - 4);
                    sWhere += " AND " + sTableName + ".Deleted=0";
                }
                return sWhere;
            }

            public List<LookupValue> BindColumn(string sTable, string sColumn, string sWhere)
            {
                List<SystemObject.LookupValue> lLV = new List<SystemObject.LookupValue>();
                string sql = "Select id," + sColumn + " from " + sTable + " where organization = '" + this.Organization.ToString() + "'";
                if (sWhere.Length > 0) sql += " AND " + sWhere;
                DataTable dt = _data.GetDataTable(sql);
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    string sValue = dt.Rows[i][sColumn].ToString();
                    LookupValue v = new SystemObject.LookupValue();
                    v.Value = sValue;
                    v.Caption = sValue;
                    v.ID = dt.Rows[i]["id"].ToString();
                    v.Name = sValue;
                    lLV.Add(v);
                }
                return lLV;
            }


            public void BindSection(string sSectionName, string sTable, string sID)
            {
                // Bind the View Guid to the Ticket in View Mode
                string s1 = string.Empty;
                string s2 = string.Empty;
                string Fields = GetViewFieldsForSection(sSectionName, out s1, out s2);
                string sql = "Select * FROM " + sTable + " WHERE ID='" + sID + "' and " + sTable + ".deleted=0";
                DataTable dt = this._data.GetDataTable(sql);
                if (dt.Rows.Count > 0)
                {
                    string[] vFields = Fields.Split(new string[] { "," }, StringSplitOptions.None);
                    for (int i = 0; i < vFields.Length; i++)
                    {
                        if (vFields[i].Length > 0)
                        {
                            if (vFields[i] == "[Lightbox]")
                            {
                                // Make a nice lightbox appear with the image in the box
                            }
                            else
                            {
                                SetObjectValue(sSectionName, vFields[i], dt.Rows[0][vFields[i]].ToString());
                            }
                        }
                    }
                }

            }


            public enum SectionMode
            {
                Add, View, Edit, Search, Customize
            };

            public string GetPictureURL(string sPictureGuid)
            {
                if (sPictureGuid == String.Empty) return string.Empty;

                string sql = "Select ID,FullFileName,Extension from Picture where id = '" + sPictureGuid + "' and deleted=0";
                DataTable dt = _data.GetDataTable(sql);
                if (dt.Rows.Count > 0)
                {
                    string sURL = USGDFramework.clsStaticHelper.GetConfig("WebSite")
                        + "SAN/Images/" + sPictureGuid.ToString() + "" + dt.Rows[0]["Extension"].ToString();
                    return sURL;
                }

                return "?";
            }


            private void BlankOutSection(string sSectionName)
            {
                // This is necessary if the user typed some things into many fields and went away and came back
                // If on Edit, they can continue to edit the business object.  If on Add however, we need to clear these field values:
                List<string> lChanges = new List<string>();
                foreach (KeyValuePair<string, BiblePayGUI.WebObj> entry in dictMembers)
                {
                    if (entry.Key.ToUpper().StartsWith(sSectionName.ToUpper()))
                    {
                        lChanges.Add(entry.Key);
                    }
                }

                foreach (string s in lChanges)
                {
                    dictMembers[s].value = string.Empty;
                }
            }


           

            public string SqlToCSV(string sql)
            {
                DataTable dt = _data.GetDataTable(sql);
                string data = "";
                for (int rows = 0; rows < dt.Rows.Count; rows++)
                {
                    string sRow = "";
                    for (int cols = 0; cols < dt.Columns.Count; cols++)
                    {
                        string colName = dt.Columns[cols].Caption;
                        string Value = dt.Rows[rows][cols].ToString();
                        colName = colName.Replace(",", "");
                        Value = Value.Replace(",", "");
                        if (rows == 0)
                        {
                            sRow += "\"" + colName + "\",";
                        }
                        else
                        {
                            sRow += "\"" + Value + "\",";
                        }


                    }
                    if (sRow.Length > 0) sRow = sRow.Substring(0, sRow.Length - 1);
                    data += sRow + "\r\n";

                }
                string sFileName = Guid.NewGuid().ToString() + ".csv";
                string sSan = USGDFramework.clsStaticHelper.GetConfig("SAN");
                string sTargetPath = sSan + sFileName;
                System.IO.StreamWriter sw = new System.IO.StreamWriter(sTargetPath, true);
                sw.WriteLine(data);
                sw.Close();
                string sURL = USGDFramework.clsStaticHelper.GetConfig("WebSite") + "SAN/" + sFileName;
                return sURL;
            }



           
            public bool IsValidEmailFormat(string s)
            {
                try
                {
                    System.Net.Mail.MailAddress a = new System.Net.Mail.MailAddress(s);
                }
                catch
                {
                    return false;
                }
                return true;
            }

            public string PurifySQLStatement(string value)
            {
                if (value.Contains("'"))   value = "";
                if (value.Contains("--"))  value = "";
                if (value.Contains("/*"))  value = "";
                if (value.Contains("*/"))  value = "";
                if (value.ToLower().Contains("drop table "))   value = "";
                if (value.Length > 100) value = "";
                return value;
            }

            public string xPrepareStatement(string sSection, string sql)
            {
                //Sql Injection Attack Prevention:
                string[] vSql = sql.Split(new string[] { "@" }, StringSplitOptions.None);
            
                int x = 0;
                for (x = 0; x < vSql.Length; x++)
                {
                    string key = vSql[x];
                    long z = key.IndexOf(",");
                    if (z == 0)
                        z = key.IndexOf(" where");
                if (z > 0)
                {
                    int endpos = key.IndexOf("=", (int)(z + 1));

                    key = key.Substring((int)z + 1, (int)(endpos-z+1));
                }
                    string value = null;
                    if (true)
                    {
                        value = GetObjectValue(sSection, key);
                        value = PurifySQLStatement(value);
                        value = "'" + value + "'";
                        sql = sql.Replace("@" + key, value);
                    }
                }
                return sql;
            }


            public Section RenderSection(string sSectionName, string sTable, int iCols, object caller, SectionMode SecMode)
            {

                Section Section1 = new Section(sSectionName, iCols, this, caller);
                Section1.SecMode = SecMode;
                LastSectionMode = SecMode; // Used in case an EDIT or ADD FAILS, then we can put the user back in the correct SectionMode
                string s1 = "";  // This is an out variable
                string s2 = "";  // This is an out variable
                string Fields = GetViewFieldsForSection(sSectionName, out s1, out s2);
                string sID = ((BiblePayGUI.USGDGui)caller).ViewGuid;
                // If the SecMode == ADD, clear the section values, and make this a new object
                if (SecMode == SectionMode.Add)
                {
                    sID = "";
                    ((BiblePayGUI.USGDGui)caller).ViewGuid = ""; //This must be empty for the Save procedure to create a new guid, and set the bIsNew flag
                    BlankOutSection(sSectionName);
                }
                if (SecMode == SectionMode.View || SecMode == SectionMode.Edit)
                {
                    if (sID.Length > 0)
                    {
                        // If we are in Edit mode, and we failed an Edit due to a validation error, dont re-bind the section as the user might have typed some values into some fields:
                        if (!bFailedValidation)
                        {
                            BindSection(sSectionName, sTable, sID);
                        }
                        if (bFailedValidation) bFailedValidation = false;
                    }
                }

                string[] vFields = Fields.Split(new string[] { "," }, StringSplitOptions.None);
                for (int i = 0; i < vFields.Length; i++)
                {
                    // Retrieve the Dictionary entry for this field, then we can add it
                    USGDDictionary d = GetDictionaryMember(sTable, vFields[i]);
                    Edit ctl = null;

                    WebObj oCurrentObjValue = GetWebObjectFromDom(sSectionName, vFields[i]);
                    string sErrText = "";
                    if (oCurrentObjValue.ErrorTextNew != null)
                    {
                        sErrText = oCurrentObjValue.ErrorTextNew;
                    }
                    if (d.DataType == null || d.DataType.ToUpper() == "TEXT")
                    {
                        ctl = new Edit(Section1.Name, Edit.GEType.Text, d.FieldName, d.Caption, this);
                        if (d.FieldSize > 0) ctl.size = d.FieldSize;
                        if (SecMode == SectionMode.View) ctl.ReadOnly = true;
                        ctl.ErrorText = sErrText;

                        Section1.AddControl(ctl);
                    }
                    else if (d.DataType.ToUpper() == "DATE")
                    {
                        ctl = new Edit(Section1.Name, Edit.GEType.Text, d.FieldName, d.Caption, this);
                        ctl.Type = Edit.GEType.Date;
                        if (d.FieldSize > 0) ctl.size = d.FieldSize;
                        if (SecMode == SectionMode.View) ctl.ReadOnly = true;
                        ctl.ErrorText = sErrText;

                        Section1.AddControl(ctl);
                    }
                    else if (d.DataType.ToUpper() == "TEXTAREA")
                    {
                        ctl = new Edit(Section1.Name, Edit.GEType.TextArea, d.FieldName, d.Caption, this);
                        if (SecMode == SectionMode.View) ctl.ReadOnly = true;
                        if (d.FieldSize > 0) ctl.size = d.FieldSize;
                        ctl.rows = d.FieldRows;
                        ctl.cols = d.FieldCols;
                        if (SecMode == SectionMode.View) ctl.ReadOnly = true;
                        ctl.ErrorText = sErrText;
                        Section1.AddControl(ctl);
                    }
                    else if (d.DataType.ToUpper() == "LOOKUP")
                    {
                        ctl = new Edit(Section1.Name, Edit.GEType.Lookup, d.FieldName, d.Caption, this);
                        //If this field matches a corresponding LOOKUP field, pull the moniker out (maybe a state list, or maybe a name of a function that gets a list of values).
                        string sLookupMethod = string.Empty;
                        string sFieldList = GetLookupValues(d.TableName, d.FieldName, out sLookupMethod);
                        ctl.ErrorText = sErrText;

                        if (SecMode == SectionMode.View) ctl.ReadOnly = true;
                        if (sLookupMethod == String.Empty && sFieldList.Length > 0)
                        {
                            ctl.LookupValues = ctl.ConvertStringToLookupList(sFieldList);
                        }
                        else if (sLookupMethod.Length > 0)
                        {
                            // See if the method exists, if so call it through reflection:
                           

                        }
                        //disabled 
                        if (d.FieldName.ToUpper() == "SUBMITTEDBY")
                        {
                            ctl.ReadOnly = true;
                        }
                        Section1.AddControl(ctl);

                    }
                    if (SecMode == SectionMode.Edit)
                    {
                        ctl.ErrorText = (d.ErrorText ?? String.Empty).ToString();
                        ctl.ErrorText = sErrText;

                        if (ctl.ErrorText.ToUpper() == "REQUIRED")
                        {
                            if (ctl.TextBoxValue.Length > 0) ctl.ErrorText = String.Empty;
                        }
                    }
                }
                return Section1;
            }

            public string NetworkID
            {
                get; set;
            }
            public double mBitnetUseCount = 0;
           
            public void Log(string sData)
            {
                try
                {
                    string sDocRoot = USGDFramework.clsStaticHelper.GetConfig("LogPath");
                    string sPath = sDocRoot + "pool_log.dat";
                    System.IO.StreamWriter sw = new System.IO.StreamWriter(sPath, true);
                    sw.WriteLine(System.DateTime.Now.ToString() + ", " + sData);
                    sw.Close();
                }
                catch (Exception)
                {
                    // Unable to Log anything here as the log itself is malfunctioning
                }
            }
    
        
            public SystemObject(string id)
            {
                ID = id;
                _data = new USGDFramework.Data();
                this.Username = "Wallet";
                MemorizeDictionary();
            }

            public USGDTable GetUSGDTable(string sql, string sTableName)
            {
                DataTable t = _data.GetDataTable(sql, false);
                USGDTable us = new USGDTable(t, this, sTableName);
                return us;
            }

            public Int32 Val(object sInput)
            {
                if (sInput == null || sInput == DBNull.Value) return 0;
                return Convert.ToInt32(sInput);
            }

            private void MemorizeDictionary()
            {
                //This allows the system to display field captions, and replace GUIDs in the GUI with actual parent object values in the GUI.
                string sql = "Select * From Dictionary";
                DataTable dt = _data.GetDataTable(sql, false);
                for (int y = 0; y < dt.Rows.Count; y++)
                {
                    USGDDictionary d = new USGDDictionary();
                    d.TableName = dt.Rows[y]["TableName"].ToString();
                    d.FieldName = dt.Rows[y]["FieldName"].ToString();
                    d.DataType = dt.Rows[y]["DataType"].ToString();
                    d.ParentTable = dt.Rows[y]["ParentTable"].ToString();
                    d.ParentFieldName = dt.Rows[y]["ParentFieldName"].ToString();
                    d.ParentGuiField1 = dt.Rows[y]["ParentGuiField1"].ToString();
                    d.ParentGuiField2 = dt.Rows[y]["ParentGuiField2"].ToString();
                    d.Caption = dt.Rows[y]["Caption"].ToString();
                    d.FieldSize = Val(dt.Rows[y]["FieldSize"]);
                    d.FieldRows = Val(dt.Rows[y]["FieldRows"]);
                    d.FieldCols = Val(dt.Rows[y]["FieldCols"]);
                    d.ErrorText = dt.Rows[y]["ErrorText"].ToString();
                    string Key = d.TableName.ToUpper() + "," + d.FieldName.ToUpper();
                    dictMasterDictionary.Add(Key, d);
                }
            }

            public USGDDictionary GetDictionaryMember(string sTableName, string sFieldName)
            {
                string key = sTableName.ToUpper() + "," + sFieldName.ToUpper();
                if (dictMasterDictionary.ContainsKey(key))
                {
                    return dictMasterDictionary[key];
                }
                else
                {
                    USGDDictionary d1 = new USGDDictionary();
                    //This section is hit when we dont have a dictionary member.
                    d1.DataType = "Text";
                    d1.FieldName = sFieldName;
                    d1.TableName = sTableName;
                    d1.Caption = sFieldName;
                    return d1;
                }
            }

            public void AddDictionaryParent(string sID, string Caption)
            {
                USGDDictionaryParent dp = new USGDDictionaryParent();
                dp.ID = sID;
                dp.Caption = Caption;
                dp.Link = "";
                if (dictDictionaryParent.ContainsKey(sID)) return;
                dictDictionaryParent.Add(sID, dp);
            }

            public string GetDictionaryParentCaption(string sID)
            {
                if (dictDictionaryParent.ContainsKey(sID))
                {
                    return dictDictionaryParent[sID].Caption;
                }
                else
                    return "";
            }

            public string GetFieldCaption(string sTableName, string sFieldName)
            {
                string key = sTableName.ToUpper() + "," + sFieldName.ToUpper();
                if (dictMasterDictionary.ContainsKey(key))
                {
                    return dictMasterDictionary[key].Caption;
                }
                else
                    return sFieldName;
            }

            public struct USGDDictionaryParent
            {
                public string ID;
                public string Caption;
                public string Link;
            }

            public struct USGDDictionary
            {
                public string TableName;
                public string FieldName;
                public string DataType;
                public string ParentTable;
                public string ParentFieldName;
                public string Caption;
                public string ParentGuiField1;
                public string ParentGuiField2;
                public int FieldSize;
                public int FieldRows;
                public int FieldCols;
                public string ErrorText;
            }

            public struct LookupValue
            {
                public string ID;
                public string Caption;
                public string Value;
                public string Name;
                public string CheckboxCaption1;
                public string CheckboxCaption2;
                public string CheckboxCaption3;
                public string CheckboxCaption4;
            }


            public string ExtractXML(string sData, string sStartKey, string sEndKey)
            {
                int iPos1 = (sData.IndexOf(sStartKey, 0) + 1);
                iPos1 = (iPos1 + sStartKey.Length);
                int iPos2 = (sData.IndexOf(sEndKey, (iPos1 - 1)) + 1);
                if ((iPos2 == 0))
                {
                    return "";
                }
                string sOut = sData.Substring((iPos1 - 1), (iPos2 - iPos1));
                return sOut;
            }


            public string Replacements(string sSectionName, string sql)
            {
                string[] s = sql.Split(new string[] { "[" }, StringSplitOptions.None);
                for (int i = 0; i < s.Length; i++)
                {
                    string Tag = ExtractXML("[" + s[i], "[", "]");
                    string sValue = GetObjectValue(sSectionName, Tag);
                    sql = sql.Replace("[" + Tag + "]", "'" + sValue + "'");
                }
                return sql;
            }


            public WebObj GetWebObjectFromDom(string section, string name)
            {
                string key = (section + name).ToUpper();
                if (dictMembers.ContainsKey(key))
                {
                    WebObj w = dictMembers[key];
                    return w;
                }
                else
                {
                    return new WebObj();
                }
            }

            public double GetObjectDouble(string section, string name)
            {
                object o = GetObjectValue(section, name);
                if (o == null) return 0;
                if (o.ToString() == "") return 0;
                return Convert.ToDouble(o);
            }
            public string GetObjectValue(string section, string name)
            {
                string key = (section + name).ToUpper();
                if (dictMembers.ContainsKey(key))
                {
                    string sOut = (dictMembers[key].value ?? "").ToString();
                    sOut = sOut.Replace("[amp]", "&");
                    sOut = sOut.Replace("[plus]", "+");
                    sOut = sOut.Replace("[percent]", "%");
                    return sOut;
                }
                else
                {
                    return string.Empty;
                }
            }

            public string GetObjectAttribute(string section, string name, string attribute)
            {
                string key = (section + name).ToUpper();
                if (dictMembers.ContainsKey(key))
                {
                    string sOut = dictMembers[key].Checked.ToString();
                    // TODO Make it get the attribute through reflection
                    return sOut;
                }
                else
                {
                    return string.Empty;
                }
            }


            public string GetObjectTag(string section, string name)
            {
                string key = (section + name + "_tag").ToUpper();
                if (dictMembers.ContainsKey(key))
                {
                    return dictMembers[key].value.ToString();
                }
                else
                {
                    return string.Empty;
                }
            }


            public void UpdateObject(string section, ref WebObj oWebObj)
            {
                string key = (section + oWebObj.name).ToUpper();

                if (key == string.Empty) return;
                // store the object in memory
                if (dictMembers.ContainsKey(key))
                {
                    dictMembers[key] = oWebObj;
                }
                else
                {
                    dictMembers.Add(key, oWebObj);
                }
            }

            public void SetObjectTag(string section, string name, string value)
            {
                string key = (section + name + "_tag").ToUpper();

                // Ensure the naming convention of the object includes the section+mode
                if (dictMembers.ContainsKey(key))
                {
                    WebObj f = dictMembers[key];
                    f.value = value;
                    dictMembers[key] = f;
                }
                else
                {
                    WebObj f = new WebObj();
                    f.name = name;
                    f.value = value;
                    dictMembers.Add(key, f);
                }
            }

            public string Substring2(string sSource, int iLength)
            {
                // This just truncates the string if its too small instead of throwing an error
                if (sSource.Length < iLength) iLength = sSource.Length;
                return sSource.Substring(0, iLength);
            }

            public void SetObjectValue(string section, string name, string value)
            {
                string key = (section + name).ToUpper();
                // Ensure the naming convention of the object includes the section+mode
                if (dictMembers.ContainsKey(key))
                {
                    WebObj f = dictMembers[key];
                    f.value = value;
                    dictMembers[key] = f;
                }
                else
                {
                    WebObj f = new WebObj();
                    f.name = name;
                    f.value = value;
                    dictMembers.Add(key, f);
                }
            }


            
            // Breadcrumb Trail
            public void AddBreadcrumb(string PageName, string Class, string Method, bool LandingPage)
            {
                // If it exists last, dont add it
                if (_breadcrumb != null)
                {
                    if (_breadcrumb.Count > 0)
                    {
                        for (int x = 0; x < _breadcrumb.Count; x++)
                        {
                            string sKey = _breadcrumb[x].Method + _breadcrumb[x].Class;
                            string mylocalkey = Method + Class;

                            if (sKey == mylocalkey)
                            {
                                //SetObjectValue("", "ApplicationMessage", PageName);
                                return;
                            }
                        }
                    }
                }
                Breadcrumb b = new Breadcrumb();
                b.PageName = PageName;
                b.Class = Class;
                b.Method = Method;
                b.LandingPage = LandingPage;
                _breadcrumb.Add(b);
                // Change the App message here:
                SetObjectValue("", "ApplicationMessage", PageName);
            }

            public Breadcrumb GetBreadcrumb()
            {
                return _breadcrumb[_breadcrumb.Count()];
            }

            public List<Breadcrumb> GetBreadcrumbTrail()
            {
                return _breadcrumb;
            }

            public string GetBreadcrumbTrailHTML()
            {
                string HTML = "";
                for (int i = 0; i < _breadcrumb.Count; i++)
                {
                    string sURL = " style='color:lime;cursor:pointer;' onclick=postdiv(this,'buttonevent','" + _breadcrumb[i].Class + "','" + _breadcrumb[i].Method + "','');";
                    HTML += "<span id=11 name=11 " + sURL + "> " + _breadcrumb[i].PageName + "</span>&nbsp;&nbsp;";
                }
                return HTML;
            }

        }

        public class Breadcrumb
        {
            public string PageName;
            public bool LandingPage = false;
            public string Class;
            public string Method;
        }

        public static class Mem
        {

        }
    }
