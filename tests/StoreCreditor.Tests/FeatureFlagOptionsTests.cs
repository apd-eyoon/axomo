using StoreCreditor.Services.Options;

namespace StoreCreditor.Tests;

public sealed class FeatureFlagOptionsTests
{
    [Fact]
    public void GetStagingEmployeeEmailSet_TrimsAndMatchesEmailsIgnoringCase()
    {
        var options = new FeatureFlagOptions
        {
            StagingEmployeeEmails =
            [
                " Ada.Lovelace@AimpointDigital.com ",
                "",
                "grace.hopper@aimpointdigital.com"
            ]
        };

        var emails = options.GetStagingEmployeeEmailSet();

        Assert.Equal(2, emails.Count);
        Assert.Contains("ada.lovelace@aimpointdigital.com", emails);
        Assert.Contains("GRACE.HOPPER@AIMPOINTDIGITAL.COM", emails);
    }
}
