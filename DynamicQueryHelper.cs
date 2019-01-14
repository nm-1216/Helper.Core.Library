/*
 * 作用：根据 XML 配置生成查询语句，格式参考：DynamicQuery.xml
 * 联系：QQ 100101392
 * 来源：https://github.com/snipen/Helper.Core.Library
 * */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Helper.Core.Library
{
    #region 逻辑处理辅助枚举
    internal class DynamicQueryConditionType
    {
        public const string Equal = "=";
        public const string NotEqual = "!=";
        public const string GreaterThan = ">";
        public const string GreaterThanEqual = ">=";
        public const string LessThan = "<";
        public const string LessThanEqual = "<=";
        public const string Like = "LIKE";
        public const string ChatIndex = "CHARINDEX";
    }
    internal class DynamicQueryGroupType
    {
        public const string And = "and";
        public const string Or = "or";
    }
    #endregion

    public class DynamicQueryHelper
    {
        #region 私有属性常量
        private static Dictionary<string, DynamicQueryItem> QueryDict = new Dictionary<string, DynamicQueryItem>();
        #endregion

        #region 对外公开方法
        /// <summary>
        /// 初始化 XML 文件
        /// </summary>
        /// <param name="xmlPath"></param>
        public static void Init(string xmlPath)
        {
            QueryDict = new Dictionary<string, DynamicQueryItem>();

            List<DynamicQueryItem> dataList = XmlHelper.ToEntityList<DynamicQueryItem>(xmlPath, "//Item");
            if (dataList != null && dataList.Count > 0)
            {
                foreach (DynamicQueryItem item in dataList)
                {
                    QueryDict.Add(item.Name, item);
                }
            }
        }
        /// <summary>
        /// 获得 SQL 语句
        /// </summary>
        /// <param name="key">XML 中 NAME 名称</param>
        /// <param name="conditionData">条件数据</param>
        /// <param name="containsWhere">是否添加 where 关键字</param>
        /// <returns></returns>
        public static string GetSql(string key, object conditionData, bool containsWhere = true)
        {
            if (QueryDict == null || !QueryDict.ContainsKey(key)) return null;

            DynamicQueryItem queryItem = QueryDict[key];
            Dictionary<string, DynamicQueryPropertyItem> conditionDataDict = ExecutePropertyInfo(conditionData);

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(queryItem.Value);

            bool groupStatus = false;
            bool conditionStatus = false;
            bool whereStatus = queryItem.Value.ToLower().IndexOf(" where ") < 0;
            if (!whereStatus)
            {
                string tempText = queryItem.Value.ToLower().Trim();
                if (!(tempText.EndsWith(" and") || tempText.EndsWith(" or")))
                {
                    groupStatus = true;
                }
            }

            // 如果需要包含 where 并且不存在 where 关键字同时存在查询条件
            if (containsWhere && (whereStatus && ((queryItem.GroupList != null && queryItem.GroupList.Count > 0) || (queryItem.ConditionList != null && queryItem.ConditionList.Count > 0))))
            {
                stringBuilder.Append(" where ");
            }

            if ((queryItem.GroupList == null || queryItem.GroupList.Count == 0) && (queryItem.ConditionList != null && queryItem.ConditionList.Count > 0))
            {
                queryItem.GroupList = new List<DynamicQueryGroup>()
                {
                    new DynamicQueryGroup() { Type = DynamicQueryGroupType.And, ConditionList = queryItem.ConditionList }
                };
                queryItem.ConditionList = null;
            }

            if (queryItem.GroupList != null && queryItem.GroupList.Count > 0)
            {
                foreach (DynamicQueryGroup groupItem in queryItem.GroupList)
                {
                    if (groupStatus)
                    {
                        stringBuilder.Append(" ");
                        if (string.IsNullOrEmpty(groupItem.Type))
                        {
                            stringBuilder.Append(DynamicQueryGroupType.And);
                        }
                        else
                        {
                            stringBuilder.Append(groupItem.Type);
                        }
                        stringBuilder.Append(" ");
                    }
                    stringBuilder.Append("(");

                    conditionStatus = false;
                    if (queryItem.ConditionList != null && queryItem.ConditionList.Count > 0)
                    {
                        foreach (DynamicQueryCondition conditionItem in queryItem.ConditionList)
                        {
                            if (conditionDataDict.ContainsKey(conditionItem.Field))
                            {
                                DynamicQueryPropertyItem fieldPropertyItem = conditionDataDict[conditionItem.Field];
                                // 如果满足条件
                                bool status = ValidCondition(conditionItem.Condition, conditionItem.Value, fieldPropertyItem.Value);
                                if (status)
                                {
                                    if (conditionStatus)
                                    {
                                        stringBuilder.Append(" ");
                                        if (string.IsNullOrEmpty(conditionItem.Type))
                                        {
                                            stringBuilder.Append(DynamicQueryGroupType.And);
                                        }
                                        else
                                        {
                                            stringBuilder.Append(conditionItem.Type);
                                        }
                                        stringBuilder.Append(" ");
                                    }

                                    if (conditionItem.Symbol.ToUpper() == DynamicQueryConditionType.Like)
                                    {
                                        stringBuilder.Append(ContactFieldList(conditionItem.Field, conditionItem.FieldList, fieldPropertyItem.Value, " {0} like '%{1}%' "));
                                    }
                                    else if (conditionItem.Symbol.ToUpper() == DynamicQueryConditionType.ChatIndex)
                                    {
                                        stringBuilder.Append(ContactFieldList(conditionItem.Field, conditionItem.FieldList, fieldPropertyItem.Value, " CHARINDEX('{1}', {0})>0 "));
                                    }
                                    else
                                    {
                                        bool propertyStatus = !(fieldPropertyItem.Type.PropertyType == typeof(int) || fieldPropertyItem.Type.PropertyType == typeof(float) || fieldPropertyItem.Type.PropertyType == typeof(double));
                                        
                                        string replaceText = "'{1}'";
                                        if (!propertyStatus)
                                        {
                                            replaceText = "{1}";
                                        }
                                        stringBuilder.Append(ContactFieldList(conditionItem.Field, conditionItem.FieldList, fieldPropertyItem.Value, string.Format(" {0}{2}{1} ", "{0}", replaceText, conditionItem.Symbol)));
                                    }
                                    conditionStatus = true;
                                }
                            }
                        }
                    }

                    stringBuilder.Append(")");
                    groupStatus = true;
                }
            }

            string commandText = stringBuilder.ToString().Trim();
            if (commandText.EndsWith("where ()"))
            {
                return StringHelper.TrimEnd(commandText, "where ()");
            }
            else
            {
                return commandText;
            }
        }
        #endregion

        #region 逻辑处理私有函数
        private static string ContactFieldList(string field, string fieldList, string value, string format)
        {
            StringBuilder stringBuilder = new StringBuilder();

            List<string> fieldDataList = null;
            if (string.IsNullOrEmpty(fieldList))
            {
                fieldDataList = new List<string>() { field };
            }
            else
            {
                fieldDataList = StringHelper.ToList<string>(fieldList, ",", true);
            }

            if (fieldDataList != null && fieldDataList.Count > 0)
            {
                stringBuilder.Append("(");
                for (int index = 0; index < fieldDataList.Count; index++)
                {
                    stringBuilder.Append(string.Format(format, fieldDataList[index], value));
                    if (index < fieldDataList.Count - 1)
                    {
                        stringBuilder.Append(" or ");
                    }
                }
                stringBuilder.Append(")");
            }

            return stringBuilder.ToString();
        }
        private static bool ValidCondition(string condition, string value, string data)
        {
            if (condition == DynamicQueryConditionType.Equal) return data == value;
            if (condition == DynamicQueryConditionType.NotEqual) return data != value;
            if (condition == DynamicQueryConditionType.GreaterThan) return double.Parse(data) > double.Parse(value);
            if (condition == DynamicQueryConditionType.GreaterThanEqual) return double.Parse(data) >= double.Parse(value);
            if (condition == DynamicQueryConditionType.LessThan) return double.Parse(data) < double.Parse(value);
            if (condition == DynamicQueryConditionType.LessThanEqual) return double.Parse(data) <= double.Parse(value);
            return false;
        }
        private static Dictionary<string, DynamicQueryPropertyItem> ExecutePropertyInfo(object data)
        {
            Dictionary<string, DynamicQueryPropertyItem> resultDict = new Dictionary<string, DynamicQueryPropertyItem>();
            ReflectionHelper.Foreach((PropertyInfo propertyInfo) =>
            {
                resultDict.Add(propertyInfo.Name, new DynamicQueryPropertyItem() { Type = propertyInfo, Value = ReflectionHelper.GetPropertyValue(data, propertyInfo).ToString() });
            }, data.GetType());
            return resultDict;
        }
        #endregion
    }

    #region 逻辑处理辅助类
    internal class DynamicQueryItem
    {
        public string Name { get; set; }
        public string Value { get; set; }

        [XmlT("Group", XmlTEnum.ElementList)]
        public List<DynamicQueryGroup> GroupList { get; set; }

        [XmlT("If", XmlTEnum.ElementList)]
        public List<DynamicQueryCondition> ConditionList { get; set; }
    }
    internal class DynamicQueryGroup
    {
        public string Name { get; set; }
        public string Type { get; set; }

        [XmlT("If", XmlTEnum.ElementList)]
        public List<DynamicQueryCondition> ConditionList { get; set; }
    }
    internal class DynamicQueryCondition
    {
        /// <summary>
        /// 字段
        /// </summary>
        public string Field { get; set; }
        /// <summary>
        /// 判断值
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// 条件
        /// </summary>
        public string Condition { get; set; }
        /// <summary>
        /// 逻辑类型 OR 或者 AND
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// 符号
        /// </summary>
        public string Symbol { get; set; }
        /// <summary>
        /// 字段列表
        /// </summary>
        public string FieldList { get; set; }

    }
    internal class DynamicQueryPropertyItem
    {
        public PropertyInfo Type { get; set; }
        public string Value { get; set; }
    }
    #endregion
}
