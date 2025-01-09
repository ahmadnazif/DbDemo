namespace DbDemo.Services;

public interface IMsisdnDb
{
    Task<bool> IsExistAsync(string msisdn);
    Task<PhoneNumber> GetAsync(string msisdn);
    Task<string> GetAsync(string msisdn, MsisdnField field);
    Task<ResponseBase> SetAsync(string msisdn, string @operator, DateTime? updateTime = null);
    Task<long> CountRowAsync(int? index = null);
    //Task<ResponseBase> InsertBatchAsync(List<PhoneNumber> list);
    IAsyncEnumerable<PhoneNumber> StreamAsync(int? index, CancellationToken ct);
    Task<ResponseBase> DeleteAsync(string msisdn);
}
