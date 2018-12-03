/*
 * 作用：利用 NPOI 读取/写入 Excel 文档，支持读取合并单元格以及带有公式的数据。
 * */
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Data;
using System.Text.RegularExpressions;
using Aspose.Cells;

namespace Helper.Core.Library
{
    public class TExcelHelper
    {
        public static readonly TExcelHelper Instance = new TExcelHelper();

        #region 私有属性常量
        private const string ExcelFormatErrorException = "Excel 文件格式不正确！";
        internal const string ExcelWorkbookNullException = "Workbook 为空！";
        #endregion

        #region 对外公开方法

        #region ToEntityList<T>
        /// <summary>
        /// 返回实体数据列表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="excelPath">Excel 路径</param>
        /// <param name="sheetName">Sheet 表单名称</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="headerIndex">表头起始索引，默认值：0，表示第一行是表头数据，与 dataIndex 相同时，表示 Excel 无表头</param>
        /// <param name="dataIndex">数据行起始索引，默认值：1，表示数据从第二行开始</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static List<T> ToEntityList<T>(string excelPath, string sheetName, object propertyMatchList = null, int headerIndex = 0, int dataIndex = 1, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            List<T> dataList = new List<T>();
            ExecuteIWorkbookRead(excelPath, (Workbook workbook) =>
            {
                Dictionary<string, string> propertyDict = CommonHelper.MergeDict(false, CommonHelper.KeyValueTransform(propertyMatchList), CommonHelper.KeyValueTransform(ReflectionGenericHelper.GetPropertyDict<T>()));
                dataList = SheetEntityList<T>(workbook, sheetName, propertyDict, headerIndex, dataIndex, reflectionType);
            });
            return dataList;
        }
        /// <summary>
        /// 返回字典数据
        /// </summary>
        /// <typeparam name="T">基本类型，例：int</typeparam>
        /// <typeparam name="K">基本类型，例：string</typeparam>
        /// <param name="excelPath">Excel 路径</param>
        /// <param name="sheetName">Sheet 表单名称</param>
        /// <param name="keyIndex">键列数据索引，默认值：0</param>
        /// <param name="valueIndex">值列数据索引，默认值：1</param>
        /// <param name="dataIndex">数据行起始索引，默认值：1，表示数据从第二行开始</param>
        /// <returns></returns>
        public static Dictionary<T, K> ToDict<T, K>(string excelPath, string sheetName, int keyIndex = 0, int valueIndex = 1, int dataIndex = 1)
        {
            Dictionary<T, K> resultDict = new Dictionary<T, K>();
            ExecuteIWorkbookRead(excelPath, (Workbook workbook) =>
            {
                Worksheet sheet = workbook.Worksheets[sheetName];
                Cells cellList = sheet.Cells;

                int rowCount = cellList.MaxDataRow + 1;
                int cellCount = cellList.MaxDataColumn + 1;

                for (int rowIndex = dataIndex; rowIndex < rowCount; rowIndex++)
                {
                    string keyData = GetSheetData(cellList, rowIndex, keyIndex);
                    string valueData = GetSheetData(cellList, rowIndex, valueIndex);
                    if(!string.IsNullOrEmpty(keyData) && !string.IsNullOrEmpty(valueData))
                    {
                        resultDict.Add((T)Convert.ChangeType(keyData, typeof(T)), (K)Convert.ChangeType(valueData, typeof(K)));
                    }
                }
            });
            return resultDict;
        }
        /// <summary>
        /// 返回基本类型数据列表
        /// </summary>
        /// <typeparam name="T">基本类型，例：int</typeparam>
        /// <param name="excelPath">Excel 路径</param>
        /// <param name="sheetName">Sheet 表单名称</param>
        /// <param name="fieldIndex">字段列索引，默认值：0，表示取第一列数据</param>
        /// <param name="dataIndex">数据行起始索引，默认值：1，表示数据从第二行开始</param>
        /// <returns></returns>
        public static List<T> ToList<T>(string excelPath, string sheetName, int fieldIndex = 0, int dataIndex = 1)
        {
            List<T> dataList = new List<T>();
            ExecuteIWorkbookRead(excelPath, (Workbook workbook) =>
            {
                Worksheet sheet = workbook.Worksheets[sheetName];
                Cells cellList = sheet.Cells;

                int rowCount = cellList.MaxDataRow + 1;
                int cellCount = cellList.MaxDataColumn + 1;
                for (int rowIndex = dataIndex; rowIndex < rowCount; rowIndex++)
                {
                    for (int colIndex = 0; colIndex < cellCount; colIndex++)
                    {
                        if (colIndex == fieldIndex)
                        {
                            string cellData = cellList[rowIndex, colIndex].StringValue;
                            if (!string.IsNullOrEmpty(cellData))
                            {
                                dataList.Add((T)Convert.ChangeType(cellData, typeof(T)));
                            }
                        }
                    }
                }
            });
            return dataList;
        }
        #endregion

        #region ToTxt<T>
        /// <summary>
        /// 根据实体数据列表创建 Excel
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="dataList">实体数据列表</param>
        /// <param name="excelPath">Excel 路径</param>
        /// <param name="sheetName">Sheet 表单名称</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="propertyList">属性列表，如果指定，则按指定属性列表生成 Excel</param>
        /// <param name="propertyContain">是否包含，true 属性包含，flase 属性排除</param>
        /// <param name="columnValueFormat">列格式化，例：yyyy年MM月dd日</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static void ToExcel<T>(List<T> dataList, string excelPath, string sheetName, object propertyMatchList = null, string[] propertyList = null, bool propertyContain = true, object columnValueFormat = null, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            ExecuteIWorkbookWrite(excelPath, (Workbook workbook) =>
            {
                List<string> filterNameList = null;
                if (propertyList != null) filterNameList = propertyList.ToList<string>();

                Dictionary<string, object> propertyDict = CommonHelper.GetParameterDict(propertyMatchList);
                Dictionary<string, object> valueFormatDict = CommonHelper.GetParameterDict(columnValueFormat);
                ToSheet(workbook, dataList, sheetName, filterNameList, propertyContain, propertyDict, valueFormatDict, reflectionType);
            });
        }
        #endregion

        #endregion

        #region 逻辑处理私有方法
        internal static void ExecuteIWorkbookRead(string excelPath, Action<Workbook> callback)
        {
            string suffix = FileHelper.GetSuffix(excelPath);
            if (ExcelFormat.FormatList.IndexOf(suffix) < 0) throw new Exception(ExcelFormatErrorException);

            Workbook workbook = new Workbook(excelPath);
            if (callback != null) callback(workbook);
        }
        internal static void ExecuteIWorkbookWrite(string excelPath, Action<Workbook> callback)
        {
            //获得 Excel 后缀
            string suffix = FileHelper.GetSuffix(excelPath);
            if (ExcelFormat.FormatList.IndexOf(suffix) < 0) throw new Exception(ExcelFormatErrorException);

            // 创建对应目录
            bool createDirectoryStatus = FileHelper.CreateDirectory(excelPath);
            // 如果创建目录失败，则终止处理
            if (!createDirectoryStatus) return;

            // 如果存在 Excel 文件，先删除文件
            if (File.Exists(excelPath)) File.Delete(excelPath);

            Workbook workbook = new Workbook();
            if (callback != null) callback(workbook);

            workbook.Save(excelPath, SaveFormat.Xlsx);
        }
        internal static List<T> SheetEntityList<T>(Workbook workbook, string sheetName, Dictionary<string, string> propertyDict = null, int headerIndex = 0, int dataIndex = 1, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            List<T> dataList = new List<T>();

            Worksheet sheet = workbook.Worksheets[sheetName];
            Cells cellList = sheet.Cells;

            Dictionary<int, TExcelToEntityColumnMapper> excelToEntityMapperList = InitExcelToEntityMapper<T>(headerIndex, cellList, propertyDict);

            dynamic propertySetDict = null;
            if (reflectionType != ReflectionTypeEnum.Original) propertySetDict = ReflectionExtendHelper.PropertySetCallDict<T>(reflectionType);

            int rowCount = cellList.MaxDataRow + 1;
            int cellCount = cellList.MaxColumn + 1;

            for (int index = dataIndex; index < rowCount; index++)
            {
                T t = new T();
                for (int cellIndex = 0; cellIndex < cellCount; cellIndex++)
                {
                    if (excelToEntityMapperList.ContainsKey(cellIndex))
                    {
                        TExcelToEntityColumnMapper columnMapper = excelToEntityMapperList[cellIndex];

                        Cell cell = cellList[index, cellIndex];
                        if (cell != null && cell.Value != null)
                        {
                            if (propertySetDict != null && propertySetDict.ContainsKey(columnMapper.ColumnPropertyName))
                            {
                                ReflectionGenericHelper.SetPropertyValue(propertySetDict[columnMapper.ColumnPropertyName], t, cell.StringValue, columnMapper.ColumnPropertyInfo);
                            }
                            else
                            {
                                ReflectionHelper.SetPropertyValue(t, cell.StringValue, columnMapper.ColumnPropertyInfo);
                            }
                        }
                    }
                }
                dataList.Add(t);
            }

            return dataList;
        }
        internal static string GetSheetData(Cells cellList, int rowIndex, int colIndex)
        {
            for (int index = 0; index < cellList.Count + 1; index++)
            {
                if (index == colIndex)
                {
                    return cellList[rowIndex, index].StringValue;
                }
            }
            return null;
        }
        internal static void ToSheet<T>(Workbook workbook, List<T> dataList, string sheetName, List<string> filterNameList = null, bool propertyContain = true, Dictionary<string, object> propertyDict = null, Dictionary<string, object> valueFormatDict = null, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            Worksheet iSheet = workbook.Worksheets[0];
            iSheet.Name = sheetName;
            // 获得表头数据
            Dictionary<string, PropertyInfo> headerColumnNameDict = CommonHelper.InitPropertyWriteMapperFormat<T, ExcelTAttribute>(propertyDict, filterNameList, propertyContain);
            Cells cellList = iSheet.Cells;

            int columnIndex = 0;
            // 遍历设置表头
            foreach (KeyValuePair<string, PropertyInfo> keyValuePair in headerColumnNameDict)
            {
                cellList[0, columnIndex].PutValue(keyValuePair.Key);
                columnIndex++;
            }

            dynamic propertyGetDict = null;
            if (reflectionType != ReflectionTypeEnum.Original) propertyGetDict = ReflectionExtendHelper.PropertyGetCallDict<T>(reflectionType);

            if (dataList != null)
            {
                // 遍历设置数据
                for (int rowIndex = 1; rowIndex <= dataList.Count; rowIndex++)
                {
                    SetRowDataValue(cellList, rowIndex, dataList[rowIndex - 1], propertyGetDict, headerColumnNameDict, valueFormatDict);
                }
            }
        }
        private static void SetRowDataValue<T>(Cells cellList, int rowIndex, T t, dynamic propertyGetDict, Dictionary<string, PropertyInfo> headerColumnNameDict, Dictionary<string, object> valueFormatDict) where T : class
        {
            Type type = typeof(T);

            int columnIndex = 0;

            object propertyValue = null;
            foreach (KeyValuePair<string, PropertyInfo> keyValuePair in headerColumnNameDict)
            {
                if (propertyGetDict != null && propertyGetDict.ContainsKey(keyValuePair.Value.Name))
                {
                    propertyValue = propertyGetDict[keyValuePair.Value.Name](t);
                }
                else
                {
                    propertyValue = ReflectionHelper.GetPropertyValue(t, keyValuePair.Value);
                }
                if (propertyValue != null)
                {
                    if ((keyValuePair.Value.PropertyType == typeof(DateTime) || keyValuePair.Value.PropertyType == typeof(Nullable<DateTime>)) && valueFormatDict != null && valueFormatDict.ContainsKey(keyValuePair.Value.Name))
                    {
                        propertyValue = ((DateTime)propertyValue).ToString(valueFormatDict[keyValuePair.Value.Name].ToString());
                    }

                    cellList[rowIndex, columnIndex].PutValue(propertyValue);
                }
                columnIndex++;
            }
        }
        #region ToEntity 相关
        private static Dictionary<int, TExcelToEntityColumnMapper> InitExcelToEntityMapper<T>(int headerIndex, Cells cellList, Dictionary<string, string> propertyDict = null) where T : class
        {
            Dictionary<int, TExcelToEntityColumnMapper> resultList = new Dictionary<int, TExcelToEntityColumnMapper>();

            Type type = typeof(T);

            Dictionary<int, string> columnNameDict = InitExcelPrimaryMapperByName(headerIndex, cellList);
            foreach (KeyValuePair<int, string> keyValueItem in columnNameDict)
            {
                if (propertyDict.ContainsKey(keyValueItem.Value))
                {
                    resultList.Add(keyValueItem.Key, new TExcelToEntityColumnMapper() { ColumnName = keyValueItem.Value, ColumnPropertyName = propertyDict[keyValueItem.Value], ColumnPropertyInfo = type.GetProperty(propertyDict[keyValueItem.Value]) });
                }
            }

            return resultList;
        }
        private static Dictionary<int, string> InitExcelPrimaryMapperByName(int headerIndex, Cells cellList)
        {
            Dictionary<int, string> columnNameDict = new Dictionary<int, string>();
            // 获得列数量
            int cellCount = cellList.MaxDataColumn + 1;
            // 获得所有列名
            for (int index = 0; index < cellCount; index++)
            {
                string cellValue = cellList[headerIndex, index].StringValue.ToString();
                columnNameDict[index] = cellValue;
            }
            return columnNameDict;
        }
        #endregion

        #endregion
    }

    #region 逻辑处理辅助类
    internal class TExcelToEntityColumnMapper
    {
        public string ColumnName { get; set; }
        public string ColumnPropertyName { get; set; }
        public PropertyInfo ColumnPropertyInfo { get; set; }
    }
    #endregion
}
