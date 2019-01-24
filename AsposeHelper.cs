/*
 * 作用：利用 Aspose 库把 Excel 导出生成 HTML 文档
 * 联系：QQ 100101392
 * 来源：https://github.com/snipen/Helper.Core.Library
 * 
 * 导出单个表单生成 HTML 文档：
 * AsposeHelper.ExcelToHtmlFile(@"C:\1.xlsx", @"C:\1.html", "Sheet1");
 * */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Helper.Core.Library
{
    public class AsposeHelper
    {
        #region 对外公开方法
        /// <summary>
        /// Excel 生成 HTML 文件
        /// </summary>
        /// <param name="excelPath">Excel 路径</param>
        /// <param name="htmlPath">Html 路径</param>
        /// <param name="sheetName">表单名称</param>
        public static void ExcelToHtmlFile(string excelPath, string htmlPath, string sheetName = "")
        {
            Aspose.Cells.HtmlSaveOptions htmlSaveOptions = new Aspose.Cells.HtmlSaveOptions(Aspose.Cells.SaveFormat.Html);
            Aspose.Cells.Workbook workBook = new Aspose.Cells.Workbook(excelPath);
            if (string.IsNullOrEmpty(sheetName))
            {
                workBook.Save(htmlPath, htmlSaveOptions);
            }
            else
            {
                Aspose.Cells.Workbook newWorkBook = new Aspose.Cells.Workbook();
                Aspose.Cells.Worksheet newWorkSheet = newWorkBook.Worksheets[0];
                newWorkSheet.Copy(workBook.Worksheets[sheetName]);
                newWorkBook.Save(htmlPath, htmlSaveOptions);
            }

            string directoryPath = string.Format("{0}/{1}_files", Path.GetDirectoryName(htmlPath), System.IO.Path.GetFileNameWithoutExtension(htmlPath));
            string[] filePathList = Directory.GetFiles(directoryPath, "*.htm");
            foreach(string filePath in filePathList)
            {
                TransformHTMLEncoding(filePath, string.Format("<script>\ndocument.write(\"<div style='color:red;font-size:10pt;font-family:Arial'>Evaluation Only. Created with Aspose.Cells for .NET.Copyright 2003 - 2018 Aspose Pty Ltd.</div>\");\n</script>"));
            }
            TransformHTMLEncoding(htmlPath, string.Format("<frame src=\"{0}_files/tabstrip.htm\" name=\"frTabs\" marginwidth=\"0\" marginheight=\"0\">", Path.GetFileNameWithoutExtension(htmlPath)));
        }
        #endregion

        #region 逻辑处理私有函数
        private static void TransformHTMLEncoding(string htmlPath, params string[] replaceTextList)
        {
            string html = "";
            using (System.IO.StreamReader streamReader = new System.IO.StreamReader(htmlPath, Encoding.GetEncoding(0)))
            {
                html = streamReader.ReadToEnd();
            }
            if (!string.IsNullOrEmpty(html))
            {
                html = System.Text.RegularExpressions.Regex.Replace(html, @"<meta[^>]*>", "<meta http-equiv=Content-Type content='text/html; charset=utf-8'>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (replaceTextList != null && replaceTextList.Length > 0)
                {
                    foreach (string replaceText in replaceTextList)
                    {
                        html = html.Replace(replaceText, "");
                    }
                }
                html = html.Replace("/sheet002.htm", "/sheet001.htm");
                using (System.IO.StreamWriter streamWriter = new System.IO.StreamWriter(htmlPath, false, Encoding.UTF8))
                {
                    streamWriter.Write(html);
                }
            }
        }
        #endregion
    }
}
