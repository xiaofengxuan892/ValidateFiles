using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Ookii.Dialogs;
using UnityEngine;
using UnityEngine.UI;
using Application = UnityEngine.Application;
using Button = UnityEngine.UI.Button;

public class CompareFiles : MonoBehaviour
{
    private readonly string LastDirPathKey = "lastSelectDirForCompare";

    private string dirPath;
    private string[] filePaths;
    private InputField dirInputText;
    private Text fileTotalNumTxt, resultText, exceptionTxt;

    void Start() {
        InitUIBinding();
        UpdateView();
    }

    void InitUIBinding() {
        var dirPathInputConfig = "Canvas/PanelTop/InputField";
        var fileTotalNumConfig = "Canvas/PanelTop/FileTotalNumText";
        var resultConfig = "Canvas/PanelDetails/ResultText";
        var exceptionConfig = "Canvas/PanelDetails/ExceptionText";
        dirInputText = GameObject.Find(dirPathInputConfig).GetComponent<InputField>();
        fileTotalNumTxt = GameObject.Find(fileTotalNumConfig).GetComponent<Text>();
        resultText = GameObject.Find(resultConfig).GetComponent<Text>();
        exceptionTxt = GameObject.Find(exceptionConfig).GetComponent<Text>();

        var btnFolderConfig = "Canvas/PanelTop/BtnFolder";
        var btnCompare = "Canvas/PanelDetails/BtnCompare";
        GameObject.Find(btnFolderConfig).GetComponent<Button>().onClick.AddListener(SelectFolderWindow);
        GameObject.Find(btnCompare).GetComponent<Button>().onClick.AddListener(CompareFilesByMd5);
    }

    void UpdateView() {
        var lastDirValue = PlayerPrefs.GetString(LastDirPathKey);
        if (string.IsNullOrEmpty(lastDirValue)) {
            return;
        }

        dirInputText.text = lastDirValue;
        filePaths = Directory.GetFiles(lastDirValue, "*.txt");
        fileTotalNumTxt.text = filePaths.Length.ToString();
    }

    #region 工具方法
    void CompareFilesByMd5() {
        var dirPath = dirInputText.text;
        if (string.IsNullOrEmpty(dirPath)) {
            exceptionTxt.text = "路径为空，请先选择目标文件夹";
            resultText.text = "";
            return;
        }

        if (!Directory.Exists(dirPath)) {
            exceptionTxt.text = "该文件夹不存在，请重新选择";
            resultText.text = "";
            return;
        }

        filePaths = Directory.GetFiles(dirPath, "*.txt");
        if (filePaths.Length <= 0) {
            exceptionTxt.text = "该文件夹下没有可以比对的日志文件";
            resultText.text = "";
            return;
        }

        exceptionTxt.text = "";
        //统计文件md5
        Dictionary<string, List<string>> md5DicList = new Dictionary<string, List<string>>();
        for (int i = 0; i < filePaths.Length; ++i) {
            var fileMd5 = GetMD5HashOfFile(filePaths[i]);
            var fileName = Path.GetFileNameWithoutExtension(filePaths[i]);
            if (md5DicList.ContainsKey(fileMd5)) {
                md5DicList[fileMd5].Add(fileName);
            }
            else {
                var fileNameListByMd5 = new List<string> {fileName};
                md5DicList.Add(fileMd5, fileNameListByMd5);
            }
        }

        //UI刷新
        StringBuilder finalLabel = new StringBuilder();
        foreach (var temp in md5DicList) {
            StringBuilder sb = new StringBuilder();
            sb.Append("<color=yellow>MD5:</color> ");
            sb.Append(temp.Key);
            sb.Append("  <color=blue>count:</color> ");
            sb.Append(temp.Value.Count);
            sb.Append("  <color=red>代表文件name:</color> ");
            sb.Append(temp.Value[temp.Value.Count - 1]);

            finalLabel.Append(sb);
            finalLabel.Append("\n");
        }
        resultText.text = finalLabel.ToString();
    }

    string GetMD5HashOfFile(string filePath) {
        using (FileStream fs = new FileStream(filePath, FileMode.Open)) {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] val = md5.ComputeHash(fs);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < val.Length; ++i) {
                sb.Append(val[i].ToString("X"));
            }

            return sb.ToString();
        }
    }

    void SelectFolderWindow() {
        var folderWindow = new VistaFolderBrowserDialog() {
            Description = "请选择目标文件夹",
            ShowNewFolderButton = false,
            RootFolder = Environment.SpecialFolder.MyComputer,
        };
        if (folderWindow.ShowDialog() == DialogResult.OK) {
            dirPath = folderWindow.SelectedPath;
            dirInputText.text = dirPath;
            filePaths = Directory.GetFiles(dirPath, "*.txt");
            fileTotalNumTxt.text = filePaths.Length.ToString();
            exceptionTxt.text = "";
            //记录下最近选择的目录，下次打开默认选择上一次的目录
            PlayerPrefs.SetString(LastDirPathKey, dirPath);
        }
    }

    void SelectFilesWindow() {
        var fileWindow = new VistaOpenFileDialog() {
            InitialDirectory = "file://" + Application.dataPath,
            Filter = "日志文件|*.txt",
            RestoreDirectory = true,
            FilterIndex = 1,
            Multiselect = true
        };
        if (fileWindow.ShowDialog() == DialogResult.OK) {
            //显示文件目录
            dirPath = Path.GetDirectoryName(fileWindow.FileNames[0]);
            dirInputText.text = dirPath;

            //该目录下所有文件
            filePaths = fileWindow.FileNames;
            fileTotalNumTxt.text = filePaths.Length.ToString();
            exceptionTxt.text = "";
        }
    }
    #endregion
}