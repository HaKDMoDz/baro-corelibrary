﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using Baro.CoreLibrary.Collections;

namespace Baro.CoreLibrary.AAA
{
    public sealed class UserDataCollection
    {
        private List<UserData> m_data = new List<UserData>();
        private ReaderWriterLockSlim m_lock = new ReaderWriterLockSlim();

        private UserData[] InternalSearch(string queryStr)
        {
            if (queryStr == "*")
            {
                return m_data.ToArray();
            }
            else
            {
                // Liste boşsa çık git...
                if (m_data.Count == 0)
                    return null;

                List<UserData> resultList = new List<UserData>();

                bool incrementalSearch = false;

                // Sorgu * ile bitiyor. Demek ki incremental search yapılacak.
                if (queryStr.Length > 1 && queryStr[queryStr.Length - 1] == '*')
                {
                    incrementalSearch = true;
                    queryStr = queryStr.Substring(0, queryStr.Length - 1); // Remove last char
                }

                // Search
                int result = BinarySearchHelper.BinarySearch<UserData>(m_data, 0, m_data.Count, new UserData(queryStr, null), (x, y) =>
                {
                    if (incrementalSearch)
                    {
                        return string.Compare(x.Key, y.Key.Substring(0, Math.Min(queryStr.Length, y.Key.Length)));
                    }
                    else
                    {
                        return string.Compare(x.Key, y.Key);
                    }
                });

                // Not found
                if (result < 0)
                    return null;

                int ResultEksi = result - 1, ResultArtı = result;

                while (ResultEksi >= 0)
                {
                    int v = string.Compare(queryStr, m_data[ResultEksi].Key.Substring(0, Math.Min(queryStr.Length, m_data[ResultEksi].Key.Length)));

                    if (v != 0)
                    {
                        break;
                    }

                    resultList.Add(m_data[ResultEksi]);
                    ResultEksi--;
                }


                while (ResultArtı < m_data.Count)
                {
                    int v = string.Compare(queryStr, m_data[ResultArtı].Key.Substring(0, Math.Min(queryStr.Length, m_data[ResultArtı].Key.Length)));

                    if (v != 0)
                    {
                        break;
                    }

                    resultList.Add(m_data[ResultArtı]);
                    ResultArtı++;
                }

                return resultList.ToArray();
            }
        }

        private int Find(string key)
        {
            return Find(new UserData(key, null));
        }

        private int Find(UserData data)
        {
            return BinarySearchHelper.BinarySearch<UserData>(m_data, 0, m_data.Count, data, (x, y) =>
                {
                    return string.Compare(x.Key, y.Key);
                }
            );
        }

        public void Add(UserData data)
        {
            m_lock.EnterWriteLock();

            try
            {
                int r = Find(data);

                if (r < 0)
                {
                    m_data.Insert(~r, data);
                }
                else
                {
                    m_data[r] = data;
                }
            }
            finally
            {
                m_lock.ExitWriteLock();
            }
        }

        public void Add(UserData[] kvp)
        {
            m_lock.EnterWriteLock();

            try
            {
                foreach (var data in kvp)
                {
                    int r = Find(data);

                    if (r < 0)
                    {
                        m_data.Insert(~r, data);
                    }
                    else
                    {
                        m_data[r] = data;
                    }
                }
            }
            finally
            {
                m_lock.ExitWriteLock();
            }
        }

        public void Add(string key, string value)
        {
            Add(new UserData(key, value));
        }

        public void Remove(string[] key)
        {
            m_lock.EnterWriteLock();

            try
            {
                foreach (var item in key)
                {
                    int r = Find(item);

                    if (r >= 0)
                    {
                        m_data.RemoveAt(r);
                    }
                }
            }
            finally
            {
                m_lock.ExitWriteLock();
            }
        }

        public void Remove(string key)
        {
            m_lock.EnterWriteLock();

            try
            {
                int r = Find(key);

                if (r >= 0)
                {
                    m_data.RemoveAt(r);
                }
            }
            finally
            {
                m_lock.ExitWriteLock();
            }
        }

        public UserData[] ToArray()
        {
            m_lock.EnterReadLock();

            try
            {
                return m_data.ToArray();
            }
            finally
            {
                m_lock.ExitReadLock();
            }
        }

        public UserData[] Search(string queryStr)
        {
            m_lock.EnterReadLock();

            try
            {
                return InternalSearch(queryStr);
            }
            finally
            {
                m_lock.ExitReadLock();
            }
        }

        internal void ReadFromXml(XmlReader xml)
        {
            while (xml.Read())
            {
                if (xml.NodeType == XmlNodeType.EndElement && xml.Name == "data")
                {
                    break;
                }

                if (xml.NodeType == XmlNodeType.Element && xml.Name == "attr")
                {
                    this.Add(xml["key"], xml["value"]);
                }
            }
        }

        internal void WriteToXml(XmlWriter w)
        {
            m_lock.EnterReadLock();

            try
            {
                w.WriteStartElement("data");

                foreach (var item in m_data)
                {
                    w.WriteStartElement("attr");
                    w.WriteAttributeString("key", item.Key);
                    w.WriteAttributeString("value", item.Value);
                    w.WriteEndElement();
                }

                w.WriteEndElement();
            }
            finally
            {
                m_lock.ExitReadLock();
            }
        }
    }
}
