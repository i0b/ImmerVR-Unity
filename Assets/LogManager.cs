using UnityEngine;
using System.Collections;
using System.IO;

#if !UNITY_EDITOR
using System.Threading.Tasks;
using Windows.Storage;
using System;
#endif

namespace iiscommon.Logging 
{
    public class LogManager // : Utilities.Singleton<LogManager>
    {
        private string fileName_, filePath_;
        private System.DateTime logStartTime_;

        private bool addTimestamp_ = false;

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void Initialize(string logName, bool addTimestamp)
        {
            addTimestamp_ = addTimestamp;

            logStartTime_ = System.DateTime.Now;

            fileName_ = logName + System.DateTime.Now.ToString("_yyMMdd_hhmmss") + ".log";
            filePath_ = Application.persistentDataPath;

            string fullPath = filePath_ + "/" + fileName_;

            Debug.Log(string.Format("Saving log file to: {0}", fullPath));

            using (FileStream fs = new FileStream(fullPath, FileMode.CreateNew))
            {
                using (StreamWriter outputFile = new StreamWriter(fs))
                {

                }
            }
        }

        public void WriteToLog(string line)
        {
            string fullPath = filePath_ + "/" + fileName_;
            using (FileStream fs = new FileStream(fullPath, FileMode.Append))
            {
                using (StreamWriter outputFile = new StreamWriter(fs))
                {
                    System.DateTime currentTime = System.DateTime.Now;
                    System.TimeSpan elapsedTime = currentTime - logStartTime_;

                    string timestamp = elapsedTime.TotalSeconds.ToString();
                    string outputLine = line + ", ";
                    if (addTimestamp_)
                    {
                        outputLine += timestamp;
                    }

                    outputFile.WriteLine(outputLine);
                }
            }
        }
    }
}