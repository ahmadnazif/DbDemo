namespace DbDemo.Services;

public interface IPhoneLibraryDb
{
    Task<bool> IsExistAsync(string msisdn);
    Task<Msisdn> GetAsync(string msisdn);
    Task<string> GetAsync(string msisdn, MsisdnField field);
    Task<ResponseBase> SetAsync(string msisdn, string @operator);
    Task<ResponseBase> DeleteAsync(string msisdn);    
}
