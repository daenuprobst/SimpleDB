using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Threading;
using System.Diagnostics;

namespace SimpleDB
{

    public static class SimpleDB
    {
        private static ConcurrentQueue<Task> _queue = new ConcurrentQueue<Task>();
        private static string _idPropertyName = "_Id";
        private static List<string> _openDocuments = new List<string>();
        private static bool _initialized = false;

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

        public static bool Initialized
        {
            get { return _initialized; }
        }

        public static void Initialize()
        {
            _initialized = true;

            // Process queue
            
            /*
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                while (true)
                {
                    while (!_queue.IsEmpty)
                    {
                        Task task;
                        _queue.TryDequeue(out task);

                        if (task != null)
                        {
                            task.RunSynchronously();
                        }
                    }
                }
            }).Start();*/
        }

        public static void Store<T>(T item)
        {
            string id = GetId<T>(item);
            if (String.IsNullOrEmpty(id)) return;

            _queue.Enqueue(new Task(() =>
            {
                List<T> data = OpenDocument<T>();
                if (data.Where(i => GetId<T>(i) == id).Count() > 0) return;
                data.Add(item);
                SaveDocument<T>(data);
            }));

            RunQueue();
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

            _queue.Enqueue(new Task(() =>
            {
                List<T> data = OpenDocument<T>();
                T oldItem = data.Where(i => GetId<T>(i) == id).SingleOrDefault();

                if (oldItem == null) return;

                int index = data.IndexOf(oldItem);
                data[index] = item;
                SaveDocument<T>(data);
            }));

            RunQueue();
        }

        public static void Delete<T>(T item)
        {
            string id = GetId<T>(item);

            if (String.IsNullOrEmpty(id)) return;

            _queue.Enqueue(new Task(() =>
            {
                List<T> data = OpenDocument<T>();
                T oldItem = data.Where(i => GetId<T>(i) == id).SingleOrDefault();

                if (oldItem == null) return;

                data.Remove(oldItem);
                SaveDocument<T>(data);
            }));

            RunQueue();
        }

        public static IQueryable<T> Query<T>()
        {
            return OpenDocument<T>().AsQueryable<T>();
        }

        private static List<T> OpenDocument<T>()
        {
            string name = GetName<T>();

            if (!DocumentExists<T>()) CreateDocument<T>();

            while (true)
            {
                try
                {
                    using (var streamReader = new StreamReader(BaseUrl + "\\" + Database + "\\" + name + ".json"))
                    {
                        return JsonConvert.DeserializeObject<List<T>>(streamReader.ReadToEnd());
                    }
                }
                catch (Exception e)
                {
                    // File is locked ...
                    Debug.WriteLine("File locked ...");
                }
            }
        }

        private static void SaveDocument<T>(List<T> data)
        {
            string name = GetName<T>();

            if (!DocumentExists<T>()) CreateDocument<T>();

            while (true)
            {
                try
                {
                    using (var fileStream = File.Create(BaseUrl + "\\" + Database + "\\" + name + ".json"))
                    {
                        using (var streamWriter = new StreamWriter(fileStream))
                        {
                            streamWriter.Write(JsonConvert.SerializeObject(data));
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    // File is locked ...
                    Debug.WriteLine("File locked ...");
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

        private static void RunQueue()
        {
            while (!_queue.IsEmpty)
            {
                Task task;
                _queue.TryDequeue(out task);

                if (task != null)
                {
                    task.RunSynchronously();
                }
            }
        }
    }
}
