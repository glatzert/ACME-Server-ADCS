using Th11s.ACMEServer.Model.Configuration;

namespace ACMEServer.Tests.Utils.Fakes;

public class FakeProfileConfiguration : FakeOptionSnapshot<ProfileConfiguration>
{
    public FakeProfileConfiguration(params ProfileConfiguration[] values)
        : base(values.ToDictionary(x => x.Name))
    { }
}
