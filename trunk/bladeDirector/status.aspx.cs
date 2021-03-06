﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using bladeDirectorWCF;

namespace bladeDirector
{
    public partial class status : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (getCurrentServerURL(this) != null)
            {
                logInPrompt.Visible = false;
                try
                {
                    doLoggedInData();
                }
                catch (Exception)
                {
                    Session.Remove("serverURL");
                    throw;
                }
            }
            else
            {
                loggedInData.Visible = false;
                doServerSelectionDialog();
            }
        }

        private void doServerSelectionDialog()
        {
            if (ddlSelectServer.Items.Count != 0)
                return;

            // Populate the list of servers
            string[] allServerRaw = Properties.Settings.Default.servers.Split('\n');
            foreach (string serverRaw in allServerRaw)
            {
                string serverRawTrimmed = serverRaw.Trim();
                if (!serverRawTrimmed.Contains('#'))
                    continue;

                string serverDesc = serverRawTrimmed.Split('#')[0];
                string serverURL = serverRawTrimmed.Split('#')[1];

                ListItem newItem = new ListItem(serverDesc + " (" + serverURL + ")", serverURL);

                ddlSelectServer.Items.Add(newItem);
            }            
        }

        private void doLoggedInData()
        {
            // Populate the main page data.
            lblServerURL.Text = getCurrentServerURL(this);
            lblClientIP.Text = Request.UserHostAddress;
            TableRow headerRow = new TableRow();
            headerRow.Cells.Add(new TableHeaderCell() { Text = "" });
            headerRow.Cells.Add(new TableHeaderCell() { Text = "State" });
            headerRow.Cells.Add(new TableHeaderCell() { Text = "Blade IP" });
            headerRow.Cells.Add(new TableHeaderCell() { Text = "Time since last keepalive" });
            headerRow.Cells.Add(new TableHeaderCell() { Text = "Currently-selected snapshot" });
            headerRow.Cells.Add(new TableHeaderCell() { Text = "Current owner" });
            headerRow.Cells.Add(new TableHeaderCell() { Text = "Next owner" });
            headerRow.Cells.Add(new TableHeaderCell() { Text = "Links" });

            tblBladeStatus.Rows.Add(headerRow);

            using (BladeDirectorServices services = new BladeDirectorServices(getCurrentServerURL(this)))
            {
                lblServerBaseWebURL.Text = services.svc.getWebSvcURL();

                string[] allBladeIPs = services.svc.getAllBladeIP();

                foreach (string bladeIP in allBladeIPs)
                {
                    // Query the server for information about this blade

                    bladeSpec bladeInfo = services.svc.getBladeByIP_withoutLocking(bladeIP);

                    // Assemble the always-visible status row
                    TableRow newRow = new TableRow();

                    newRow.Cells.Add(makeTableCell(new ImageButton()
                    {
                        ImageUrl = "images/collapsed.png",
                        AlternateText = "Details",
                        OnClientClick = "javascript:toggleDetail($(this), " + bladeInfo.bladeID + "); return false;"
                    }));
                    newRow.Cells.Add(new TableCell() { Text = bladeInfo.state.ToString() });
                    newRow.Cells.Add(new TableCell() { Text = bladeInfo.bladeIP });
                    if (bladeInfo.lastKeepAlive == DateTime.MinValue)
                    {
                        newRow.Cells.Add(new TableCell() { Text = "(none)" });
                    }
                    else
                    {
                        string cssClass = "";
                        if (DateTime.Now - bladeInfo.lastKeepAlive > services.svc.getKeepAliveTimeout())
                            cssClass = "timedout";
                        TableCell cell = new TableCell
                        {
                            Text = formatDateTimeForWeb((DateTime.Now - bladeInfo.lastKeepAlive)),
                            CssClass = cssClass
                        };
                        newRow.Cells.Add(cell);
                    }
                    newRow.Cells.Add(new TableCell() { Text = bladeInfo.currentSnapshot });
                    newRow.Cells.Add(new TableCell() { Text = bladeInfo.currentOwner ?? "none" });
                    newRow.Cells.Add(new TableCell() { Text = bladeInfo.nextOwner ?? "none" });

                    string iloURL = String.Format("iloConsole.aspx?bladeIP={0}", bladeInfo.bladeIP);
                    HyperLink link = new HyperLink() { NavigateUrl = iloURL, Text = "iLo" };
                    TableCell iloURLtableCell = new TableCell();
                    iloURLtableCell.Controls.Add(link);
                    newRow.Cells.Add(iloURLtableCell);

                    tblBladeStatus.Rows.Add(newRow);

                    // Then populate the invisible-until-expanded details row.
                    tblBladeStatus.Rows.Add(makeDetailRow(services, bladeInfo));
                }

                // Finally, populate any log events.
                logEntry[] logEvents = services.svc.getLogEvents(100);
                int logID = 0;
                foreach (logEntry logEvent in logEvents)
                {
                    // Make the expandable row, with a little bit of info
                    TableRow tumbRow = new TableRow();
                    tumbRow.Cells.Add(makeTableCell(new ImageButton()
                    {
                        ImageUrl = "images/collapsed.png",
                        AlternateText = "Details",
                        OnClientClick = "javascript:toggleDetail($(this),  null); return false;"
                    }));

                    tumbRow.Cells.Add(new TableCell() { Text = logEvent.timestamp.ToString() });
                    tumbRow.Cells.Add(new TableCell() { Text = logEvent.threadName ?? "(unknown)" });
                    string thumbtext = logEvent.msg.Split('\n')[0];
                    if (thumbtext.Length > 50)
                        thumbtext = thumbtext.Substring(0, 50) + "...";
                    tumbRow.Cells.Add(new TableCell() { Text = thumbtext });
                    tblLogTable.Rows.Add(tumbRow);

                    // and then the full detail row.
                    TableRow detailRow = new TableRow();
                    detailRow.Cells.Add(new TableCell() { Text = logEvent.msg, ColumnSpan = 4});
                    detailRow.Style.Add("display", "none");
                    tblLogTable.Rows.Add(detailRow);

                    logID++;
                }
            }            
        }

        public static string getCurrentServerURL(Page thePage)
        {
            if (thePage.Session["serverURL"] == null)
                return null;
            return thePage.Session["serverURL"].ToString();
        }

        private string formatDateTimeForWeb(TimeSpan toshow)
        {
            if (toshow > TimeSpan.FromHours(24))
                return " > 24 hours ";
            return String.Format("{0}h {1}m {2}s", toshow.Hours, toshow.Minutes, toshow.Seconds );
        }

        private TableRow makeDetailRow(BladeDirectorServices svc, bladeSpec bladeInfo)
        {
            Table detailTable = new Table();

            TableRow miscTR = new TableRow();
            miscTR.Cells.Add(makeTableCell(
                new Label() { Text = "Blade DB ID: " },
                new Label() { Text = bladeInfo.bladeID.ToString() + "<br/>", CssClass = "fixedSize"},
                new Label() { Text = "ISCSI IP: "},
                new Label() { Text = bladeInfo.iscsiIP.ToString() + "<br/>", CssClass = "fixedSize" },
                new Label() { Text = "Kernel debug port: " },
                new Label() { Text = bladeInfo.kernelDebugPort.ToString() + "<br/>", CssClass = "fixedSize" },
                new Label() { Text = "Is currently having BIOS config deployed: " },
                new Label() { Text = bladeInfo.currentlyHavingBIOSDeployed.ToString() + "<br/>", CssClass = "fixedSize" },
                new Label() { Text = "Is currently acting as VM server: " },
                new Label() { Text = bladeInfo.currentlyBeingAVMServer.ToString() + "<br/>", CssClass = "fixedSize"}
            ));
            detailTable.Rows.Add(miscTR);

            // And add rows for any VMs.
            vmSpec[] VMs = svc.svc.getVMByVMServerIP_nolocking(bladeInfo.bladeIP);
            if (VMs.Length > 0)
            {
                TableRow VMHeaderRow = new TableRow();
                VMHeaderRow.Cells.Add(new TableHeaderCell() { Text = "VM name" });
                VMHeaderRow.Cells.Add(new TableHeaderCell() { Text = "Time since last keepalive" });
                VMHeaderRow.Cells.Add(new TableHeaderCell() { Text = "VM IP" });
                VMHeaderRow.Cells.Add(new TableHeaderCell() { Text = "Current owner" });
                VMHeaderRow.Cells.Add(new TableHeaderCell() { Text = "Kernel debug info" });
                detailTable.Rows.Add(VMHeaderRow);
            }
            foreach (vmSpec vmInfo in VMs)
            {
                TableRow thisVMRow = new TableRow();

                thisVMRow.Cells.Add(new TableCell() { Text = vmInfo.friendlyName });
                thisVMRow.Cells.Add(new TableCell() { Text = formatDateTimeForWeb((DateTime.Now - vmInfo.lastKeepAlive)) });
                thisVMRow.Cells.Add(new TableCell() { Text = vmInfo.VMIP });
                thisVMRow.Cells.Add(new TableCell() { Text = vmInfo.currentOwner });
                string dbgStr = String.Format("Port {0} key \"{1}\"", vmInfo.kernelDebugPort, vmInfo.kernelDebugKey) ;
                thisVMRow.Cells.Add(new TableCell() { Text = dbgStr });
                //thisVMRow.Cells.Add(new TableCell() { Text = vmInfo.currentSnapshot });
                detailTable.Rows.Add(thisVMRow);
            }

            TableHeaderRow toRet = new TableHeaderRow();
            TableCell paddingCell = new TableCell { CssClass = "invisible" };
            toRet.Cells.Add(paddingCell);
            TableCell tc = new TableCell();
            tc.ColumnSpan = 7;
            tc.Controls.Add(detailTable);
            toRet.Cells.Add(tc);
            toRet.Style.Add("display", "none");

            return toRet;
        }

        private static HtmlGenericControl makeInvisibleDiv()
        {
            HtmlGenericControl toRet = new HtmlGenericControl("div");
            toRet.Style.Add("display", "none");
            toRet.Attributes["class"] = "fixedSize";

            return toRet;
        }

        private Control makeImageButton(string text, string imageURL, string onClick, bool isVisible = true)
        {
            ImageButton toRet = new ImageButton()
            {
                ImageUrl = imageURL,
                AlternateText = text,
                OnClientClick = onClick,
                Visible = isVisible
            };

            return toRet;
        }

        private TableCell makeTableCell(params Control[] innerControl)
        {
            TableCell tc = new TableCell();
            foreach (Control control in innerControl)
                tc.Controls.Add(control);
            tc.VerticalAlign = VerticalAlign.Middle;
            return tc;
        }
        
        protected void cmdAddNode_Click(object sender, EventArgs e)
        {
            using (BladeDirectorServices services = new BladeDirectorServices(getCurrentServerURL(this)))
            {
                services.svc.addNode(txtNewNodeIP.Text, txtNewISCSI.Text, txtNewIloIP.Text, UInt16.Parse(txtNewPort.Text), "TODO.the.key.here", "name of " + txtNewNodeIP.Text);
            }
            Response.Redirect(Request.RawUrl);
        }

        protected void cmdLogin_Click(object sender, EventArgs e)
        {
            Session.Add("serverURL", ddlSelectServer.SelectedValue);
            Response.Redirect(Request.RawUrl);
        }

        protected void cmdLogout_Click(object sender, EventArgs e)
        {
            Session.Remove("serverURL");
            Response.Redirect(Request.RawUrl);
        }
    }
}