using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Mavercloud.PDF.Helpers
{
    public static class Object
    {
        #region Fields
        /// <summary>
        /// 
        /// </summary>
        private static Dictionary<Type, List<PropertyInfo>> ObjectPropertiesCache = new Dictionary<Type, List<PropertyInfo>>();

        /// <summary>
        /// 
        /// </summary>
        private static Dictionary<MemberInfo, Dictionary<Type, object[]>> PropertyAttributesCache
            = new Dictionary<MemberInfo, Dictionary<Type, object[]>>();
        #endregion Fields

        #region Constructors
        #endregion Constructors

        #region Properties
        #endregion Properties

        #region Events
        #endregion Events

        #region Public Methods


        /// <summary>
        /// Copies the obj's properties.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="srcObj">The SRC obj.</param>
        /// <returns></returns>
        public static T CopyObjToObj<T>(object srcObj)
        {
            T tarObj = Activator.CreateInstance<T>();
            CopyObjToObj(srcObj, tarObj);
            return tarObj;
        }

        /// <summary>
        /// Copies the obj's properties.
        /// </summary>
        /// <param name="srcObj">The source obj.</param>
        /// <param name="tarType">Type of the target object.</param>
        /// <returns></returns>
        public static object CopyObjToObj(object srcObj, Type tarType)
        {
            object tarObj = Activator.CreateInstance(tarType);
            CopyObjToObj(srcObj, tarObj);
            return tarObj;
        }

        /// <summary>
        /// Copies the obj's properties.
        /// </summary>
        /// <typeparam name="T">Type of target object</typeparam>
        /// <param name="srcObjs">The source objs.</param>
        /// <returns></returns>
        public static List<T> CopyObjsToObjs<T>(IEnumerable srcObjs)
        {
            List<T> tarObjs = new List<T>();
            if (srcObjs != null)
            {
                foreach (object srcObj in srcObjs)
                {
                    tarObjs.Add(CopyObjToObj<T>(srcObj));
                }
            }
            return tarObjs;
        }

        /// <summary>
        /// Copies the objs to objs.
        /// </summary>
        /// <param name="srcObjs">The SRC objs.</param>
        /// <param name="genericType">Type of the generic.</param>
        /// <returns></returns>
        public static ICollection CopyObjsToObjs(IEnumerable srcObjs, Type genericType)
        {
            IList tarList = CreateGenericListInstance(new Type[] { genericType });
            if (srcObjs != null)
            {
                foreach (object srcObj in srcObjs)
                {
                    object tarObj = Activator.CreateInstance(genericType);
                    CopyObjToObj(srcObj, tarObj);
                    tarList.Add(tarObj);
                }
            }
            return tarList;
        }

        /// <summary>
        /// Copies the obj's properties.
        /// </summary>
        /// <param name="srcObj">The source obj.</param>
        /// <param name="tarObj">The target obj.</param>
        public static void CopyObjToObj(object srcObj, object tarObj)
        {
            if (srcObj == null) return;
            if (tarObj == null)
            {
                tarObj = Activator.CreateInstance(tarObj.GetType());
            }
            Type srcType = srcObj.GetType();
            Type tarType = tarObj.GetType();
            List<PropertyInfo> srcPropertyInfos = GetProperties(srcType);
            List<PropertyInfo> tarPropertyInfos = GetProperties(tarType);
            foreach (PropertyInfo srcPropertyInfo in srcPropertyInfos)
            {
                if (srcPropertyInfo.PropertyType.IsValueType
                    || srcPropertyInfo.PropertyType == typeof(string))
                {
                    string propertyName = srcPropertyInfo.Name;
                    object propertyValue = srcPropertyInfo.GetValue(srcObj, null);
                    foreach (PropertyInfo tarPropertyInfo in tarPropertyInfos)
                    {
                        bool isNameEqual = tarPropertyInfo.Name == propertyName;
                        if (isNameEqual)
                        {
                            SetPropertyValue(tarObj, tarPropertyInfo, propertyValue);
                            break;
                        }
                    }
                }
            }
        }


        public static List<TResult> DataTableToList<TResult>(this DataTable dt) where TResult : class, new()
        {
            List<PropertyInfo> prlist = new List<PropertyInfo>();
            Type t = typeof(TResult);
            Array.ForEach<PropertyInfo>(t.GetProperties(), p => { if (dt.Columns.IndexOf(p.Name) != -1) prlist.Add(p); });
            List<TResult> oblist = new List<TResult>();

            foreach (DataRow row in dt.Rows)
            {
                TResult ob = new TResult();
                prlist.ForEach(p => { if (row[p.Name] != DBNull.Value) p.SetValue(ob, row[p.Name], null); });
                oblist.Add(ob);
            }
            return oblist;
        }

        public static DataTable ListToDataTable<TResult>(this IEnumerable<TResult> value) where TResult : class
        {
            List<PropertyInfo> pList = new List<PropertyInfo>();
            Type type = typeof(TResult);
            DataTable dt = new DataTable();
            Array.ForEach<PropertyInfo>(type.GetProperties(), p => { pList.Add(p); dt.Columns.Add(p.Name); });
            foreach (var item in value)
            {
                DataRow row = dt.NewRow();
                pList.ForEach(p => row[p.Name] = p.GetValue(item, null));
                dt.Rows.Add(row);
            }
            return dt;
        }

        /// <summary>
        /// Gets the attribute.
        /// </summary>
        /// <param name="memberInfo">The member info.</param>
        /// <param name="attrType">Type of the attr.</param>
        /// <returns></returns>
        static public object GetAttribute(MemberInfo memberInfo, Type attrType)
        {
            object[] attrs = GetAttributes(memberInfo, attrType);
            if (attrs != null && attrs.Length > 0)
            {
                return attrs[0];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 获取某个特性.
        /// </summary>
        /// <param name="objType">实体类型Type of the obj.</param>
        /// <param name="attrType">特性类型Type of the attr.</param>
        /// <returns></returns>
        static public object GetAttribute(Type objType, Type attrType)
        {
            object[] attrs = objType.GetCustomAttributes(attrType, true);
            if (attrs != null && attrs.Length > 0)
            {
                return attrs[0];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets object's properties.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public static List<PropertyInfo> GetProperties(Type type)
        {
            List<PropertyInfo> properties = null;
            if (!TryGetProperties(type, out properties))
            {
                properties = type.GetProperties().ToList();
                CacheProperties(type, properties);

            }
            return properties;
        }

        static public PropertyInfo GetProperty(Type type, string propertyName)
        {
            return type.GetProperty(propertyName);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="member"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static object GetPropertyValue(PropertyInfo property, object obj)
        {
            return property.GetValue(obj);
        }

        public static object GetPropertyValue(string propertyName, object obj)
        {
            Type t = obj.GetType();
            PropertyInfo property = t.GetProperty(propertyName);
            if (property != null)
            {
                return GetPropertyValue(property, obj);
            }
            else
            {
                return null;
            }
        }

        public static object GetFollowingPropertyValue(string propertyFollowPath, object obj)
        {
            object value = obj;
            Type currentType = obj.GetType();

            foreach (string propertyName in propertyFollowPath.Split('.'))
            {
                PropertyInfo property = currentType.GetProperty(propertyName);
                if (property == null)
                {
                    value = null;
                    break;
                }
                else
                {
                    value = property.GetValue(value, null);
                    currentType = property.PropertyType;
                }
            }
            return value;
        }

        /// <summary>
        /// Sets the property value.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        public static void SetPropertyValue(object obj, PropertyInfo property, object value)
        {
            
            property.SetValue(obj, value, null);
            
        }

        public static void SetPropertyValue(object obj, string propertyName, object value)
        {
            PropertyInfo property = GetProperty(obj.GetType(), propertyName);
            if (property != null)
            {
                SetPropertyValue(obj, property, value);
            }
        }


        #endregion

        #region Internal Methods




        /// <summary>
        /// Creates the generic list instance.
        /// </summary>
        /// <param name="genericArguments">The generic arguments.</param>
        /// <returns></returns>
        private static IList CreateGenericListInstance(Type[] genericArguments)
        {
            Type list = typeof(List<>);
            Type generic = list.MakeGenericType(genericArguments);
            return Activator.CreateInstance(generic) as IList;
        }





        /// <summary>
        /// Gets member's attributes.
        /// </summary>
        /// <param name="memberInfo">The member info.</param>
        /// <param name="attrType">Type of the attr.</param>
        /// <returns></returns>
        private static object[] GetAttributes(MemberInfo memberInfo, Type attrType)
        {
            object[] attrs = null;
            if (!TryGetAttributes(memberInfo, attrType, out attrs))
            {
                attrs = memberInfo.GetCustomAttributes(attrType, true);
                CacheAttributes(memberInfo, attrType, attrs);
            }
            return attrs;
        }



        /// <summary>
        /// Tries to get members.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="properties">The properties.</param>
        /// <returns></returns>
        static private bool TryGetProperties(Type type, out List<PropertyInfo> properties)
        {
            bool isExist = false;
            properties = null;
            lock (ObjectPropertiesCache)
            {
                isExist = ObjectPropertiesCache.TryGetValue(type, out properties);
            }
            return isExist;
        }

        /// <summary>
        /// Caches the members.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="properties">The properties.</param>
        static private void CacheProperties(Type type, List<PropertyInfo> properties)
        {
            lock (ObjectPropertiesCache)
            {
                if (!ObjectPropertiesCache.ContainsKey(type))
                {
                    ObjectPropertiesCache.Add(type, properties);
                }
            }
        }

        /// <summary>
        /// Tries the get attributes.
        /// </summary>
        /// <param name="member">The member.</param>
        /// <param name="attyType">Type of the atty.</param>
        /// <param name="attrs">The attrs.</param>
        /// <returns></returns>
        static private bool TryGetAttributes(MemberInfo member, Type attyType, out object[] attrs)
        {
            bool isExist = false;
            attrs = null;
            lock (PropertyAttributesCache)
            {
                if (PropertyAttributesCache.ContainsKey(member))
                {
                    isExist = PropertyAttributesCache[member].TryGetValue(attyType, out attrs);
                }
            }
            return isExist;
        }
        /// <summary>
        /// Cache the attributes
        /// </summary>
        /// <param name="member"></param>
        /// <param name="attrType"></param>
        /// <param name="attrs"></param>
        static private void CacheAttributes(MemberInfo member, Type attrType, object[] attrs)
        {
            lock (PropertyAttributesCache)
            {
                Dictionary<Type, object[]> attrList = null;
                if (PropertyAttributesCache.ContainsKey(member))
                {
                    attrList = PropertyAttributesCache[member];
                    if (!attrList.ContainsKey(attrType))
                    {
                        attrList.Add(attrType, attrs);
                    }
                }
                else
                {
                    attrList = new Dictionary<Type, object[]>();
                    PropertyAttributesCache.Add(member, attrList);
                    attrList.Add(attrType, attrs);
                }
            }
        }
        #endregion
    }
}
