using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace PurgeDuplicateSearchResults
{
    public class PartNumberDup
    {
        private long filesDeleted = 0;

        public long FilesDeleted
        {
            get { return filesDeleted; }
        }


        public PartNumberDup()
        {
            DupFiles = new List<string>();
        }


        public List<string> DupFiles { get; set; }
        public void RemoveDups(int threshold)
        {
            if (DupFiles == null || DupFiles.Count <= threshold) return;


            foreach(string file in DupFiles)
            {
                try
                {
                    if (File.Exists(file))
                    {
                        var fileInfo = new FileInfo(file);
                        Console.WriteLine($"Deleting {file} of size {fileInfo.Length} created on {fileInfo.CreationTime}");
                        File.Delete(file);
                        filesDeleted++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}
