namespace Th11s.ACMEServer.AspNetCore.Endpoints
{
    public static class EndpointNames
    {
        public const string Directory = nameof(Directory);
        public const string NewNonce = nameof(NewNonce);
        
        public const string NewAccount = nameof(NewAccount);
        public const string Account = nameof(Account);
        public const string OrderList = nameof(OrderList);
        
        public const string NewOrder = nameof(NewOrder);
        public const string Order = nameof(Order);

        public const string NewAuthz = nameof(NewAuthz);
        
        public const string RevokeCert = nameof(RevokeCert);
        
        public const string KeyChange = nameof(KeyChange);


    }
}
