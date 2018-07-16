using System.Collections.Generic;
using UnityEngine;
using System.IO;

#if !UNITY_EDITOR
using System.Threading.Tasks;
using Windows.Storage;
using System;
#endif

public class Logger : MonoBehaviour {
	public string filepath;
	private string fullpath;

	public int UserID = 0;

	private FileStream fileStream;
	private StreamWriter streamWriter;
    private string filename;
	public void Initialize(string logname) {
		filename = UserID + "_" + logname + "_" + System.DateTime.Now.ToString("_yyMMdd_hhmmss") + ".csv";
		//filepath = Application.dataPath;
		fullpath = filepath + "/" + filename;

		fileStream = new FileStream(fullpath, FileMode.Append);
		streamWriter = new StreamWriter(fileStream);
	}

	//Used for crash recovery - results in file always having the same name
	public void InitializeWithoutDate(string logname) {
		filename = logname + ".csv";
		filepath = Application.persistentDataPath;
		Debug.Log(filepath);
		fullpath = filepath + "/" + filename;

		fileStream = new FileStream(fullpath, FileMode.Append);
		streamWriter = new StreamWriter(fileStream);
	}

	public void writeToLog(string exportString) {
		if (streamWriter == null) {
			return;
		}

		long currentTimestamp = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;

		streamWriter.WriteLine(currentTimestamp + ";" + UserID + ";" + exportString);
		streamWriter.Flush();
	}

	public void writeHeader(string header) {
		if (streamWriter == null) {
			return;
		}

		streamWriter.WriteLine(header);
		streamWriter.Flush();
	}

	void OnApplicationPause(bool pauseStatus) {
		Debug.Log("Application paused");
	}

	void OnApplicationQuit() {
		Debug.Log("Application ending after " + Time.time + " seconds");
	}

}
