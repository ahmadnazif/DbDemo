namespace DbDemo.Services;

public interface IPhoneLibraryDb
{
    Task<bool> IsExistAsync(string msisdn);
    Task<Msisdn> GetAsync(string msisdn);
    Task<string> GetAsync(string msisdn, MsisdnField field);
    Task<ResponseBase> SetAsync(string msisdn, string @operator, DateTime? updateTime = null);
    //[Obsolete("Use the one in dedicated interface")] Task<long> CountRowAsync(int? index = null);
    //Task<ResponseBase> InsertBatchAsync(List<PhoneNumber> list);
    Task<long> CountRowAsync(int? index = null);
    Task<ResponseBase> DeleteAsync(string msisdn);    
}
