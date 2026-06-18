using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace BHG_DR_LIB
{
    public class SQLSvrManager
    {
        #region Hidden
        public string ConnectionString = "Data Source=bhgazuresql01.database.windows.net;Authentication=Active Directory Password;Initial Catalog=BHG_DR;Persist Security Info=True;User ID=ayxbhg@bhgrecovery.onmicrosoft.com;Password=Alteryx#BHG2021";
        #endregion
        private SqlConnection GetSqlConnection(string strCon)
        {
            if (strCon != null)
            {
                return new SqlConnection(strCon);
            }
            else { return null; }
        }
        public SqlDataAdapter GetSqlDataAdapter(string strcmd, SqlConnection sqlCon)
        {
            return new SqlDataAdapter(strcmd, sqlCon);
        }
        public DataTable GetTableData(string tblname, string strCmd, string dbCon)
        {
            DataTable tbl = new DataTable(tblname);
            //try
            {
                SqlDataAdapter sDA = GetSqlDataAdapter(strCmd, GetSqlConnection(dbCon));
                sDA.SelectCommand.CommandTimeout = 9000;
                sDA.Fill(tbl);
            }
            //catch (Exception e)
            //{
            //    Console.WriteLine("Error: " + e.Message);
            //}
            return tbl;
        }
        public DataTable GetTableData(string tblname, string strCmd, string dbCon, int ErrorCnt = 0)
        {
            DataTable tbl = new DataTable(tblname);
            try
            {
                SqlDataAdapter sDA = GetSqlDataAdapter(strCmd, GetSqlConnection(dbCon));
                sDA.SelectCommand.CommandTimeout = 9000;
                sDA.Fill(tbl);
            }
            catch (Exception e)
            {
                ErrorCnt += 1;
                Console.WriteLine("Error: " + e.Message);
                if (ErrorCnt < 3)
                {
                    GetTableData(tblname, strCmd, dbCon, ErrorCnt);
                }
            }
            return tbl;
        }
        public DataTable GetTableData(SqlCommand cmd)
        {
            DataTable tbl = new DataTable();
            try
            {
                SqlDataAdapter sda = GetSqlDataAdapter(cmd.CommandText, cmd.Connection);
                sda.SelectCommand.CommandTimeout = 9000;
                sda.Fill(tbl);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
            return tbl;
        }
        public DataTable GetTableData(SqlDataAdapter sda)
        {
            DataTable tbl = new DataTable();
            try
            {
                sda.SelectCommand.CommandTimeout = 900;
                sda.Fill(tbl);
            }
            catch(Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
            return tbl;
        }
        //public Task<DataTable> async GetTableDataAsync(string tblname, string strCmd, string dbCon)
        //{
        //    DataTable tbl = new DataTable(tblname);
        //    try
        //    {
        //        Task.Factory.StartNew(() =>
        //        {
        //            SqlDataAdapter sDA = GetSqlDataAdapter(strCmd, GetSqlConnection(dbCon));
        //            sDA.SelectCommand.CommandTimeout = 900;
        //            sDA.Fill(tbl);
        //        });
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine("Error: " + e.Message);
        //    }
        //    return Task<tbl>;
        //}
        public int ExeSqlCmd(string strCmd, string strConMe)
        {
            int Result = 0;
            SqlConnection ConMe = GetSqlConnection(strConMe);
            SqlCommand sqlCmd = new SqlCommand(strCmd, ConMe)
            {
                CommandTimeout = 9999
            };
            try
            {
                sqlCmd.Connection.Open();
                Result = sqlCmd.ExecuteNonQuery();
                //Console.WriteLine(strCmd);
                //Console.WriteLine("The Result =  " + Result.ToString());
                //Console.ReadKey();
            }
            catch (Exception e)
            {
                //ErrorLog(strCmd, e.Message);
                //Console.WriteLine(strCmd);
                Console.WriteLine(e.Message);
                //Console.ReadKey();
            }
            finally
            {
                sqlCmd.Connection.Close();
            }
            return Result;
        }
        public Models.RCodes ExeSqlCmd(string strCmd, string strConMe, bool ErrorCheck = false)
        {
            Models.RCodes Result = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = 0
            };

            SqlConnection ConMe = GetSqlConnection(strConMe);
            SqlCommand sqlCmd = new SqlCommand(strCmd, ConMe)
            {
                CommandTimeout = 9999
            };
            try
            {
                sqlCmd.Connection.Open();
                Result.RowsProcessed = sqlCmd.ExecuteNonQuery();
                //Console.WriteLine(strCmd);
                //Console.WriteLine("The Result =  " + Result.ToString());
                //Console.ReadKey();
            }
            catch (Exception e)
            {
                Result.IsResult = false;
                Result.ExceptMsg = e.Message;
                if (e.InnerException.Message != null)
                {
                    Result.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            finally
            {
                sqlCmd.Connection.Close();
            }
            return Result;
        }
        public DataTable ExecStrPro(string strCmd, string ParaName, string sc, string strConMe)
        {
            DataTable Results = new DataTable();
            SqlDataAdapter sda = new SqlDataAdapter();
            sda.SelectCommand = new SqlCommand(strCmd, GetSqlConnection(strConMe));
            sda.SelectCommand.CommandType = CommandType.StoredProcedure;
            sda.SelectCommand.CommandTimeout = 9000;
            SqlParameter paras = new SqlParameter();
            paras.Value = sc;
            paras.ParameterName = ParaName;
            sda.SelectCommand.Parameters.Add(paras);
            try
            {
                sda.Fill(Results);
            }
            catch (Exception e)
            {
                //ErrorLog(strCmd, e.Message);
                Console.WriteLine(e.Message);
            }
            finally
            {
                
            }
            return Results;
        }
    }
}
