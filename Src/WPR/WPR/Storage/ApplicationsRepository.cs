using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using WPR.Models;

namespace WPR.Storage
{
    internal class ApplicationsRepository
    {
        private readonly string _storageFilePath;

        public ApplicationsRepository(string baseFolder)
        {
            Directory.CreateDirectory(baseFolder);
            _storageFilePath = Path.Combine(baseFolder, "apps.json");
        }

        public IList<WprApplication> Load()
        {
            if (!File.Exists(_storageFilePath))
            {
                return new List<WprApplication>();
            }

            try
            {
                var json = File.ReadAllText(_storageFilePath);
                var list = JsonConvert.DeserializeObject<List<WprApplication>>(json);
                return list ?? new List<WprApplication>();
            }
            catch
            {
                return new List<WprApplication>();
            }
        }

        public void Save(IEnumerable<WprApplication> apps)
        {
            var json = JsonConvert.SerializeObject(apps, Formatting.Indented);
            File.WriteAllText(_storageFilePath, json);
        }

        public void AddOrReplace(WprApplication app)
        {
            var list = Load().ToList();
            var existing = list.FirstOrDefault(a => a.ProductId == app.ProductId);
            if (existing != null)
            {
                list.Remove(existing);
            }

            list.Add(app);
            Save(list);
        }

        public void Remove(string productId)
        {
            var list = Load().ToList();
            list.RemoveAll(a => a.ProductId == productId);
            Save(list);
        }
    }
}
