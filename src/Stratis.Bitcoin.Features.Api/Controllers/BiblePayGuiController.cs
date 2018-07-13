using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BiblePayGUI;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Stratis.Bitcoin.Features.Api
{
    public class BiblePayGuiController : Controller
    {
        BiblePayGUI.SystemObject sys = new SystemObject("wallet");

        USGD u = new USGD();
        public string ReIndexBlockChain()
        {
            return "This action clears the blocks and triggers a reindex.";
        }

        private string GetBanner(string sDomain, string sSiteBanner)
        {
            string html = "<div id='top_banner'>";
            html += "<table width='100%' class='title2'>"
                + "      <tr><td  rowspan=2 width=15%>"
                + "                <img class='content companylogo' id='org_image' src='" + sDomain + "Images/logo.png' width=90 height=90 />"
                + "           </td> "
                + "           <td width=20% nowrap align=left>"
                +"                   <h1><bold><span id='org_name'>" + sSiteBanner + "</span></h1>"
                +"            </td>"
                +"<td width=50%>&nbsp;</td>"
                +"<td width=15% nowrap align=left></td>"
                +"<td width=8%>&nbsp;</td>"
                +"<td width=8%>&nbsp;</td></tr>"
                +"<tr>"
                +"     <td width=37% nowrap align=left>&nbsp;</td><td align=left>&nbsp;</td><td>&nbsp;</td>"
                +"     <td>&nbsp;</td>"
                +"</tr>"
                +"<tr>"
                +"     <td></td><td width=10%></td>"
                +"     <td><h7><span name=ApplicationMessage>Application Message</span></h7><span align=right><div id=12></div></span></td>"
                +"</tr>"
                +"</table>"
                +"</div>";
            return html;
        }
        public ActionResult StratisWeb()
        {
            
            string sPostData = new System.IO.StreamReader(this.HttpContext.Request.Body).ReadToEnd();
            sPostData = sPostData.Replace("post=", "");

            if (sPostData == string.Empty)
            {
                // This is the initial call during page load
                string style = "";
                USGDFramework.Data d = new USGDFramework.Data();
                string sURL1 = this.HttpContext.Request.QueryString.ToString();
                string sMenu = d.GetTopLevelMenu(sURL1);
                if (sys.Theme == null) sys.Theme = "Biblepay";
                string sDomain = this.HttpContext.Request.Scheme + "://" + this.HttpContext.Request.Host + "/StratisWeb/";
                string sTheme = sDomain + "css/" + sys.Theme + ".css";
                string sCss = " <link rel=stylesheet href='" + sDomain + "css/jquery-ui.css'> "
                    + "<link rel=stylesheet href='" + sTheme + "'>";
                // Dynamic Top banner 
                string sSiteURL = this.HttpContext.Request.QueryString.ToString();
                string sSiteBanner = "Biblepay Wallet";

                string sBanner = GetBanner(sDomain, sSiteBanner);
                string sJQuery= "<script src='" + sDomain + "Scripts/jquery-1.12.4.js'></script>";

                sJQuery += "<script src='" + sDomain + "Scripts/jquery-ui.js'></script>";
                //sJQuery += "<script src='" + sDomain + "Scripts/jquery.uploadify.js'></script>";
                sJQuery += "<script src='" + sDomain + "Scripts/Core.js'></script>";
                sJQuery += "<script src='" + sDomain + "Scripts/jquery.contextMenu.js'></script>";
                //sJQuery += "<script src='" + sDomain + "Scripts/featherlight.js'></script>";
                //sJQuery += "<script src='" + sDomain + "Scripts/featherlight.gallery.js'></script>";

                string sOut = "<html><head>" + style + sJQuery 
                    + sCss + " </head><body onload=formload();>" + sBanner
                    + "<table><tr valign=top><td width=10%>" 
                    + sMenu + "</td><td width=2%>&nbsp;</td><td width=86%>"
                    + "<div name=divbreadcrumb></div><div name=1>"
                    +"<div name=2><div name=3></div></div></div></td><td width=2%>&nbsp;</td></tr></table></body></html>";

                ContentResult c = new ContentResult();
                c.Content = sOut;
                c.ContentType = "html";

                return c;
            }

            ContentResult c1 = new ContentResult();
            string s1 = u.HandleRequest(sPostData, sys);
            c1.Content = s1;
            return c1;
           
        }

    }
}
