using System.Text.Json;

namespace ReciteHelper.Infrastructure.Utilities;

public class OCRProvider
{
    private readonly ReciteHelper.Core.Configuration.ConfigOptions _config;

    public OCRProvider(ReciteHelper.Core.Configuration.ConfigOptions config)
    {
        _config = config;
    }
    private AlibabaCloud.SDK.Ocr_api20210707.Client? CreateValidation()
    {
        if (_config is null ||
             _config.OCRAccess is null ||
              _config.OCRSecret is null)
            return null;

        var config = new Aliyun.Credentials.Models.Config()
        {
            Type = "access_key",
            AccessKeyId = _config.OCRAccess,
            AccessKeySecret = _config.OCRSecret
        };
        var credentialClient = new Aliyun.Credentials.Client(config);

        var conf = new AlibabaCloud.OpenApiClient.Models.Config
        {
            Credential = credentialClient,
        };
        conf.Endpoint = "ocr-api.cn-hangzhou.aliyuncs.com";
        var client = new AlibabaCloud.SDK.Ocr_api20210707.Client(conf);

        return client;
    }

    public string? Request()
    {
        var client = CreateValidation();

        if (client is null)
        {
            return null;
        }

        var bodyStream = AlibabaCloud.DarabonbaStream.StreamUtil.ReadFromFilePath(@"C:\Users\Arabid\Desktop\test.png");
        var recognizeGeneralRequest = new AlibabaCloud.SDK.Ocr_api20210707.Models.RecognizeGeneralRequest
        {
            Body = bodyStream,
        };
        var runtime = new AlibabaCloud.TeaUtil.Models.RuntimeOptions();

        var result = client.RecognizeGeneralWithOptions(recognizeGeneralRequest, runtime);
        var data = JsonSerializer.Deserialize<Text>(result.Body.Data);

        if (data is null) return null;
        return data.Content;
    }
}
