using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BiblePayGUI
{
    public class Home : USGDGui
    {
        public Home(SystemObject S) : base(S)
        {
            
        }

        public WebReply Leaderboard()
        {
            string sSourceTable = "Leaderboardmainsummary";
            SQLDetective s = Sys.GetSectionSQL("Leaderboard View", sSourceTable, string.Empty);
            s.WhereClause = "1=1";
            s.OrderBy = "Added";
            string sql = s.GenerateQuery();
            Weblist w = new Weblist(Sys);
            w.bShowRowHighlightedByUserName = true;
            w.bSupportCloaking = true;
            WebReply wr = w.GetWebList(sql, "Leaderboard View", "Leaderboard View", "", "Leaderboard", this, false);
            return wr;
        }
        
        public WebReply NetworkMain()
        {
            Dialog d = new Dialog(Sys);
            WebReply wr = d.CreateDialog("Network", "Switch Network", "Network changed to Main", 0, 0);
            Sys.NetworkID = "main";
            return wr;
        }

        public WebReply SettingsEdit()
        {
            double dBalance = 1;
            Sys.SetObjectValue("Settings Edit", "Balance", dBalance.ToString());
            Section SettingsEdit = new Section("Settings Edit", 2, Sys, this);
            Edit geDefaultWithdrawalAddress = new Edit("Settings Edit", "DefaultWithdrawalAddress", Sys);
            geDefaultWithdrawalAddress.CaptionText = "Default Receive Address:";
            geDefaultWithdrawalAddress.TextBoxStyle = "width:420px";
            SettingsEdit.AddControl(geDefaultWithdrawalAddress);
            SettingsEdit.AddBlank();
            Edit geBalance = new Edit("Settings Edit", "Balance", Sys);
            geBalance.CaptionText = "Balance:";
            geBalance.Type = Edit.GEType.Text;
            geBalance.TextBoxAttribute = "readonly";
            geBalance.TextBoxStyle = clsStaticHelper.msReadOnly;
            SettingsEdit.AddControl(geBalance);
            SettingsEdit.AddBlank();
            // CPID

            Edit geCPID = new Edit("Settings Edit", "CPID", Sys);
            geCPID.CaptionText = "CPID:";
            geCPID.TextBoxStyle = "width:420px";
            SettingsEdit.AddControl(geCPID);
            
            // Theme

            Edit ddTheme = new Edit("Settings Edit", "Theme", Sys);
            ddTheme.Type = Edit.GEType.Lookup;
            ddTheme.CaptionText = "Theme:";
            ddTheme.LookupValues = new List<SystemObject.LookupValue>();
            SystemObject.LookupValue i1 = new SystemObject.LookupValue();
            i1.ID = "Biblepay";
            i1.Value = "Biblepay";
            i1.Caption = "Biblepay";
            ddTheme.LookupValues.Add(i1);

            SystemObject.LookupValue i2 = new SystemObject.LookupValue();
            i2.ID = "Dark";
            i2.Value = "Dark";
            i2.Caption = "Dark";
            ddTheme.LookupValues.Add(i2);
            SettingsEdit.AddBlank();
            SettingsEdit.AddControl(ddTheme);
            SettingsEdit.AddBlank();
            Edit geBtnSave = new Edit("Settings Edit", Edit.GEType.Button, "btnSettingsSave", "Save", Sys);
            SettingsEdit.AddControl(geBtnSave);
            SettingsEdit.AddBlank();
            return SettingsEdit.Render(this, true);
        }

       
        public WebReply btnSettingsSave_Click()
        {
            Dialog d = new Dialog(Sys);
            string sCPID = Sys.GetObjectValue("Settings Edit", "CPID");
            string sNarr = "Record Updated. CPID (" + sCPID + ")";
            WebReply wr = d.CreateDialog("S1", "Settings Edit", sNarr, 0, 0);
            return wr;
        }

    }
}
