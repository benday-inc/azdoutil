using Benday.AzureDevOpsUtil.Api.TfvcAssessment;

namespace Benday.AzureDevOpsUtil.UnitTests;

[TestClass]
public class AzureDevOpsErrorMessageReaderFixture
{
    private const string Fallback = "404 Not Found";

    /// <summary>
    /// The body Azure DevOps returns for a TFVC path that cannot be read.
    /// </summary>
    private const string ItemNotFoundBody =
        "{\"$id\":\"1\",\"innerException\":null," +
        "\"message\":\"VS403405: The item $/Main/Benday.TfvcDemo does not exist on the server, " +
        "or you do not have permission to access it.\"," +
        "\"typeName\":\"Microsoft.TeamFoundation.VersionControl.Server.ItemNotFoundException\"," +
        "\"typeKey\":\"ItemNotFoundException\",\"errorCode\":0,\"eventId\":4001}";

    [TestMethod]
    public void GetMessage_ReadsTheServerMessage()
    {
        var actual = AzureDevOpsErrorMessageReader.GetMessageOrDefault(ItemNotFoundBody, Fallback);

        StringAssert.Contains(actual, "VS403405", "The error code should survive.");
        StringAssert.Contains(
            actual, "does not exist on the server", "The explanation should survive.");
    }

    [TestMethod]
    public void GetMessage_EmptyBodyFallsBack()
    {
        Assert.AreEqual(
            Fallback,
            AzureDevOpsErrorMessageReader.GetMessageOrDefault(null, Fallback),
            "Null should fall back.");

        Assert.AreEqual(
            Fallback,
            AzureDevOpsErrorMessageReader.GetMessageOrDefault("", Fallback),
            "Empty should fall back.");

        Assert.AreEqual(
            Fallback,
            AzureDevOpsErrorMessageReader.GetMessageOrDefault("   ", Fallback),
            "Whitespace should fall back.");
    }

    [TestMethod]
    public void GetMessage_HtmlSignInPageFallsBack()
    {
        // An expired token gets an html sign-in page rather than json.
        var actual = AzureDevOpsErrorMessageReader.GetMessageOrDefault(
            "<!DOCTYPE html><html><body>Sign In</body></html>", Fallback);

        Assert.AreEqual(Fallback, actual, "Html is not a json error body.");
    }

    [TestMethod]
    public void GetMessage_JsonWithoutAMessageFallsBack()
    {
        var actual = AzureDevOpsErrorMessageReader.GetMessageOrDefault(
            "{\"count\":0,\"value\":[]}", Fallback);

        Assert.AreEqual(Fallback, actual, "There is no message to read.");
    }

    [TestMethod]
    public void GetMessage_NullMessageFallsBack()
    {
        var actual = AzureDevOpsErrorMessageReader.GetMessageOrDefault(
            "{\"message\":null}", Fallback);

        Assert.AreEqual(Fallback, actual, "A null message is not usable.");
    }

    [TestMethod]
    public void GetMessage_JsonArrayFallsBack()
    {
        var actual = AzureDevOpsErrorMessageReader.GetMessageOrDefault("[1,2,3]", Fallback);

        Assert.AreEqual(Fallback, actual, "An array carries no message property.");
    }
}
