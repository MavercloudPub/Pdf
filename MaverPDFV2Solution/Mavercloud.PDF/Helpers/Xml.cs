using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Mavercloud.PDF.Helpers
{
    public static class Xml
    {
        #region 对象到 XML String


        /// <summary>
        /// 将Object对象实体序列化成字符串
        /// </summary>
        /// <param name="o">Object实体</param>
        /// <param name="encoding">字符编码，默认系统的当前 ANSI 代码页的编码</param>
        /// <returns>字符串</returns>
        static public string ObjectToXmlStr(object o, Encoding encoding)
        {
            return ObjectToXmlStr(o, encoding, null, null);
        }

        static public string ObjectToXmlStr(object o, Encoding encoding, XmlSerializerNamespaces ns, XmlAttributeOverrides overrides)
        {
            if (encoding == null) encoding = Encoding.Default;
            string str = "";
            XmlSerializer ser = null;
            if (overrides == null)
            {
                ser = new XmlSerializer(o.GetType());
            }
            else
            {
                ser = new XmlSerializer(o.GetType(), overrides);
            }
            System.IO.MemoryStream mem = new MemoryStream();
            TextWriter writer = new StreamWriter(mem, encoding);
            //     XmlTextWriter writer = new XmlTextWriter(mem, encoding);    
            if (ns != null)
            {
                ser.Serialize(writer, o, ns);
            }
            else
            {
                ser.Serialize(writer, o);
            }
            str = encoding.GetString(mem.ToArray());
            return str;
        }

        /// <summary>
        /// 将Objects数组中每一个对象实体序列化成字符串
        /// </summary>
        /// <typeparam name="T">泛型，实体类型</typeparam>
        /// <param name="o">Object实体</param>
        /// <param name="encoding">字符编码，默认系统的当前 ANSI 代码页的编码</param>
        /// <returns></returns>
        static public string ObjectsToXmlStr<T>(List<T> o, Encoding encoding)
        {
            return ObjectsToXmlStr<T>(o, encoding, null);
        }

        static public string ObjectsToXmlStr<T>(List<T> o, Encoding encoding, XmlSerializerNamespaces ns)
        {
            if (encoding == null) encoding = Encoding.Default;
            string str = "";
            XmlSerializer ser = new XmlSerializer(o.GetType());
            System.IO.MemoryStream mem = new MemoryStream();
            TextWriter writer = new StreamWriter(mem, encoding);
            //       XmlTextWriter writer = new XmlTextWriter(mem, );
            if (ns != null)
            {
                ser.Serialize(writer, o, ns);
            }
            else
            {
                ser.Serialize(writer, o);
            }
            str = encoding.GetString(mem.ToArray());
            return str;
        }


        /// <summary>
        /// 将Table中每一行数据序列化成字符串
        /// </summary>
        /// <typeparam name="T">泛型，实体类型</typeparam>
        /// <param name="table">数据集Table</param>
        /// <param name="encoding">字符编码，默认系统的当前 ANSI 代码页的编码</param>
        /// <returns></returns>
        public static string DataTableToXmlStr<T>(DataTable table, Encoding encoding)
        {
            //List<T> o = DataBindHelper.BindDataTableToObjArray<T>(table);
            return null;// ObjectsToXmlStr<T>(o, encoding);
        }

        /// <summary>
        /// 将Table转换成Xml字符串，不带XSD结构
        /// </summary>
        /// <param name="table"></param>
        /// <param name="encoding">字符编码，默认系统的当前 ANSI 代码页的编码</param>
        /// <returns></returns>
        public static string DataTableToXmlStr(DataTable table, Encoding encoding)
        {
            if (encoding == null) encoding = Encoding.Default;
            string str = "";
            System.IO.MemoryStream mem = new MemoryStream();
            TextWriter writer = new StreamWriter(mem, encoding);
            table.DataSet.WriteXml(writer, XmlWriteMode.IgnoreSchema);
            str = string.Format("<?xml version=\"1.0\" encoding=\"{0}\"?>\r\n{1}", encoding.HeaderName, encoding.GetString(mem.ToArray()));
            return str;
        }

        #endregion

        #region 对象到 XML File

        /// <summary>
        /// Objects to XML file.
        /// </summary>
        /// <param name="o">Object实体</param>
        /// <param name="fileName">文件路径</param>
        /// <param name="encoding">字符编码，默认系统的当前 ANSI 代码页的编码</param>
        static public void ObjectToXmlFile(object o, string fileName, Encoding encoding)
        {
            if (encoding == null) encoding = Encoding.Default;
            XmlSerializer ser = new XmlSerializer(o.GetType());
            using (TextWriter writer = new StreamWriter(fileName, false, encoding))
            {
                ser.Serialize(writer, o);
            }
        }


        /// <summary>
        /// Objectses to XML file.
        /// </summary>
        /// <param name="o">Object实体集合</param>
        /// <param name="fileName">文件路径</param>
        /// <param name="encoding">字符编码，默认系统的当前 ANSI 代码页的编码</param>
        public static void ObjectsToXmlFile(IList o, string fileName, Encoding encoding)
        {
            if (encoding == null) encoding = Encoding.Default;
            Type type = o.GetType();
            XmlSerializer ser = new XmlSerializer(o.GetType());
            using (TextWriter writer = new StreamWriter(fileName, false, encoding))
            {
                ser.Serialize(writer, o);
            }
        }


        /// <summary>
        /// 将数据集Table中每行数据序列化至XML file.
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="table">数据集Table</param>
        /// <param name="fileName">文件路径</param>
        /// <param name="encoding">字符编码，默认系统的当前 ANSI 代码页的编码</param>
        public static void DataTableToXmlFile<T>(DataTable table, string fileName, Encoding encoding)
        {
            //List<T> o = DataBindHelper.BindDataTableToObjArray<T>(table);
            //ObjectsToXmlFile(o, fileName, encoding);
        }

        /// <summary>
        /// 将Table转换成Xml字符，并保存至文件，不带XSD结构
        /// </summary>
        /// <param name="table">数据集Table</param>
        /// <param name="fileName">文件路径</param>
        /// <param name="encoding">字符编码，默认系统的当前 ANSI 代码页的编码</param>
        public static void DataTableToXmlFile(DataTable table, string fileName, Encoding encoding)
        {
            if (encoding == null) encoding = Encoding.Default;
            string str = "";
            System.IO.MemoryStream mem = new MemoryStream();
            TextWriter writer = new StreamWriter(mem, encoding);
            table.DataSet.WriteXml(writer, XmlWriteMode.IgnoreSchema);
            str = string.Format("<?xml version=\"1.0\" encoding=\"{0}\"?>\r\n{1}", encoding.HeaderName, encoding.GetString(mem.ToArray()));
            StreamWriter fwriter = new StreamWriter(fileName, false, encoding);
            fwriter.Write(str);
            fwriter.Close();
        }

        #endregion

        #region XML String 到对象


        /// <summary>
        /// 将xml字符串文档反序列化成指定类型的对象.
        /// </summary>
        /// <param name="xmlStr">xml字符串文档</param>
        /// <param name="type">指定Object类型</param>
        /// <returns></returns>
        static public object XmlStrToObject(string xmlStr, Type type)
        {
            XmlSerializer ser = new XmlSerializer(type);
            TextReader reader = new StringReader(xmlStr);
            object obj = ser.Deserialize(reader);
            return obj;
        }

        /// <summary>
        /// 将xml字符串文档反序列化成指定类型的对象.
        /// </summary>
        /// <typeparam name="T">泛型，指定Object类型</typeparam>
        /// <param name="xmlStr">xml字符串文档</param>
        /// <returns>泛型，指定Object类型</returns>
        static public T XmlStrToObject<T>(string xmlStr)
        {
            return (T)XmlStrToObject(xmlStr, typeof(T));
        }

        /// <summary>
        /// 将xml字符串文档反序列化成指定类型的对象数组.
        /// </summary>
        /// <typeparam name="T">泛型，指定Object类型</typeparam>
        /// <param name="xmlStr">xml字符串文档</param>
        /// <returns>对象数组</returns>
        static public List<T> XmlStrToObjects<T>(string xmlStr)
        {
            List<T> o = new List<T>();
            XmlSerializer ser = new XmlSerializer(o.GetType());
            TextReader reader = new StringReader(xmlStr);
            o = (List<T>)ser.Deserialize(reader);
            return o;
        }


        /// <summary>
        /// 将xml字符串文档反序列化成DataTable.
        /// </summary>
        /// <typeparam name="T">泛型，指定Object类型</typeparam>
        /// <param name="xmlStr">xml字符串文档</param>
        /// <returns>DataTable</returns>
        static public DataTable XmlStrToDataTable<T>(string xmlStr)
        {
            List<T> o = XmlStrToObjects<T>(xmlStr);
            DataTable table = new DataTable();
            //DataBindHelper.AppendObjArrayToTable<T>(o,table);
            return table;
        }

        /// <summary>
        /// 使用指定的文件将 XML 架构和数据读入 System.Data.DataSet。
        /// </summary>
        /// <param name="xmlStr">架构和数据XML字符串</param>
        /// <returns>DataTable</returns>
        static public DataTable XmlStrToDataTable(string xmlStr)
        {
            DataSet dataSet = new DataSet();
            StringReader reader = new StringReader(xmlStr);
            dataSet.ReadXml(reader);
            return dataSet.Tables[0];
        }

        #endregion

        #region XML File 到对象


        /// <summary>
        /// XML文件反序列化至指定类型的Object.
        /// </summary>
        /// <param name="fileName">xml文件路径</param>
        /// <param name="type">指定类型</param>
        /// <returns>Object实体</returns>
        static public object XmlFileToObject(string fileName, Type type)
        {
            XmlSerializer ser = new XmlSerializer(type);
            object obj = null;
            using (FileStream reader = new FileStream(fileName, FileMode.Open))
            {
                obj = ser.Deserialize(reader);
            }
            return obj;
        }

        /// <summary>
        /// XML文件反序列化至指定类型的Object数组
        /// </summary>
        /// <typeparam name="T">泛型，指定类型</typeparam>
        /// <param name="fileName">xml文件路径</param>
        /// <returns>Object数组</returns>
        static public List<T> XmlFileToObjects<T>(string fileName)
        {
            List<T> o = new List<T>();
            XmlSerializer ser = new XmlSerializer(o.GetType());
            using (FileStream reader = new FileStream(fileName, FileMode.Open))
            {
                o = (List<T>)ser.Deserialize(reader);
            }
            return o;
        }

        static public List<T> XmlFileToObjects<T>(Stream fileStream)
        {
            List<T> o = new List<T>();
            XmlSerializer ser = new XmlSerializer(o.GetType());
            using (fileStream)
            {
                o = (List<T>)ser.Deserialize(fileStream);
            }
            return o;
        }

        /// <summary>
        /// XML文件反序列化至指定类型的Object数组
        /// </summary>
        /// <param name="fileName">xml文件路径</param>
        /// <param name="typeFullName">完整类型名称</param>
        /// <returns>Object数组</returns>
        static public object XmlFileToObjects(string fileName, string typeFullName)
        {
            string tlistT = string.Format("System.Collections.Generic.List`1[[{0}]]", typeFullName);
            Type tList = Type.GetType(tlistT);
            object olist = Activator.CreateInstance(tList);
            XmlSerializer ser = new XmlSerializer(olist.GetType());
            using (FileStream reader = new FileStream(fileName, FileMode.Open))
            {
                olist = ser.Deserialize(reader);
            }
            return olist;
        }

        /// <summary>
        /// 将指定路径xml字符文档反序列化成DataTable.
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="fileName">xml文件路径</param>
        /// <returns></returns>
        static public DataTable XmlFileToDataTable<T>(string fileName)
        {
            List<T> o = XmlFileToObjects<T>(fileName);
            DataTable table = new DataTable();
            //DataBindHelper.AppendObjArrayToTable<T>(o, table);
            return table;
        }
        /// <summary>
        /// 将指定路径将 XML 架构和数据读入 System.Data.DataSet。
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        static public DataTable XmlFileToDataTable(string fileName)
        {
            DataSet dataSet = new DataSet();
            dataSet.ReadXml(fileName);
            return dataSet.Tables[0];
        }

        #endregion

        #region 另类解析

        /// <summary>
        /// 从xml字符串中指定位置，查找指定字符
        /// </summary>
        /// <param name="str">xml字符串</param>
        /// <param name="findstr">查找的指定字符</param>
        /// <param name="startIndex">起始位置</param>
        /// <returns></returns>
        static public string XmlStrToSubStr(string str, string findstr, ref int startIndex)
        {
            string result = "";
            if (startIndex < 0) return result;
            string startStr = "<" + findstr + ">";
            string endStr = "</" + findstr + ">";
            int currStartIndex = str.IndexOf(startStr, startIndex);
            if (currStartIndex < 0) return result;
            int currEndIndex = str.IndexOf(endStr, currStartIndex);
            if (currEndIndex < 0) return result;
            result = str.Substring(currStartIndex + startStr.Length, currEndIndex - (currStartIndex + startStr.Length));
            return result;
        }
        #endregion

        public static string GetAttributeValue(string attrName, XmlNode xmlNode)
        {
            string attrValue = "";
            foreach (XmlAttribute attr in xmlNode.Attributes)
            {
                if (attr.Name == attrName)
                {
                    attrValue = attr.InnerText;
                }
            }
            return attrValue;
        }
    }
}
