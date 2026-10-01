using Dapper;
using Arietta.Core;
using System;
using System.Data;
using System.Globalization;

namespace Arietta.Features.Database;





internal static class DapperSqlMapper
{
    public sealed class GameBizHandler : SqlMapper.TypeHandler<GameBiz>
    {
        public override GameBiz Parse(object value)
        {
            return value switch
            {
                string s when Enum.TryParse<GameBiz>(s, out var biz) => biz,
                long l => (GameBiz)l,
                int i => (GameBiz)i,
                _ => GameBiz.WutheringWaves,
            };
        }

        public override void SetValue(IDbDataParameter parameter, GameBiz value)
        {
            parameter.DbType = DbType.String;
            parameter.Value = value.ToString();
        }
    }

    public sealed class DateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset>
    {
        public override DateTimeOffset Parse(object value)
        {
            return value switch
            {
                string s when DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto) => dto,
                long l => DateTimeOffset.FromUnixTimeMilliseconds(l),
                _ => default,
            };
        }

        public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
        {
            parameter.DbType = DbType.String;
            parameter.Value = value.ToString("O", CultureInfo.InvariantCulture);
        }
    }
}
