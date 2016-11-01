﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace bladeDirector
{
    public partial class status : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            TableRow headerRow = new TableRow();
            headerRow.Cells.Add(new TableHeaderCell() {Text = "State"});
            headerRow.Cells.Add(new TableHeaderCell() { Text = "Blade IP" });
            headerRow.Cells.Add(new TableHeaderCell() { Text = "Time since last keepalive"});
            headerRow.Cells.Add(new TableHeaderCell() { Text = "Currently-selected snapshot" });
            headerRow.Cells.Add(new TableHeaderCell() { Text = "Current owner" });
            headerRow.Cells.Add(new TableHeaderCell() { Text = "Next owner" });

            tblBladeStatus.Rows.Add(headerRow);

            List<bladeOwnership> allBladeInfo = hostStateDB.getAllBladeInfo();

            foreach (bladeOwnership bladeInfo in allBladeInfo)
            {
                TableRow newRow = new TableRow();
                newRow.Cells.Add(new TableCell() {Text = bladeInfo.state.ToString()});
                newRow.Cells.Add(new TableCell() {Text = bladeInfo.bladeIP});
                if (bladeInfo.lastKeepAlive == DateTime.MinValue)
                {
                    newRow.Cells.Add(new TableCell() {Text = "(none)"});
                }
                else
                {
                    string cssClass = "";
                    if (DateTime.Now - bladeInfo.lastKeepAlive > hostStateDB.keepAliveTimeout)
                        cssClass = "timedout";
                    TableCell cell = new TableCell
                    {
                        Text = (DateTime.Now - bladeInfo.lastKeepAlive).ToString(),
                        CssClass = cssClass
                    };
                    newRow.Cells.Add(cell);
                }
                newRow.Cells.Add(new TableCell() { Text = bladeInfo.currentSnapshot});
                newRow.Cells.Add(new TableCell() { Text = bladeInfo.currentOwner ?? "none" });
                newRow.Cells.Add(new TableCell() { Text = bladeInfo.nextOwner ?? "none" });

                tblBladeStatus.Rows.Add(newRow);

                List<string> logEvents = hostStateDB.getLogEvents();
                foreach (string logEvent in logEvents)
                    lstLog.Items.Add(logEvent);
            }
        }

        protected void cmdReset_Click(object sender, EventArgs e)
        {
            hostStateDB.resetAll();
        }

        protected void cmdAddNode_Click(object sender, EventArgs e)
        {
            bladeOwnership newBlade = new bladeOwnership(txtNewNodeIP.Text, txtNewISCSI.Text, txtNewIloIP.Text, ushort.Parse(txtNewPort.Text), "-clean");
            hostStateDB.addNode(newBlade);
        }
    }
}