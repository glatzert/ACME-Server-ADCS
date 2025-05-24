namespace Th11s.ACMEServer.AspNetCore.Endpoints;

public static class EndpointNames
{
    public const string Directory = nameof(Directory);
    public const string Profile = nameof(Profile);
    public const string NewNonce = nameof(NewNonce);
    
    public const string NewAccount = nameof(NewAccount);
    public const string GetAccount = nameof(GetAccount);
    public const string GetOrderList = nameof(GetOrderList);
    
    public const string NewOrder = nameof(NewOrder);
    public const string GetOrder = nameof(GetOrder);
    public const string GetAuthorization = nameof(GetAuthorization);
    public const string AcceptChallenge = nameof(AcceptChallenge);
    public const string FinalizeOrder = nameof(FinalizeOrder);
    public const string GetCertificate = nameof(GetCertificate);

    public const string NewAuthz = nameof(NewAuthz);
    
    public const string RevokeCert = nameof(RevokeCert);
    
    public const string KeyChange = nameof(KeyChange);


}
