using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;

namespace HCPDotNetDAL
{
    public class Database
    {
        public string ConnectionString { get; set; }
        
        public Database()
        {
            ConnectionString = "server=localhost;user id=root;persistsecurityinfo=True;database=dotnet";
        }


        public DataTable GetDataTable(string sql, List<MySqlParameter> parameters, bool isStoreProc)
        {
            DataTable table = new DataTable();
            using (var conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.CommandTimeout = 600;
                    cmd.CommandType = (isStoreProc) ? CommandType.StoredProcedure : CommandType.Text;
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.Add(param);
                        }
                    }
                    table.Load(cmd.ExecuteReader());
                }
            }
            return table;
        }

        public DataTable GetDataTable(string sql, List<MySqlParameter> parameters)
        {
            return GetDataTable(sql, parameters, false);
        }

        public DataTable GetDataTableSp(string sql, List<MySqlParameter> parameters)
        {
            return GetDataTable(sql, parameters, true);
        }

        public DataTable GetDataTable(string sql)
        {
            return GetDataTable(sql, null);
        }

        public DataTable GetDataTableSp(string sql)
        {
            return GetDataTableSp(sql, null);
        }

        public DataRow GetDataRow(string sql, List<MySqlParameter> parameters)
        {
            var table = GetDataTable(sql, parameters);

            if (table.Rows?.Count == 1)
            {
                return table.Rows[0];
            }
            return null;
        }

        public int UpdateDatabase(string sql, List<MySqlParameter> parameters)
        {
            int rowsAffected = 0;
            using (var conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                
                using (var cmd = new MySqlCommand(sql,conn))
                {
                    cmd.CommandTimeout = 600;
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.Add(param);
                        }
                    }
                    rowsAffected = cmd.ExecuteNonQuery();
                }
            }
            return rowsAffected;
        }

        public int UpdateDatabase(MySqlCommand cmd, List<MySqlParameter> parameters)
        {
            int rowsAffected = 0;
            
            cmd.CommandTimeout = 600;
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.Add(param);
                }
            }
            rowsAffected = cmd.ExecuteNonQuery();
               
            return rowsAffected;
        }

    }
}
