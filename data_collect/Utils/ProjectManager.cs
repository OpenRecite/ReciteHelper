using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReciteHelper.DataCollect.Model
{
    public static class ProjectManager
    {
        private static readonly string DataFilePath = "data.json";

        private static Project? _currentProject;
        private static readonly Lock _lock = new Lock();

        public static Project CurrentProject
        {
            get
            {
                if (_currentProject is null) Load();
                if (_currentProject is null) return new();

                return _currentProject;
            }
        }

        public static void Load()
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(DataFilePath))
                    {
                        string json = File.ReadAllText(DataFilePath, Encoding.UTF8);
                        _currentProject = JsonSerializer.Deserialize<Project>(json);
                    }
                    else
                    {
                        _currentProject = new Project();
                    }
                }
                catch (Exception)
                {
                    _currentProject = new Project();
                }

                if (_currentProject == null)
                {
                    _currentProject = new Project();
                }

                if (_currentProject.Questions == null)
                {
                    _currentProject.Questions = new List<Question>();
                }
            }
        }

        public static void Save()
        {
            lock (_lock)
            {
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    };

                    string json = JsonSerializer.Serialize(_currentProject, options);
                    File.WriteAllText(DataFilePath, json, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"保存数据失败: {ex.Message}", ex);
                }
            }
        }

        public static void Reset()
        {
            lock (_lock)
            {
                _currentProject = new Project();
            }
        }
    }
}