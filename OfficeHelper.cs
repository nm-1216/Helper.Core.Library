using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helper.Core.Library
{
    public class OfficeHelper
    {
        private const string WordPathNullException = "Word 文档路径或者 HTML 文件路径不能为空！";
        private const string ExcelPathNullException = "Excel 文档路径或者 HTML 文件路径不能为空！";

        #region 对外公开方法
        /// <summary>
        /// Word 转换成 HTML 文件
        /// </summary>
        /// <param name="wordPath">Word 文档路径</param>
        /// <param name="htmlPath">Html 文件路径</param>
        public static void WordToHtmlFile(string wordPath, string htmlPath)
        {
            if (string.IsNullOrEmpty(wordPath)) throw new Exception(WordPathNullException);

            Microsoft.Office.Interop.Word.ApplicationClass applicationClass = new Microsoft.Office.Interop.Word.ApplicationClass();
            Type wordType = applicationClass.GetType();
            Microsoft.Office.Interop.Word.Documents documents = applicationClass.Documents;

            // 打开文件  
            Type documentsType = documents.GetType();

            Microsoft.Office.Interop.Word.Document document = (Microsoft.Office.Interop.Word.Document)documentsType.InvokeMember("Open",
            System.Reflection.BindingFlags.InvokeMethod, null, documents, new Object[] { wordPath, true, true });

            // 转换格式，另存为html  
            Type documentType = document.GetType();

            string directoryPath = Path.GetDirectoryName(htmlPath);
            string fileDirectoryPath = Path.Combine(directoryPath, Path.GetFileNameWithoutExtension(htmlPath));

            /*下面是Microsoft Word 9 Object Library的写法，如果是10，可能写成： 
              * docType.InvokeMember("SaveAs", System.Reflection.BindingFlags.InvokeMethod, 
              * null, doc, new object[]{saveFileName, Word.WdSaveFormat.wdFormatFilteredHTML}); 
              * 其它格式： 
              * wdFormatHTML 
              * wdFormatDocument 
              * wdFormatDOSText 
              * wdFormatDOSTextLineBreaks 
              * wdFormatEncodedText 
              * wdFormatRTF 
              * wdFormatTemplate 
              * wdFormatText 
              * wdFormatTextLineBreaks 
              * wdFormatUnicodeText 
            */
            documentType.InvokeMember("SaveAs", System.Reflection.BindingFlags.InvokeMethod,
                null, document, new object[] { htmlPath, Microsoft.Office.Interop.Word.WdSaveFormat.wdFormatFilteredHTML });

            //关闭文档  
            documentType.InvokeMember("Close", System.Reflection.BindingFlags.InvokeMethod,
            null, document, new object[] { null, null, null });

            // 退出 Word  
            wordType.InvokeMember("Quit", System.Reflection.BindingFlags.InvokeMethod, null, applicationClass, null);

            //转化HTML页面统一编码格式
            TransformHTMLEncoding(htmlPath);
        }

        /// <summary>
        /// Excel 转换成 HTML 文件
        /// </summary>
        /// <param name="excelPath">Excel 文档路径</param>
        /// <param name="htmlPath">Html 文件路径</param>
        /// <param name="sheetIndex">表单索引，如果大于 0，则按指定的 Sheet 生成 HTML</param>
        public static void ExcelToHtmlFile(string excelPath, string htmlPath, int sheetIndex = 0)
        {
            if (string.IsNullOrEmpty(excelPath)) throw new Exception(ExcelPathNullException);

            //实例化Excel  
            Microsoft.Office.Interop.Excel.Application application = new Microsoft.Office.Interop.Excel.Application();

            //打开文件，n.FullPath是文件路径  
            Microsoft.Office.Interop.Excel.Workbook workbook = application.Application.Workbooks.Open(excelPath, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);

            string directoryPath = Path.GetDirectoryName(htmlPath);
            string fileDirectoryPath = Path.Combine(directoryPath, Path.GetFileNameWithoutExtension(htmlPath));

            Microsoft.Office.Interop.Excel.Workbook newWorkbook = null;
            if (sheetIndex > 0)
            {
                Microsoft.Office.Interop.Excel.Worksheet worksheet = (Microsoft.Office.Interop.Excel.Worksheet)workbook.Worksheets[sheetIndex];
                newWorkbook = application.Application.Workbooks.Add(1);
                worksheet.Copy(newWorkbook.Sheets[1]);
                ((Microsoft.Office.Interop.Excel.Worksheet)newWorkbook.Worksheets[2]).Delete();
                //进行另存为操作    
                newWorkbook.SaveAs(htmlPath, Microsoft.Office.Interop.Excel.XlFileFormat.xlHtml, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Microsoft.Office.Interop.Excel.XlSaveAsAccessMode.xlNoChange, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
            }
            else
            {
                //进行另存为操作    
                workbook.SaveAs(htmlPath, Microsoft.Office.Interop.Excel.XlFileFormat.xlHtml, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Microsoft.Office.Interop.Excel.XlSaveAsAccessMode.xlNoChange, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
            }
            //逐步关闭所有使用的对象  
            workbook.Close(false, Type.Missing, Type.Missing);
            if (newWorkbook != null)
            {
                newWorkbook.Close(false, Type.Missing, Type.Missing);
            }
            application.Quit();

            System.Runtime.InteropServices.Marshal.ReleaseComObject(workbook);
            if (newWorkbook != null)
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(newWorkbook);
            }
            System.Runtime.InteropServices.Marshal.ReleaseComObject(application.Application.Workbooks);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(application);

            if (sheetIndex > 0)
            {
                TransformHTMLEncoding(htmlPath, string.Format("<frame src=\"{0}.files/tabstrip.html\" name=\"frTabs\" marginwidth=0 marginheight=0>", Path.GetFileNameWithoutExtension(htmlPath)));
            }
            System.Diagnostics.Process[] processList = System.Diagnostics.Process.GetProcessesByName("EXCEL");
            foreach (System.Diagnostics.Process process in processList)
            {
                process.Kill();
            }
        }
        #endregion

        #region 逻辑处理私有方法
        private static void TransformHTMLEncoding(string htmlPath, params string[] replaceTextList)
        {
            string html = "";
            using(System.IO.StreamReader streamReader = new System.IO.StreamReader(htmlPath, Encoding.GetEncoding(0)))
            {
                html = streamReader.ReadToEnd();
            }
            if (!string.IsNullOrEmpty(html))
            {
                html = System.Text.RegularExpressions.Regex.Replace(html, @"<meta[^>]*>", "<meta http-equiv=Content-Type content='text/html; charset=gb2312'>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (replaceTextList != null && replaceTextList.Length > 0)
                {
                    foreach(string replaceText in replaceTextList)
                    {
                        html = html.Replace(replaceText, "");
                    }
                }
                using(System.IO.StreamWriter streamWriter = new System.IO.StreamWriter(htmlPath, false, Encoding.Default))
                {
                    streamWriter.Write(html);
                }
            }
        }
        #endregion
    }
}
