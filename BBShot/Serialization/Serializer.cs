using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Xml;

namespace Popgun.Serialization
{
    public static class Serializer<T> where T : class
    {
        private static Dictionary<Type, XmlSerializer> Serializers = new Dictionary<Type, XmlSerializer>();

        public static T Load(String path)
        {
            if (!File.Exists(path))
                return null;

            using (var fs = new FileStream(path, FileMode.Open))
            {
                using (XmlReader reader = XmlReader.Create(fs))
                {
                    foreach (Type t in types)
                    {
                        if (!Serializers.ContainsKey(t))
                            Serializers.Add(t, new XmlSerializer(t));

                        if (Serializers[t].CanDeserialize(reader))
                            return Serializers[t].Deserialize(reader) as T;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// store all derived types of T:
        /// is used in deserialization
        /// </summary>
        private static Type[] types = AppDomain.CurrentDomain.GetAssemblies()
                                            .SelectMany(s => s.GetTypes())
                                            .Where(t => typeof(T).IsAssignableFrom(t)
                                                && t.IsClass
                                                && !t.IsGenericType)
                                                .ToArray();
    }
}
