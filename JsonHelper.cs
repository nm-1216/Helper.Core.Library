/*
 * 作用：Newtonsoft.Json 实现 Json 数据与实体数据相互转换。
 * 联系：QQ 100101392
 * 来源：https://github.com/snipen/Helper.Core.Library
 * */
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helper.Core.Library
{
    public class JsonHelper
    {
        #region 对外公开方法
        /// <summary>
        /// 实体数据/列表转 Json
        /// </summary>
        /// <param name="data">实体数据/列表</param>
        /// <returns></returns>
        public static string ToJson(object data)
        {
            return JsonConvert.SerializeObject(data);
        }

        /// <summary>
        /// Json 数据转实体数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="json">Json 数据</param>
        /// <returns></returns>
        public static T ToEntity<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        /// <summary>
        /// Json 数据转实体数据列表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="json">Json 数据</param>
        /// <returns></returns>
        public static List<T> ToEntityList<T>(string json)
        {
            return JsonConvert.DeserializeObject<List<T>>(json);
        }
        #endregion
    }
}
