using System;
using System.Data;
using System.Collections.Generic;
using System.Text;

namespace HCPDotNetDAL
{
    public class SettingsDA : IDisposable
    {
        private Database CreateDatabase()
        {
            return new Database() { ConnectionString = ConnectionString };
        }

        public string ConnectionString { get; set; }

        public void Dispose()
        {

        }

        public Dictionary<string,string> GetAllSettings()
        {
            var dict = new Dictionary<string, string>();
            Database db = CreateDatabase();
            var table = db.GetDataTable("SELECT * from `dotnet`.`smp_settings`", null);
            foreach(DataRow row in table.Rows )
            {
                string settingName = row["SettingName"] as string;
                string settingValue = row["SettingValue"] as string;
                
                if(!dict.ContainsKey(settingName))
                {
                    dict.Add(settingName, settingValue);
                }
            }
            return dict;
        }
    }
}
