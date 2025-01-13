namespace DbDemo.Services;

public interface IPhoneLibraryDb
{
    Task<bool> IsExistAsync(string msisdn);
    Task<PhoneNumber> GetAsync(string msisdn);
    Task<string> GetAsync(string msisdn, MsisdnField field);
    Task<ResponseBase> SetAsync(string msisdn, string @operator, DateTime? updateTime = null);
    Task<long> CountRowAsync(int? index = null);
    //Task<ResponseBase> InsertBatchAsync(List<PhoneNumber> list);
    IAsyncEnumerable<PhoneNumber> StreamAsync(int? index, CancellationToken ct);
    Task<ResponseBase> DeleteAsync(string msisdn);
    Task<ResponseBase> SaveDocAsync(string username, string filename);
    Task<string> GetDocAsync(string username);
    Task<bool> IsUserExistAsync(string username);
}
