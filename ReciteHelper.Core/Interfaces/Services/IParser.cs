namespace ReciteHelper.Core.Interfaces.Services;

public interface IParser
{
    T Parse<T>(string content);
    string Serialize<T>(T obj);
}