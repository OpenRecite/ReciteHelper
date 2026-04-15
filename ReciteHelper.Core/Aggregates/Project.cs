using ReciteHelper.Core.Entities;
using ReciteHelper.SharedKernel;
using System.Text.Json.Serialization;

namespace ReciteHelper.Core.Aggregates
{
    [method: JsonConstructor]
    public class Project() : AggregateRoot
    {
        [JsonPropertyName("name")]
        public string? ProjectName { get; set; }

        [JsonPropertyName("path")]
        public string? StoragePath { get; set; }

        [JsonPropertyName("bankfile")]
        public string? QuestionBankPath { get; set; }

        [JsonPropertyName("chapter")]
        public List<Chapter>? Chapters { get; set; }

        [JsonPropertyName("last_accessed")]
        public DateTime LastAccessed { get; private set; }

        public List<Question> ExportQuestions()
        {
            List<Question> questions = [];

            foreach (Chapter chapter in Chapters!)
            {
                questions.AddRange(chapter.Questions!);
            }

            return questions;
        }

        public void UpdateLastAccessed()
        {
            LastAccessed = DateTime.Now;
        }
    }
}
