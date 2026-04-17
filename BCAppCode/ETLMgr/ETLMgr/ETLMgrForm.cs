using Microsoft.Data.SqlClient;
using System.Data;
using System.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ETLMgr
{
    public partial class ETLMgrForm : Form
    {
        public SqlConnection dbCon = new SqlConnection();
        
        public ETLMgrForm()
        {
            InitializeComponent();
            dbCon.ConnectionString = "Data Source=bhgazuresql01.database.windows.net;Initial Catalog=BHG_DR;Persist Security Info=True;User ID=ayxbhg@bhgrecovery.onmicrosoft.com;Password=Alteryx#BHG2021;Authentication=\"Active Directory Password\"";
            //dbCon.Credential = new SqlCredential("ayxbhg@bhgrecovery.onmicrosoft.com", System "Alteryx#BHG2021");
        }

        private void btnDailyChecksRefresh_Click(object sender, EventArgs e)
        {
            DataTable tbl = new DataTable();
            SqlDataAdapter sDA = new SqlDataAdapter("select o.TaskId, o.TaskName, o.RunAt" +
                ", [TaskStatus] = case when o.Status = 17 then 'Pending' when o.Status = 18 then 'Processing' when o.Status = 19 then 'Completed' when o.Status = 20 then 'Error' else 'Unknown' end" +
                ", o.Duration, o.SiteCode, o.WorkDate, o.[RowCount]" +
                ", Remaining = isnull((select count(1) from tsk.tbl_Tasks2 where ParentTaskId = o.TaskId and Status = 17), 0)" +
	            ", Failed = isnull((select count(1) from tsk.tbl_Tasks2 where ParentTaskId = o.TaskId and Status = 20), 0)" +
	            ", o.LastModAt from tsk.tbl_Tasks2 o " +
                "where (RowState = 24 and status in (17, 18, 19) and ParentTaskId is null and WorkDate = '" +
                dtpWorkDate.Value.ToShortDateString() + 
                "' or (Status in (17, 18) and RowState = 24 and ParentTaskId is null)) " + 
                " order by LastModAt desc", dbCon);
            sDA.SelectCommand.CommandTimeout = 9000;
            sDA.Fill(tbl);
            dgvDailyChecks.DataSource = tbl;
            foreach (DataColumn c in tbl.Columns)
            {
                switch (c.ColumnName.ToLower())
                {
                    case "rowstate":
                        dgvDailyChecks.Columns[c.ColumnName].Visible = false;
                        break;
                    case "lastmodby":
                        dgvDailyChecks.Columns[c.ColumnName].Visible = false;
                        break;
                    case "parenttaskid":
                        dgvDailyChecks.Columns[c.ColumnName].Visible = false;
                        break;
                    case "constr":
                        dgvDailyChecks.Columns[c.ColumnName].Visible = false;
                        break;
                    case "wherecondition":
                        dgvDailyChecks.Columns[c.ColumnName].Visible = false;
                        break;
                    case "sortorder":
                        dgvDailyChecks.Columns[c.ColumnName].Visible = false;
                        break;
                    case "srcschema":
                        dgvDailyChecks.Columns[c.ColumnName].Visible = false;
                        break;
                    case "fromtblvw":
                        dgvDailyChecks.Columns[c.ColumnName].Visible = false;
                        break;
                    case "connectionid":
                        dgvDailyChecks.Columns[c.ColumnName].Visible = false;
                        break;
                    case "schemaversion":
                        dgvDailyChecks.Columns[c.ColumnName].Visible = false;
                        break;
                    case "rowtrax":
                        dgvDailyChecks.Columns[c.ColumnName].Visible = false;
                        break;
                    case "pq":
                        dgvDailyChecks.Columns[c.ColumnName].Visible = false;
                        break;
                    case "isnewschema":
                        dgvDailyChecks.Columns[c.ColumnName].Visible = false;
                        break;
                    case "actionkey":
                        dgvDailyChecks.Columns[c.ColumnName].Visible = false;
                        break;
                    case "actionstepkey":
                        dgvDailyChecks.Columns[c.ColumnName].Visible = false;
                        break;
                    case "oncompletion":
                        dgvDailyChecks.Columns[c.ColumnName].Visible = false;
                        break;
                    case "onerror":
                        dgvDailyChecks.Columns[c.ColumnName].Visible = false;
                        break;
                }
            }
            
        }
    }
}
