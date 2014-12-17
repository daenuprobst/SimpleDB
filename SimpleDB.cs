using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace SimpleDB
{
    public static class SimpleDB
    {
        private static string _idPropertyName = "_Id";

        public static string Database { get; set; }
        public static string BaseUrl { get; set; }
        public static string IdPropertyName
        {
            get
            {
                return _idPropertyName;
            }
            set
            {
                _idPropertyName = value;
            }
        }

        public static void Store<T>(T item)
        {
            string id = GetId<T>(item);

            if (String.IsNullOrEmpty(id)) return;

            List<T> data = OpenDocument<T>();

            if (data.Where(i => GetId<T>(i) == id).Count() > 0) return;

            data.Add(item);
            SaveDocument<T>(data);
        }

        public static T Get<T>(string id)
        {
            List<T> data = OpenDocument<T>();
            return data.Where(i => GetId<T>(i) == id).SingleOrDefault();
        }

        public static void Update<T>(T item)
        {
            string id = GetId<T>(item);

            if (String.IsNullOrEmpty(id)) return;

            List<T> data = OpenDocument<T>();
            T oldItem = data.Where(i => GetId<T>(i) == id).SingleOrDefault();

            if (oldItem == null) return;

            int index = data.IndexOf(oldItem);
            data[index] = item;
            SaveDocument<T>(data);
        }

        public static void Delete<T>(T item)
        {
            string id = GetId<T>(item);

            if (String.IsNullOrEmpty(id)) return;

            List<T> data = OpenDocument<T>();
            T oldItem = data.Where(i => GetId<T>(i) == id).SingleOrDefault();

            if (oldItem == null) return;

            data.Remove(oldItem);
            SaveDocument<T>(data);
        }

        public static IQueryable<T> Query<T>()
        {
            return OpenDocument<T>().AsQueryable<T>();
        }

        private static List<T> OpenDocument<T>()
        {
            if (!DocumentExists<T>()) CreateDocument<T>();

            using (var streamReader = new StreamReader(BaseUrl + "\\" + Database + "\\" + GetName<T>() + ".json"))
            {
                return JsonConvert.DeserializeObject<List<T>>(streamReader.ReadToEnd());
            }
        }

        private static void SaveDocument<T>(List<T> data)
        {
            if (!DocumentExists<T>()) CreateDocument<T>();
            using (var fileStream = File.Create(BaseUrl + "\\" + Database + "\\" + GetName<T>() + ".json"))
            {
                using (var streamWriter = new StreamWriter(fileStream))
                {
                    streamWriter.Write(JsonConvert.SerializeObject(data));
                }
            }
        }

        private static bool DocumentExists<T>()
        {
            return File.Exists(BaseUrl + "\\" + Database + "\\" + GetName<T>() + ".json");
        }

        private static void CreateDocument<T>() 
        {
            List<T> emptyList = new List<T>();
            using (var fileStream = File.Create(BaseUrl + "\\" + Database + "\\" + GetName<T>() + ".json"))
            {
                using (var streamWriter = new StreamWriter(fileStream))
                {
                    streamWriter.Write(JsonConvert.SerializeObject(emptyList));
                }
            }
            
        }

        private static string GetName<T>()
        {
            return typeof(T).Name;
        }

        private static string GetId<T>(T item)
        {
            Type type = typeof(T);
            var propertyInfo = type.GetProperty(IdPropertyName);
            return propertyInfo.GetValue(item) as String;
        }
    }
}
