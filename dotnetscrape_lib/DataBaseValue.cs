using System;
using System.Data;


namespace dotnetscrape_lib
{
    public static class DataBaseValue
    {
        public static T GetValue<T>(DataRow row, string columnName, object defaultValue)
        {
            if (row == null)
                throw new NullReferenceException("row cannot be null");
            if (row.Table == null)
                throw new NullReferenceException("row.Table cannot be null");
            if (!row.Table.Columns.Contains(columnName)) return (T)defaultValue;
            var isNullable = (Nullable.GetUnderlyingType(typeof(T)) != null);
            var t = (isNullable) ? Nullable.GetUnderlyingType(typeof(T)) : typeof(T);
            if (row[columnName].GetType() == t && !row.IsNull(columnName))
            {
                object val = row[columnName];
                if (val.GetType() == typeof(string))
                {
                    val = val.ToString().Trim();
                    return (T)val;
                }
                else
                {
                    return (T)row[columnName];
                }
            }
            return (row.IsNull(columnName) && isNullable) ? default : (T)defaultValue;
        }
    }
}
