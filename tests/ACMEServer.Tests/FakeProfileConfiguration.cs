using Th11s.ACMEServer.Model.Configuration;

namespace Th11s.AcmeServer.Tests
{
    public class FakeProfileConfiguration : FakeOptionSnapshot<ProfileConfiguration>
    {
        public FakeProfileConfiguration(params ProfileConfiguration[] values)
            : base(values.ToDictionary(x => x.Name))
        { }
    }
}
