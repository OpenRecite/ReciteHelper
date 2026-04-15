using ReciteHelper.Core.Interfaces.Services;
using System.Text.Json;

namespace ReciteHelper.Infrastructure.Services;

public class ParserService : IParser
{
    public T Parse<T>(string content)
    {
        try
        {
            // 尝试 JSON 解析
            return JsonSerializer.Deserialize<T>(content)!;
        }
        catch (Exception)
        {
            // 如果 JSON 解析失败，尝试其他解析方式
            throw new NotImplementedException("解析类型不支持");
        }
    }

    public string Serialize<T>(T obj)
    {
        return JsonSerializer.Serialize(obj);
    }
}