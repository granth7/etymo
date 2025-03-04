using Dapper;
using Newtonsoft.Json;
using System.Data;

namespace etymo.ApiService.Postgres.Handlers;

public class DictionaryTypeHandler : SqlMapper.TypeHandler<Dictionary<string, string>>
{
    public override void SetValue(IDbDataParameter parameter, Dictionary<string, string> value)
    {
        parameter.Value = JsonConvert.SerializeObject(value);
        parameter.DbType = DbType.String;
    }

    public override Dictionary<string, string> Parse(object value)
    {
        return JsonConvert.DeserializeObject<Dictionary<string, string>>(value as string);
    }
}
